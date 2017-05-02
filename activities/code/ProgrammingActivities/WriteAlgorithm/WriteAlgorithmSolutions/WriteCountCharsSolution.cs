using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WriteAlgorithmSolutions
{
    class WriteCountCharsSolution
    {
        public static int CountChars(char c, string s)
        {
            // s.Length gives you the number of characters in a string.
            // s[0] gives you first character in a string.

            int count = 0;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == c)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
