using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WriteAlgorithmSolutions
{
    class BubbleSortSolution
    {
        // Implement bubble sort and return the array of sorted integers.
        public static int[] Sort(int[] numbers)
        {
            int[] result = numbers;

            for(int end = numbers.Length - 1; end > 0; end--) // Keep track of where we have to compare up to, after each cycle we have moved one to the very end, so dont include that in the next cycle.
            {
                for(int first = 0; first < end; first++) // Starting from the start of the list, up until the last "unsorted" item, compare each of them.
                {
                    // Compare the two numbers in the window, if left is > right, then swap, all the larger numbers will bubble down to the end.
                    if (result[first] > result[first + 1])
                    {
                        int temp = result[first];
                        result[first] = result[first + 1];
                        result[first + 1] = temp;
                    }
                }
            }

            return result;
        }

        public static void PrintArray(int[] arr)
        {
            foreach (int i in arr)
            {
                Console.WriteLine(i);
            }
        }
    }
}
