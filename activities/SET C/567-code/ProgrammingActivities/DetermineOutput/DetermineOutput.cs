using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetermineOutput
{
    class DetermineOutput
    {
        static void Main(string[] args)
        {
            Console.WriteLine("What is the output?");

            // Uncomment to see the answers.
            Question1();
            Question2();
            Question3();
        }

        // PROBLEM 1:
        // What does Question1() print to the console?
        private static void Question1()
        {
            int a = 0;
            int b = 1;
            int c = 2;

            int d = (a - 1) * (b + 2) * (c + 3);

            if(d >= 0) {
                int e = a + b + d;
                Console.WriteLine("e is {0}", e);    
            }
            else {
                int e = a - b - d;
                Console.WriteLine("e is {0}", e);
            }
        }

        // PROBLEM 2:
        // What does Question2() print to the console?
        private static void Question2()
        {
            int[] arr = { 1, 2, 3, 4};

            int x = 10;
            int y = 0;
            for (int i = 0; i < arr.Count(); i++) {
                x += arr[i] - 1;
                y += i;
            }

            Console.WriteLine("x is {0}", x);
            Console.WriteLine("y is {0}", y);
        }

        // PROBLEM 3:
        // What does Question3() print to the console?
        private static void Question3()
        {
            Bar();
        }

        private static void Bar()
        {
            int y = 2;
            int foo = Foo(2);

            for (int i = 0; i < foo; i++)
            {
                if (Baz(i))
                {
                    Console.WriteLine(String.Format("{0}", i + Foo(y)));
                }
                else
                {
                    Console.WriteLine(String.Format("{0}", Foo(i)));
                }
            }
        }

        private static int Foo(int a)
        {
            return a * a;
        }

        private static bool Baz(int i) {
            if(i == 0) {
                return true;
            }
            else if (i == 3) {
                return true;
            }
            else {
                return false;
            }
        }
    }
}




