using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebuggingSolutions
{
    struct Quiz
    {
        public int score;
        public int max;

        public Quiz(int score, int max)
        {
            this.score = score;
            this.max = max;
        }
    }

    class Debug
    {
        static private List<Quiz> quizzes = new List<Quiz>();

        static void Main(string[] args)
        {
            // Run the program.
            AddQuiz(5, 100);
            AddQuiz(20, 100);
            AddQuiz(99, 100);
            AddQuiz(78, 100);
            AddQuiz(50, 100);

            double average = CalculateAverage();
            Console.WriteLine("RESULT:");
            Console.WriteLine("#######");
            Console.WriteLine(String.Format("Average was: {0}", average));
            Console.Read(); // Wait until user presses enter.

            // Expected output:
            // Average was: 50.4
        }

        public static void AddQuiz(Quiz q)
        {
            // put q in the list
            quizzes.Add(q);
        }

        public static void AddQuiz(int score, int max)
        {
            // first create a Quiz object
            Quiz q = new Quiz(score, max); // 1. Switch the arguments.
            // now add the Quiz
            AddQuiz(q);
        }

        public static double CalculateAverage() // 2. Need to return a double, rather than an int to prevent dropping the decimal.
        {
            // keep track of the totals
            double totalScore = 0;
            double count = 0; // 3. Need to keep tracking of number of quizzes, not the average of the max.

            // loop through the ArrayList
            for (int i = 0; i < quizzes.Count; i++) // 4. Need to do i++, not ++i
            {
                // get the Quiz at index i
                Quiz q = quizzes.ElementAt(i);

                // update totalScore and totalMax
                totalScore += q.score;
                count++; // 5. Need to keep track of the count. Could also simply do quizzes.Count
            }

            return (totalScore / count); // 6. Need to return the actual average, not multiplying by 100.
        }
    }
}
