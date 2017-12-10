using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WriteAlgorithmSolutions
{
    class FibSolution
    {
        // Prints the first 10 integers in the Fibonacci sequence.
        public static void PrintFib()
        {
            // Use Console.WriteLine(string s) to write a line to the console.
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(Fib(i));
            }
        }
        
        // Returns the nth value of the Fibonacci sequence
        public static int Fib(int n)
        {
            switch(n)
            {
                case 0:
                    return 0;
                case 1:
                    return 1;
                default:
                    return Fib(n - 1) + Fib(n - 2);
            }
        }
    }
}
