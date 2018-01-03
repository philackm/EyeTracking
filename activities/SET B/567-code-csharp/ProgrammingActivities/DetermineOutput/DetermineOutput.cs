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
            //Question1();
            //Question2();
            //Question3();
        }

        // PROBLEM 1:
        // What does Question1() print to the console?
        private static void Question1()
        {
            bool x = true;
            int y = 1;
            int z = 2;
            int a = 0;

            x = (y - 1) == a;

            if(!x) {
                a = y + 1;
            }
            else {
                z = z + 1;
                a = y + z;
            }

            Console.WriteLine("a is {0}", a);
        }

        // PROBLEM 2:
        // What does Question2() print to the console?
        private static void Question2()
        {
            int[] arr = { 1, 2, 3, 4};
            int x = 0;
            foreach (int a in arr) {
                int t = a * 2;
                x += t;
                x -= 1;
            }

            Console.WriteLine("x is {0}", x);
        }

        // PROBLEM 3:
        // What does Question3() print to the console?
        private static void Question3()
        {
            Bar();
        }

        private static int Foo(int a)
        {
            return (a * a) - 1;
        }

        private static bool Baz(int i) {
            // % is the modulus operator, e.g., x % y, returns the remainder
            // after dividing x by y.
            return (i % 2) == 0;
        }

        private static void Bar()
        {
            int y = 2;

            for (int i = 0; i < 5; i++)
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
    }
}




