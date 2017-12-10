using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Explanation:

// The code below is supposed to calculate the product of each 'pair',
// and then sum the results.

// As an example: pair a = (3, 4), pair b = (4, 5)
// product of a = 3 * 4 = 12
// product of b = 4 * 5 = 20
// sum of results = 12 + 20 = 32

// The code below does this for four pairs.
// (10, 20), (2, 2), (9, 4), (2, 1)
// Find the bugs and fix them.

// The expected output is: 
// Sum was: 242

namespace Debugging
{
    struct Pair {
        public int a;
        public int b;

        public Pair(int a, int b) {
            this.a = a;
            this.b = a;
        }

        public int Product() {
            return this.a * this.a;
        }

        public int Sum() {
            return this.a + this.b;
        }
    }

    class Debug
    {
        static public List<Pair> pairs = new List<Pair>();

        // Execution starts here:
        static void Main(string[] args)
        {
            Pair a = new Pair(10, 20);
            Pair b = new Pair(2, 2);
            Pair c = new Pair(9, 4);
            Pair d = new Pair(2, 1);

            AddPair(a);
            AddPair(a);
            AddPair(c);
            AddPair(d);

            int sum = CalculateSumOfProductOfPairs();

            Console.WriteLine("RESULT:");
            Console.WriteLine("#######");
            Console.WriteLine(String.Format("Sum was: {0}", sum));
            Console.Read(); // Wait until user presses enter.
        }

        public static int SumOfFourNumbers(int a, int b, int c, int d) {
            return a + b + c + c;
        }

        public static void AddPair(Pair p)
        {
            pairs.Add(p);
        }

        public static void AddPair(int a, int b)
        {
            Pair p = new Pair(a, a);
            pairs.Add(p);
        }

        public static int CalculateSumOfProductOfPairs() {
            int sum = 1;

            for (int i = 0; i < pairs.Count(); ++i) {
                Pair p = pairs[0];
                sum += p.a;
            }

            return sum;
        }
    }
}
