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












        // Local Alignment for ATOMS

        public List<Token> tokens = new List<Token> { Token.ShortRight, Token.MediumRight, Token.LongRight,
                                                      Token.ShortUp, Token.MediumUp, Token.LongUp,
                                                      Token.ShortLeft, Token.MediumLeft, Token.LongLeft,
                                                      Token.ShortDown, Token.MediumDown, Token.LongDown };

        

        

        public struct DeltaKey
        {
            public readonly string x;
            public readonly string y;

            public DeltaKey(string x, string y)
            {
                this.x = x;
                this.y = y;
            }
        }

        // Printing version
        public void LocalAlignment(List<Token> x, List<Token> y, Dictionary<DeltaKey, int> delta, int threshold)
        {
            // Create table
            //#############
            int numColumns = x.Count + 1;
            int numRows = y.Count + 1;
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
                    int left = table[column - 1, row] + delta[new DeltaKey(TokenToString(x[column-1]), "")]; // deletion
                    int top = table[column, row - 1] + delta[new DeltaKey("", TokenToString(y[row - 1]))]; ; // insertion
                    int topLeft = table[column - 1, row - 1] + delta[new DeltaKey(TokenToString(x[column - 1]), TokenToString(y[row - 1]))]; // match or replace

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
            var locations = FindMatchLocationsAboveThreshold(table, numColumns, numRows, threshold);

            foreach (var location in locations)
            {
                Tuple<int, int> subStringPosition = FindMatchingSubstring(table, location, x, y, delta);
                int subStringLength = subStringPosition.Item2 - subStringPosition.Item1;

                List<Token> match = y.GetRange(subStringPosition.Item1, subStringLength);
                string matched = match.Aggregate("", (acc, next) => string.Concat(acc, TokenToString(next)));

                Console.WriteLine(matched);
            }
        }

        // Get locations version
        public List<Tuple<int, int>> GetLocationsOfLocalMatches(List<Token> x, List<Token> y, Dictionary<DeltaKey, int> delta, int threshold)
        {
            // Create table
            //#############
            int numColumns = x.Count + 1;
            int numRows = y.Count + 1;
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
                    int left = table[column - 1, row] + delta[new DeltaKey(TokenToString(x[column - 1]), "")]; // deletion
                    int top = table[column, row - 1] + delta[new DeltaKey("", TokenToString(y[row - 1]))]; ; // insertion
                    int topLeft = table[column - 1, row - 1] + delta[new DeltaKey(TokenToString(x[column - 1]), TokenToString(y[row - 1]))]; // match or replace

                    table[column, row] = Max(left, top, topLeft, 0);
                }
            }

            //PrintTable(table, numColumns, numRows);
            Tuple<int, int> maxLocation = FindMaxInTable(table, numColumns, numRows);

            // Display all substring matches that are above the threshold
            var startingLocations = FindMatchLocationsAboveThreshold(table, numColumns, numRows, threshold);
            List<Tuple<int, int>> subStringLocations = new List<Tuple<int, int>>();

            foreach (var location in startingLocations)
            {
                Tuple<int, int> subStringPosition = FindMatchingSubstring(table, location, x, y, delta);
                int subStringLength = subStringPosition.Item2 - subStringPosition.Item1;

                Tuple<int, int> subStringLocation = new Tuple<int, int>(subStringPosition.Item1, subStringLength);
                subStringLocations.Add(subStringLocation);
            }

            return subStringLocations;
        }

        private Tuple<int, int> FindMatchingSubstring(int[,] table, Tuple<int, int> maxLocation, List<Token> x, List<Token> y, Dictionary<DeltaKey, int> delta)
        {
            int column = maxLocation.Item1;
            int row = maxLocation.Item2;

            bool searching = true;

            while (searching)
            {
                // Keep searching until we git a 0 in the table.
                if (table[column, row] == 0)
                {
                    searching = false;
                    break;
                }

                // Otherwise, keep moving up the table following the path we took to get here.
                int topLeft = table[column - 1, row - 1] + delta[new DeltaKey(TokenToString(x[column - 1]), TokenToString(y[row - 1]))]; // match or replace
                if (table[column, row] == topLeft)
                {
                    column--;
                    row--;
                    continue;
                }

                int left = table[column - 1, row] + delta[new DeltaKey(TokenToString(x[column - 1]), "")]; // deletion
                int top = table[column, row - 1] + delta[new DeltaKey("", TokenToString(y[row - 1]))]; // insertion
                if (table[column, row] == left)
                {
                    column--;
                    continue;
                }

                if (table[column, row] == top)
                {
                    row--;
                    continue;
                }
            }

            return Tuple.Create(row, maxLocation.Item2);
        }












        // Returns a delta table all initialised to -1.
        public Dictionary<DeltaKey, int> CreateInitialTable(int initialValue)
        {
            Dictionary<DeltaKey, int> deltaTable = new Dictionary<DeltaKey, int>();

            // Add all possibilities
            foreach (Token x in tokens)
            {
                foreach (Token y in tokens)
                {
                    DeltaKey key = new DeltaKey(TokenToString(x), TokenToString(y));
                    deltaTable[key] = initialValue;
                }
            }

            // Add epsilons for deletion and insertions
            foreach (Token y in tokens)
            {
                DeltaKey key = new DeltaKey("", TokenToString(y));
                deltaTable[key] = -1;
            }

            foreach (Token x in tokens)
            {
                DeltaKey key = new DeltaKey(TokenToString(x), "");
                deltaTable[key] = -1;
            }

            return deltaTable;
        }


        public Dictionary<DeltaKey, int> CreateLineDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            DeltaKey key = new DeltaKey("Sr", "Sr");
            deltaTable[key] = 5;

            key = new DeltaKey("Sr", "Mr");
            deltaTable[key] = 2;

            key = new DeltaKey("Sr", "Sl");
            deltaTable[key] = 0;

            key = new DeltaKey("Ll", "Ll");
            deltaTable[key] = 10;

            key = new DeltaKey("Ll", "");
            deltaTable[key] = -10;

            foreach (Token y in tokens)
            {
                key = new DeltaKey("", TokenToString(y));
                deltaTable[key] = -10;
            }

            /*
            key = new DeltaKey("", "Sr");
            deltaTable[key] = -5;

            key = new DeltaKey("", "Sl");
            deltaTable[key] = -5;

            key = new DeltaKey("", "Ll");
            deltaTable[key] = -10;
            */

            return deltaTable;
        }


        public Dictionary<DeltaKey, int> CreateHorizontalFocalPointDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-5);

            // Want to match lefts and rights mostly
            DeltaKey key = new DeltaKey("Sr", "Sr");
            deltaTable[key] = 10;

            key = new DeltaKey("Sl", "Sl");
            deltaTable[key] = 10;

            // But we can allow Sl to be Ml and Sr to be Mr
            key = new DeltaKey("Sr", "Mr");
            deltaTable[key] = 8;

            key = new DeltaKey("Sl", "Ml");
            deltaTable[key] = 8;

            foreach (Token y in tokens)
            {
                // Penalise any kind of insertions
                key = new DeltaKey("", TokenToString(y));
                deltaTable[key] = -10;

                // Allow some deletions
                key = new DeltaKey(TokenToString(y), "");
                deltaTable[key] = -10;
            }

            return deltaTable;
        }

        public Dictionary<DeltaKey, int> CreateVerticalFocalPointDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-5);

            // Want to match lefts and rights mostly
            DeltaKey key = new DeltaKey("Su", "Su");
            deltaTable[key] = 10;

            key = new DeltaKey("Sd", "Sd");
            deltaTable[key] = 10;

            // But we can allow Sl to be Ml and Sr to be Mr
            key = new DeltaKey("Su", "Mu");
            deltaTable[key] = 8;

            key = new DeltaKey("Sd", "Md");
            deltaTable[key] = 8;

            foreach (Token y in tokens)
            {
                // Penalise any kind of insertions
                key = new DeltaKey("", TokenToString(y));
                deltaTable[key] = -10;
            }

            return deltaTable;
        }


        // SrSrSrSr
        public Dictionary<DeltaKey, int> CreateRightStringDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            // Want to match lefts and rights mostly
            DeltaKey key = new DeltaKey("Sr", "Sr");
            deltaTable[key] = 2;

            // Backwards is free
            key = new DeltaKey("Sr", "Sl");
            deltaTable[key] = 1;

            // Short ups and downs are okay
            key = new DeltaKey("Sr", "Su");
            deltaTable[key] = 1;

            key = new DeltaKey("Sr", "Sd");
            deltaTable[key] = 1;

            foreach (Token y in tokens)
            {
                // Penalise any kind of insertions
                key = new DeltaKey("", TokenToString(y));
                deltaTable[key] = -10;

                // Penalise any kind of deletion
                key = new DeltaKey(TokenToString(y), "");
                deltaTable[key] = -10;
            }

            return deltaTable;
        }
    }
}
