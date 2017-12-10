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
            // 1. Print the sum of the numbers 1, 2, 3, 4, 5, 6.
            // Write the code in the file "SumNumbers.cs"

            // Uncomment this to test your implementation:
            // Question1.SumNumbers(new int[] { 1, 2, 3, 4, 5, 6 });

            // ######################################################################
            // 2. For the numbers 1 through 10, print the corresponding english word. For 
            // example:
            // int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            // PrintNumbers(numbers) should output "one, two, three, four" and so on.
            // Write the code in the file "PrintNumbers.cs"

            // Uncomment this to test your implementation:
            // Question2.PrintNumbers(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            // ######################################################################
            // 3. Given a string in camelcase, return the number of words. 
            // "saveChangedInTheEditor", is an example of a camelcase string where the first
            // word is all lowercase and each consecutive word starts with an uppercase letter.
            // Write the code in the file "CamelCase.cs"

            string words = "howManyWordsAreInThisExample";

            // Uncomment this to test your implementation:
            //Question3.NumberOfWordsInCamelCase(words);
        }
    }
}
