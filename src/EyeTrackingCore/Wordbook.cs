using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeTrackingCore
{
    public enum Token
    {
        ShortRight, MediumRight, LongRight,
        ShortUp, MediumUp, LongUp,
        ShortLeft, MediumLeft, LongLeft,
        ShortDown, MediumDown, LongDown,

        Brief, Hold,

        Editor, SolutionExplorer, Output, Nothing
    }

    public class Wordbook
    {
        public List<Token> saccadeTokens = new List<Token>();
        private string saccadeBook = "";
        public string SaccadeBook
        {
            get { return this.saccadeBook; }
        }

        public List<Token> fixationTokens = new List<Token>();
        private string fixationBook = "";
        public string FixationBook
        {
            get { return this.fixationBook; }
        }

        public List<Token> vsLocationTokens = new List<Token>();
        private string vsLocationBook = "";
        public string VSLocationBook
        {
            get { return this.vsLocationBook; }
        }

        public Wordbook(List<Saccade> saccades)
        {
            saccadeBook = GenerateSaccadeBook(saccades);
        }

        public Wordbook(List<Fixation> fixations)
        {
            fixationBook = GenerateFixationBook(fixations);
            vsLocationBook = GenerateLocationBook(fixations);
        }

        public Wordbook(List<Fixation> fixations, List<Saccade> saccades)
        {
            saccadeBook = GenerateSaccadeBook(saccades);
            fixationBook = GenerateFixationBook(fixations);
            vsLocationBook = GenerateLocationBook(fixations);
        }

        private string GenerateSaccadeBook(List<Saccade> saccades)
        {
            string book = "";

            foreach (Saccade saccade in saccades)
            {
                Token saccadeToken = TokenForSaccade(saccade);
                saccadeTokens.Add(saccadeToken);

                book = string.Concat(book, TokenToString(saccadeToken));
            }

            return book;
        }

        private string GenerateFixationBook(List<Fixation> fixations)
        {
            string book = "";

            foreach (Fixation fixation in fixations)
            {
                Token fixationToken = TokenForFixation(fixation);
                fixationTokens.Add(fixationToken);

                book = string.Concat(book, TokenToString(fixationToken));
            }

            return book;
        }

        private string GenerateLocationBook(List<Fixation> fixations)
        {
            string book = "";

            foreach (Fixation fixation in fixations)
            {
                Token locationToken = TokenForLocation(fixation.location);
                vsLocationTokens.Add(locationToken);

                book = string.Concat(book, TokenToString(locationToken));
            }

            return book;
        }

        private Token TokenForSaccade(Saccade saccade)
        {
            string saccadeType = Enum.GetName(typeof(SaccadeType), saccade.Type);
            string sector = Enum.GetName(typeof(Sector), saccade.Sector4);
            string tokenName = String.Format("{0}{1}", saccadeType, sector);

            Token result;
            Enum.TryParse<Token>(tokenName, out result);

            return result;
        }

        private Token TokenForFixation(Fixation fixation)
        {
            return (fixation.endTime - fixation.startTime) < 500 ? Token.Brief : Token.Hold;
        }
        
        private Token TokenForLocation(VSLocation location)
        {
            Token resultToken = Token.Nothing;

            switch(location)
            {
                case VSLocation.Editor:
                    resultToken = Token.Editor;
                    break;
                case VSLocation.Output:
                    resultToken = Token.Output;
                    break;
                case VSLocation.SolutionExplorer:
                    resultToken = Token.SolutionExplorer;
                    break;
                case VSLocation.Nothing:
                    resultToken = Token.Nothing;
                    break;
            }

            return resultToken;
        }

        private Dictionary<Token, string> TokenStringRepresentationTable =
            new Dictionary<Token, string>
            {
                [Token.ShortRight] = "Sr", [Token.MediumRight] = "Mr", [Token.LongRight] = "Lr",
                [Token.ShortUp] = "Su", [Token.MediumUp] = "Mu", [Token.LongUp] = "Lu",
                [Token.ShortLeft] = "Sl", [Token.MediumLeft] = "Ml", [Token.LongLeft] = "Ll",
                [Token.ShortDown] = "Sd", [Token.MediumDown] = "Md", [Token.LongDown] = "Ld",

                [Token.Brief] = "Br", [Token.Hold] = "Ho",

                [Token.Editor] = "Ed", [Token.SolutionExplorer] = "Se", [Token.Output] = "Ou", [Token.Nothing] = "No"
            };

        private string TokenToString(Token token)
        {
            return TokenStringRepresentationTable[token];
        }

        private IEnumerable<KeyValuePair<string, int>> SortedWordCount(List<Token> tokens, int wordSize)
        {
            Dictionary<string, int> wordCount = new Dictionary<string, int>();

            for (int i = 0; i <= tokens.Count - wordSize; i++)
            {
                string word = "";
                for (int windowPointer = 0; windowPointer < wordSize; windowPointer++)
                {
                    word = string.Concat(word, TokenToString(tokens[i + windowPointer]));
                }

                IncrementCountForWord(wordCount, word);
            }

            return wordCount.OrderByDescending(x => x.Value);
        }

        public IEnumerable<KeyValuePair<string, int>> SortedSaccadeWordCount(int wordSize)
        {
            return SortedWordCount(saccadeTokens, wordSize);
        }

        public IEnumerable<KeyValuePair<string, int>> SortedFixationWordCount(int wordSize)
        {
            return SortedWordCount(fixationTokens, wordSize);
        }

        public IEnumerable<KeyValuePair<string, int>> SortedLocationWordCount(int wordSize)
        {
            return SortedWordCount(vsLocationTokens, wordSize);
        }

        private void IncrementCountForWord(Dictionary<string, int> dict, string word)
        {
            if(dict.ContainsKey(word))
            {
                dict[word] = dict[word] + 1;
            }
            else
            {
                dict[word] = 1;
            }
        }




        // Local Alignment / Local Edit Distance 

        // The weights for different actions.
        int deletion = -1; // left
        int insertion = -1; // top
        int replacement = -1; // top-left if x[i-1] != y[j-1]
        int match = 1; // top-left if x[i - 1] == y[j - 1]

        public void LocalAlignment(string x, string y)
        {
            // Create table
            //#############
            int numColumns = x.Length + 1;
            int numRows = y.Length + 1;
            int[,] table = new int[numColumns, numRows];
            
            // Zero out first row and column.
            for (int column = 0; column < numColumns; column++)
            {
                table[column, 0] = 0;
            }

            for (int row = 0; row < numRows; row++)
            {
                table[0, row] = 0;
            }

            // Starting at (1, 1) going left to right, to bottom fill in the spaces in the table.
            for (int row = 1; row < numRows; row++)
            {
                for (int column = 1; column < numColumns; column++)
                {
                    int left = table[column - 1, row] + deletion;
                    int top = table[column, row - 1] + insertion;
                    int topLeft = table[column - 1, row - 1] + (x[column - 1] == y[row - 1] ? match : replacement);

                    table[column, row] = Max(left, top, topLeft, 0);
                }
            }

            //PrintTable(table, numColumns, numRows);
            Tuple<int, int> maxLocation = FindMaxInTable(table, numColumns, numRows);

            /* Find an print the best substring match
            Tuple<int, int> subStringPosition = FindMatchingSubstring(table, maxLocation, x, y);
            int subStringLength = subStringPosition.Item2 - subStringPosition.Item1;
            Console.WriteLine(y.Substring(subStringPosition.Item1, subStringLength));
            */

            // Display all substring matches that are above the threshold
            var locations = FindMatchLocationsAboveThreshold(table, numColumns, numRows, x.Length - 2);

            foreach(var location in locations)
            {
                Tuple<int, int> subStringPosition = FindMatchingSubstring(table, location, x, y);
                int subStringLength = subStringPosition.Item2 - subStringPosition.Item1;
                Console.WriteLine(y.Substring(subStringPosition.Item1, subStringLength));
            }
        }

        // first int is starting index of substring, second int is index of end of substring
        private Tuple<int, int> FindMatchingSubstring(int[,] table, Tuple<int, int> maxLocation, string x, string y)
        {
            int column = maxLocation.Item1;
            int row = maxLocation.Item2;

            bool searching = true;

            while(searching)
            {
                // Keep searching until we git a 0 in the table.
                if (table[column, row] == 0)
                {
                    searching = false;
                    break;
                }

                // Otherwise, keep moving up the table following the path we took to get here.
                int topLeft = table[column - 1, row - 1] + (x[column - 1] == y[row - 1] ? match : replacement);
                if(table[column, row] == topLeft)
                {
                    column--;
                    row--;
                    continue;
                }

                int left = table[column - 1, row] + deletion;
                int top = table[column, row - 1] + insertion;
                if (table[column, row] == left)
                {
                    column--;
                    continue;
                }
                
                if(table[column, row] == top)
                {
                    row--;
                    continue;
                }
            }

            return Tuple.Create(row, maxLocation.Item2);
        }

        private List<Tuple<int, int>> FindMatchLocationsAboveThreshold(int[,] table, int numColumns, int numRows, int threshold)
        {
            List<Tuple<int, int>> locations = new List<Tuple<int, int>>();

            for (int row = 0; row < numRows; row++)
            {
                for (int column = 0; column < numColumns; column++)
                {
                    if(table[column, row] >= threshold)
                    {
                        locations.Add(Tuple.Create(column, row));
                    }
                }
            }

            return locations;
        }

        private Tuple<int, int> FindMaxInTable(int[,] table, int numColumns, int numRows)
        {
            Tuple<int, int> maxLocation = new Tuple<int, int>(0,0);

            int max = 0;

            for (int row = 0; row < numRows; row++)
            {
                for (int column = 0; column < numColumns; column++)
                {
                    if(table[column, row] > max)
                    {
                        max = table[column, row];
                        maxLocation = Tuple.Create(column, row);
                    }
                }
            }

            return maxLocation;
        }

        private int Max(int w, int x, int y, int z)
        {
            return Math.Max(w, Math.Max(x, Math.Max(y, z)));
        }

        private void PrintTable(int[,] table, int columns, int rows)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Console.Write(String.Format("{0}, ", table[column, row]));
                }

                Console.WriteLine();
            }
        }

    }
}
