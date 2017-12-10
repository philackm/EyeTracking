using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WriteAlgorithm
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            // Note, you can use Console.WriteLine() to write lines to the output console.

            // ######################################################################
            // 1. Print the product of all of the following numbers: 1, 2, 3, 4, 5, 6
            // Write the code in the file "ProductOfNumbers.cs"

            // Uncomment this to test your implementation:
            //Question1.ProductOfNumbers(new int[] { 1, 2, 3, 4, 5, 6 });

            // ######################################################################
            // 2. For each number in the list 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            // if the number is divisible by 2, print "divisible by two"
            // if the number is divisible by 3, print "divisible by three"
            // if the number is divisible by both 2 and 3 print "divisble by two AND three"
            // otherwise print nothing.

            // Uncomment this to test your implementation:
            //Question2.DivisionTest(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            // ######################################################################
            // Given a list of numbers, print the number of times a duplicate appears in the list
            // for example, given the list { 1, 4, 5, 4, 2, 7, 8, 5 }
            // 4 appears twice and 5 appears twice, so we would print "2".

            int[] numbers = new int[] { 1, 4, 5, 4, 2, 7, 8, 5 };
            // Uncomment this to test your implementation:
            //Question3.NumberOfDuplicates(numbers);
        }
    }
}
