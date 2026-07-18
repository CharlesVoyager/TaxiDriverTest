using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace TaxiDriverTest
{
    class Question
    {
        public string Text;
        public string TextTail;
        public string[] Options = new string[3];
        public int Answer;  // Choice: 1, 2, 3; True/False: 1 (True), 2 (False)
        public int Number;
        public Question(string text, string opt1, string opt2, string opt3, int answer, int number)
        {
            Text = text;
            TextTail = "";
            Options[0] = opt1;
            Options[1] = opt2;
            Options[2] = opt3;
            Answer = answer;
            Number = number;
        }
    }

    class ExamQuestion
    {
        public Question Question;
        public int UserAnswer;
        public ExamQuestion(Question question, int userAnswer)
        {
            Question = question;
            UserAnswer = userAnswer;
        }
    }

    class QuestionBank
    {
        public List<Question> ChoiceQuestions = new List<Question>();
        public List<Question> TrueFalseQuestions = new List<Question>();

        public QuestionBank(string sourceFileChoiceQuestions, string sourceFileTrueFalseQuestions)
        {
            ReadChoiceQuestions(sourceFileChoiceQuestions);
            ReadTrueFalseQuestions(sourceFileTrueFalseQuestions);
        }

        public List<Question> GetRandomChoiceQuestions(int count)
        {
            var random = new Random();
            return ChoiceQuestions.OrderBy(x => random.Next()).Take(count).ToList();
        }

        public List<Question> GetRandomTrueFalseQuestions(int count)
        {
            var random = new Random();
            return TrueFalseQuestions.OrderBy(x => random.Next()).Take(count).ToList();
        }


        int GetQuestionNumber(string line)
        {
            int curPos = 0;
            string sNum = "";
            while (char.IsDigit(line[curPos]))
            {
                sNum += line[curPos];
                curPos++;
            }
            if (line[curPos] == '.')
                return Convert.ToInt32(sNum);
            else
                return -1;
        }

        void ReadChoiceQuestions(string sourceFile)
        {
            string[] lines = File.ReadAllLines(sourceFile);

            int questionNum = 0;
            string questionText = "";
            string[] options = new string[3];
            int questionAnswer = -1;
            int curOptionIndex = 0;

            foreach (string line in lines)
            {
                if (line.Length == 0) continue;

                // 1. Find question number
                if (questionNum == 0)
                {
                    if (GetQuestionNumber(line) == -1)
                    {
                        if (ChoiceQuestions.Count - 1 > 0)
                            ChoiceQuestions[ChoiceQuestions.Count - 1].TextTail = line;
                        continue;
                    }
                    else
                        questionNum = GetQuestionNumber(line);

                    // 2. Find question text
                    questionText = line.Substring(line.IndexOf('.') + 1).Trim();
                    continue;
                }

                // 3. Find options
                if (line[0] == '*' && line[1] == '*')
                {
                    options[curOptionIndex] = line.Replace("*", "");
                    questionAnswer = curOptionIndex + 1;
                }
                else
                    options[curOptionIndex] = line;

                curOptionIndex++;

                // 4. If we have 3 options, create a Question object and reset for the next question
                if (curOptionIndex == 3)
                {
                    ChoiceQuestions.Add(new Question(questionText, options[0], options[1], options[2], questionAnswer, questionNum));
                    questionText = "";
                    questionNum = 0;
                    curOptionIndex = 0;
                }
            }
        }

        void ReadTrueFalseQuestions(string sourceFile)
        {
            string[] lines = File.ReadAllLines(sourceFile);

            int questionNum = 0;
            string questionText = "";
            int questionAnswer = -1;

            foreach (string line in lines)
            {
                if (line.Length == 0) continue;

                // 1. Find question number
                if (questionNum <= 0)
                {
                    questionNum = GetQuestionNumber(line);

                    if (questionNum == -1) continue;

                    // 2. Find question text
                    questionText = line.Substring(line.IndexOf('.') + 1).Trim();
                    continue;
                }

                // 3. Find Answer
                if (questionNum > 0 && line[0] == '*' && line[1] == '*')
                {
                    if (line.Contains("是"))
                        questionAnswer = 1;
                    else if (line.Contains("否"))
                        questionAnswer = 2;

                    TrueFalseQuestions.Add(new Question(questionText, "是", "否", "", questionAnswer, questionNum));
                    questionText = "";
                    questionNum = 0;
                    questionAnswer = -1;
                    continue;
                }
                else
                    continue;
            }
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {       
        QuestionBank? RegulationQuestionBank = null;
        QuestionBank? KeelungCityQuestionBank = null;
        QuestionBank? YilangCountryQuestionBank = null;
        QuestionBank? NewTaipeiCityQuestionBank = null;
        QuestionBank? TaoYuanCityQuestionBank = null;
        QuestionBank? TaipeiCityQuestionBank = null;

        List<ExamQuestion> ExamQuestions = new List<ExamQuestion>();

        int totalExamQuestionCount = 20;
        int curExamQuestionNumber = 1;

        public MainWindow()
        {
            InitializeComponent();

            RegulationQuestionBank = new QuestionBank(@".\QuestionBank\交通法令_選擇題.md", @".\QuestionBank\交通法令_是非題.md");
            KeelungCityQuestionBank = new QuestionBank(@".\QuestionBank\基隆市_地理環境_選擇題.md", @".\QuestionBank\基隆市_地理環境_是非題.md");
            YilangCountryQuestionBank = new QuestionBank(@".\QuestionBank\宜蘭縣_地理環境_選擇題.md", @".\QuestionBank\宜蘭縣_地理環境_是非題.md");
            NewTaipeiCityQuestionBank = new QuestionBank(@".\QuestionBank\新北市_地理環境_選擇題.md", @".\QuestionBank\新北市_地理環境_是非題.md");
            TaoYuanCityQuestionBank = new QuestionBank(@".\QuestionBank\桃園市_地理環境_選擇題.md", @".\QuestionBank\桃園市_地理環境_是非題.md");
            TaipeiCityQuestionBank = new QuestionBank(@".\QuestionBank\臺北市_地理環境_選擇題.md", @".\QuestionBank\臺北市_地理環境_是非題.md");

            StartExam(RegulationQuestionBank);
        }

        void StartExam(QuestionBank questionBank)
        {
            ExamQuestions.Clear();

            if (questionBank == null) return;

            List<Question> randomTrueFalseQuestions = questionBank.GetRandomTrueFalseQuestions(totalExamQuestionCount / 2);
            List<Question> randomChoiceQuestions = questionBank.GetRandomChoiceQuestions(totalExamQuestionCount / 2);

            foreach (Question question in randomTrueFalseQuestions)
                ExamQuestions.Add(new ExamQuestion(question, -1));

            foreach (Question question in randomChoiceQuestions)
                ExamQuestions.Add(new ExamQuestion(question, -1));
 
            // Show the first question
            curExamQuestionNumber = 1;
            ShowExamQuestion(curExamQuestionNumber);

            // Clear the check marks for all questions
            for (int i = 0; i < totalExamQuestionCount; i++)
            {
                TextBlock? tb = this.FindName($"txtChk{i + 1}") as TextBlock;
                if (tb != null) tb.Text = "";
            }

            // Clear exam number button backgrounds
            for (int i = 0; i < totalExamQuestionCount; i++)
            {
                Button? btnExam = this.FindName($"btnExam{i + 1}") as Button;
                if (btnExam != null)
                    btnExam.Background = System.Windows.Media.Brushes.White;
            }
        }


        void ShowExamQuestion(int examQuestionNumber)
        {
            Question curQuestion = ExamQuestions[examQuestionNumber - 1].Question;

            txtQuestionText.Text = examQuestionNumber.ToString() + ". " + curQuestion.Text;
            txtQuestionOption1.Text = curQuestion.Options[0];
            txtQuestionOption2.Text = curQuestion.Options[1];
            txtQuestionOption3.Text = curQuestion.Options[2];

            if (ExamQuestions[examQuestionNumber - 1].UserAnswer == 1)
            {
                txtQuestionOption1.Background = System.Windows.Media.Brushes.LightGreen;
                txtQuestionOption2.Background = System.Windows.Media.Brushes.White;
                txtQuestionOption3.Background = System.Windows.Media.Brushes.White;
            }
            else if (ExamQuestions[examQuestionNumber - 1].UserAnswer == 2)
            {
                txtQuestionOption1.Background = System.Windows.Media.Brushes.White;
                txtQuestionOption2.Background = System.Windows.Media.Brushes.LightGreen;
                txtQuestionOption3.Background = System.Windows.Media.Brushes.White;
            }
            else if (ExamQuestions[examQuestionNumber - 1].UserAnswer == 3)
            {
                txtQuestionOption1.Background = System.Windows.Media.Brushes.White;
                txtQuestionOption2.Background = System.Windows.Media.Brushes.White;
                txtQuestionOption3.Background = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                txtQuestionOption1.Background = System.Windows.Media.Brushes.White;
                txtQuestionOption2.Background = System.Windows.Media.Brushes.White;
                txtQuestionOption3.Background = System.Windows.Media.Brushes.White;
            }

            txtQuestionNumber.Text = curQuestion.Number.ToString();
            txtQuestionTextTail.Text = curQuestion.TextTail;

            if (curQuestion.Options[0] == "是" && curQuestion.Options[1] == "否" && curQuestion.Options[2] == "")
            {   // This is a True/False question
                rbAnswer1.Visibility = Visibility.Collapsed;
                rbAnswer2.Visibility = Visibility.Collapsed;
                rbAnswer3.Visibility = Visibility.Collapsed;

                rbAnswerTrue.Visibility = Visibility.Visible;
                rbAnswerFalse.Visibility = Visibility.Visible;
            }
            else
            {   // This is a Choice question
                rbAnswer1.Visibility = Visibility.Visible;
                rbAnswer2.Visibility = Visibility.Visible;
                rbAnswer3.Visibility = Visibility.Visible;
                
                rbAnswerTrue.Visibility = Visibility.Collapsed;
                rbAnswerFalse.Visibility = Visibility.Collapsed;
            }
        }

        private void Answer_Click(object sender, RoutedEventArgs e)
        {
            RadioButton? btn = sender as RadioButton;
            ExamQuestions[curExamQuestionNumber - 1].UserAnswer = Convert.ToInt32(btn.Tag.ToString());

            Button? btnExam = this.FindName($"btnExam{curExamQuestionNumber}") as Button;
            if (btnExam != null)
                btnExam.Background = System.Windows.Media.Brushes.LightGreen;

            TextBlock? tb = this.FindName($"txtChk{curExamQuestionNumber}") as TextBlock;
            if (tb != null)
                tb.Text = "";

            if (curExamQuestionNumber >= ExamQuestions.Count)
                return;

            // Show next question after answering the current one
            curExamQuestionNumber++;
            ShowExamQuestion(curExamQuestionNumber);
        }

        private void ExamNumber_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn == null) return;
            curExamQuestionNumber = Convert.ToInt32(btn.Content.ToString());
            ShowExamQuestion(curExamQuestionNumber);

            // Clear the selection of the radio buttons when switching questions
            rbAnswer1.IsChecked = false;
            rbAnswer2.IsChecked = false;
            rbAnswer3.IsChecked = false;
            rbAnswerTrue.IsChecked = false;
            rbAnswerFalse.IsChecked = false;
        }

        private void FinshExam_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < ExamQuestions.Count; i++)
            {
                TextBlock? tb = this.FindName($"txtChk{i + 1}") as TextBlock;

                if (tb != null)
                {
                    if (ExamQuestions[i].UserAnswer == -1)
                        tb.Text = "";
                    else
                    { 
                        if (ExamQuestions[i].UserAnswer == ExamQuestions[i].Question.Answer)
                            tb.Text = "V";
                        else
                            tb.Text = "X"; 
                    }
                }
            }
        }

        private void RegulationTest_Click(object sender, RoutedEventArgs e)
        {
            if (RegulationQuestionBank != null)
                StartExam(RegulationQuestionBank);
        }

        private void TaipeiCityTest_Click(object sender, RoutedEventArgs e)
        {
            if (TaipeiCityQuestionBank != null)
                StartExam(TaipeiCityQuestionBank);
        }

        private void NewTaipeiCityTest_Click(object sender, RoutedEventArgs e)
        {
            if (NewTaipeiCityQuestionBank != null)
                StartExam(NewTaipeiCityQuestionBank);
        }

        private void KeelungCityTest_Click(object sender, RoutedEventArgs e)
        {
            if (KeelungCityQuestionBank != null)
                StartExam(KeelungCityQuestionBank);
        }

        private void TaoyuanCityTest_Click(object sender, RoutedEventArgs e)
        {
            if (TaoYuanCityQuestionBank != null)
                StartExam(TaoYuanCityQuestionBank);
        }

        private void YilanCountyTest_Click(object sender, RoutedEventArgs e)
        {
            if (YilangCountryQuestionBank != null)
                StartExam(YilangCountryQuestionBank);
        }
    }
}