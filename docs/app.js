'use strict';

/* ---------- Parsing (ported 1:1 from MainWindow.xaml.cs) ---------- */

class Question {
  constructor(text, opt1, opt2, opt3, answer, number) {
    this.text = text;
    this.textTail = '';
    this.options = [opt1, opt2, opt3];
    this.answer = answer; // Choice: 1,2,3. True/False: 1 (是/True), 2 (否/False)
    this.number = number;
  }
}

function getQuestionNumber(line) {
  let curPos = 0;
  let sNum = '';
  while (curPos < line.length && line[curPos] >= '0' && line[curPos] <= '9') {
    sNum += line[curPos];
    curPos++;
  }
  if (sNum.length === 0) return -1;
  if (line[curPos] === '.') return parseInt(sNum, 10);
  return -1;
}

function parseChoiceQuestions(raw) {
  const lines = raw.split(/\r?\n/);
  const questions = [];

  let questionNum = 0;
  let questionText = '';
  let options = ['', '', ''];
  let questionAnswer = -1;
  let curOptionIndex = 0;

  for (const line of lines) {
    if (line.length === 0) continue;

    // 1. Find question number
    if (questionNum === 0) {
      if (getQuestionNumber(line) === -1) {
        if (questions.length - 1 > 0) questions[questions.length - 1].textTail = line;
        continue;
      } else {
        questionNum = getQuestionNumber(line);
      }
      // 2. Find question text
      questionText = line.substring(line.indexOf('.') + 1).trim();
      continue;
    }

    // 3. Find options
    if (line[0] === '*' && line[1] === '*') {
      options[curOptionIndex] = line.split('*').join('');
      questionAnswer = curOptionIndex + 1;
    } else {
      options[curOptionIndex] = line;
    }
    curOptionIndex++;

    // 4. If we have 3 options, create a Question object and reset for the next question
    if (curOptionIndex === 3) {
      questions.push(new Question(questionText, options[0], options[1], options[2], questionAnswer, questionNum));
      questionText = '';
      questionNum = 0;
      options = ['', '', ''];
      curOptionIndex = 0;
    }
  }
  return questions;
}

function parseTrueFalseQuestions(raw) {
  const lines = raw.split(/\r?\n/);
  const questions = [];

  let questionNum = 0;
  let questionText = '';
  let questionAnswer = -1;

  for (const line of lines) {
    if (line.length === 0) continue;

    // 1. Find question number
    if (questionNum <= 0) {
      questionNum = getQuestionNumber(line);
      if (questionNum === -1) continue;
      // 2. Find question text
      questionText = line.substring(line.indexOf('.') + 1).trim();
      continue;
    }

    // 3. Find answer
    if (questionNum > 0 && line[0] === '*' && line[1] === '*') {
      if (line.includes('是')) questionAnswer = 1;
      else if (line.includes('否')) questionAnswer = 2;

      questions.push(new Question(questionText, '是', '否', '', questionAnswer, questionNum));
      questionText = '';
      questionNum = 0;
      questionAnswer = -1;
      continue;
    }
  }
  return questions;
}

function shuffle(array) {
  const a = array.slice();
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

class QuestionBank {
  constructor(choiceRaw, tfRaw) {
    this.choiceQuestions = parseChoiceQuestions(choiceRaw);
    this.trueFalseQuestions = parseTrueFalseQuestions(tfRaw);
  }
  getRandomChoiceQuestions(count) {
    return shuffle(this.choiceQuestions).slice(0, count);
  }
  getRandomTrueFalseQuestions(count) {
    return shuffle(this.trueFalseQuestions).slice(0, count);
  }
}

/* ---------- Exam state ---------- */

const TOTAL_EXAM_QUESTION_COUNT = 20;

const banks = {};
for (const cat of CATEGORIES) {
  banks[cat.id] = new QuestionBank(RAW_BANKS[cat.choiceKey], RAW_BANKS[cat.tfKey]);
}

let examQuestions = []; // [{question, userAnswer}]
let curExamQuestionNumber = 1;
let curCategoryId = CATEGORIES[0].id;

/* ---------- DOM ---------- */

const el = {
  examNumber: document.getElementById('txtExamNumber'),
  questionText: document.getElementById('txtQuestionText'),
  option1: document.getElementById('txtQuestionOption1'),
  option2: document.getElementById('txtQuestionOption2'),
  option3: document.getElementById('txtQuestionOption3'),
  textTail: document.getElementById('txtQuestionTextTail'),
  questionNumber: document.getElementById('txtQuestionNumber'),
  categoryBar: document.getElementById('categoryBar'),
  answerChoice: document.getElementById('answerChoice'),
  answerTF: document.getElementById('answerTF'),
  rb: {
    1: document.getElementById('rbAnswer1'),
    2: document.getElementById('rbAnswer2'),
    3: document.getElementById('rbAnswer3'),
  },
  rbTF: {
    1: document.getElementById('rbAnswerTrue'),
    2: document.getElementById('rbAnswerFalse'),
  },
  examNumbers: document.getElementById('examNumbers'),
  checks: document.getElementById('checks'),
  finishBtn: document.getElementById('btnFinish'),
  scoreSummary: document.getElementById('scoreSummary'),
};

function buildCategoryBar() {
  el.categoryBar.innerHTML = '';
  for (const cat of CATEGORIES) {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'cat-btn';
    btn.textContent = cat.label;
    btn.dataset.catId = cat.id;
    btn.addEventListener('click', () => startExam(cat.id));
    el.categoryBar.appendChild(btn);
  }
  refreshCategoryBar();
}

function refreshCategoryBar() {
  el.categoryBar.querySelectorAll('.cat-btn').forEach((btn) => {
    btn.classList.toggle('active', btn.dataset.catId === curCategoryId);
  });
}

function buildExamNumberGrid() {
  el.examNumbers.innerHTML = '';
  el.checks.innerHTML = '';
  for (let i = 1; i <= TOTAL_EXAM_QUESTION_COUNT; i++) {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'num-btn';
    btn.textContent = String(i);
    btn.dataset.num = String(i);
    btn.addEventListener('click', () => goToQuestion(i));
    el.examNumbers.appendChild(btn);

    const chk = document.createElement('div');
    chk.className = 'chk';
    chk.dataset.num = String(i);
    el.checks.appendChild(chk);
  }
}

function startExam(categoryId) {
  curCategoryId = categoryId;
  refreshCategoryBar();

  const bank = banks[categoryId];
  examQuestions = [];

  const randomTF = bank.getRandomTrueFalseQuestions(TOTAL_EXAM_QUESTION_COUNT / 2);
  const randomChoice = bank.getRandomChoiceQuestions(TOTAL_EXAM_QUESTION_COUNT / 2);

  for (const q of randomTF) examQuestions.push({ question: q, userAnswer: -1 });
  for (const q of randomChoice) examQuestions.push({ question: q, userAnswer: -1 });

  curExamQuestionNumber = 1;
  showExamQuestion(curExamQuestionNumber);

  el.examNumbers.querySelectorAll('.num-btn').forEach((btn) => btn.classList.remove('answered'));
  el.checks.querySelectorAll('.chk').forEach((c) => {
    c.textContent = '';
    c.className = 'chk';
  });
  el.scoreSummary.textContent = '';
  el.finishBtn.disabled = false;
}

function isTrueFalse(question) {
  return question.options[0] === '是' && question.options[1] === '否' && question.options[2] === '';
}

function showExamQuestion(num) {
  const cur = examQuestions[num - 1];
  const q = cur.question;

  el.examNumber.textContent = num + '.';
  el.questionText.value = q.text;
  el.option1.textContent = q.options[0];
  el.option2.textContent = q.options[1];
  el.option3.textContent = q.options[2];
  el.textTail.textContent = q.textTail;
  el.questionNumber.textContent = String(q.number);

  el.option1.style.background = '';
  el.option2.style.background = '';
  el.option3.style.background = '';
  const opts = [null, el.option1, el.option2, el.option3];
  if (cur.userAnswer >= 1 && cur.userAnswer <= 3) opts[cur.userAnswer].style.background = '#c8f0c8';

  const tf = isTrueFalse(q);
  el.answerChoice.classList.toggle('hidden', tf);
  el.answerTF.classList.toggle('hidden', !tf);

  Object.values(el.rb).forEach((r) => r.classList.remove('selected'));
  Object.values(el.rbTF).forEach((r) => r.classList.remove('selected'));
  if (tf && cur.userAnswer >= 1) el.rbTF[cur.userAnswer].classList.add('selected');
  if (!tf && cur.userAnswer >= 1) el.rb[cur.userAnswer].classList.add('selected');
}

function answerClick(value) {
  examQuestions[curExamQuestionNumber - 1].userAnswer = value;

  const btn = el.examNumbers.querySelector(`.num-btn[data-num="${curExamQuestionNumber}"]`);
  if (btn) btn.classList.add('answered');
  const chk = el.checks.querySelector(`.chk[data-num="${curExamQuestionNumber}"]`);
  if (chk) {
    chk.textContent = '';
    chk.className = 'chk';
  }

  if (curExamQuestionNumber >= examQuestions.length) return;

  curExamQuestionNumber++;
  showExamQuestion(curExamQuestionNumber);
}

function goToQuestion(num) {
  curExamQuestionNumber = num;
  showExamQuestion(num);
}

function finishExam() {
  let correct = 0;
  let answered = 0;
  for (let i = 0; i < examQuestions.length; i++) {
    const chk = el.checks.querySelector(`.chk[data-num="${i + 1}"]`);
    if (!chk) continue;
    const cur = examQuestions[i];
    if (cur.userAnswer === -1) {
      chk.textContent = '';
      chk.className = 'chk';
      continue;
    }
    answered++;
    if (cur.userAnswer === cur.question.answer) {
      chk.textContent = 'V';
      chk.className = 'chk correct';
      correct++;
    } else {
      chk.textContent = 'X';
      chk.className = 'chk wrong';
    }
  }
  el.scoreSummary.textContent = `已作答 ${answered} / ${examQuestions.length} 題，答對 ${correct} 題`;
}

/* ---------- Wire up ---------- */

Object.entries(el.rb).forEach(([val, r]) => r.addEventListener('click', () => answerClick(Number(val))));
Object.entries(el.rbTF).forEach(([val, r]) => r.addEventListener('click', () => answerClick(Number(val))));
el.finishBtn.addEventListener('click', finishExam);

buildCategoryBar();
buildExamNumberGrid();
startExam(curCategoryId);
