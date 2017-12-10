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

            // Question1();
            // Question2();
            // Question3();
        }

        // PROBLEM 1:
        // What does Question1() print to the console?
        private static void Question1()
        {
            int x = 2;
            int y = 3;

            x = x + y;
            y = x - y;
            x = x - y;

            Console.WriteLine("x is {0}", x);
            Console.WriteLine("y is {0}", y);
        }

        // PROBLEM 2:
        // What does Question2() print to the console?
        private static void Question2()
        {
            int iX = 0, iNum = 2, iSum = 3;
            while (iX <= 5)
            {
                iSum = iSum + iX;
                iX = iX + 2;
                iNum++;
            }
            Console.WriteLine("iNum is {0}", iNum);
        }

        // PROBLEM 3:
        // What does Question3() print to the console?
        private static void Question3()
        {
            Bar();
        }

        private static int Foo(int a)
        {
            return a * a;
        }

        private static void Bar()
        {
            int y = 2;

            for (int i = 0; i < 5; i++)
            {
                if (i % 2 == 0)
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




