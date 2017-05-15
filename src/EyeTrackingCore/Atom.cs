using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO:
// Other features: Distractions (off-screen left, off-screen down, etc.), Brief to Hold ratio, Area of 95% of fixations, ...

namespace EyeTrackingCore
{
    public class AtomBook
    {
        private readonly Wordbook wordbook;
        
        public readonly AtomType[] ScanningTypes = { AtomType.ScanRightUp, AtomType.ScanUpLeft, AtomType.ScanLeftDown, AtomType.ScanDownRight,
                                                     AtomType.ScanHorizontal, AtomType.ScanVertical, AtomType.ScanBox };
        public readonly AtomType[] StringTypes = { AtomType.StringUp, AtomType.StringRight, AtomType.StringDown, AtomType.StringLeft };
        public readonly AtomType[] ComparisonTypes = { AtomType.CompareHorizontal, AtomType.CompareVertical };
        public readonly AtomType[] LineTypes = { AtomType.MediumLine, AtomType.LongLine };

        public readonly Dictionary<AtomType, List<Atom>> atoms = new Dictionary<AtomType, List<Atom>>();

        public AtomBook(Wordbook wordbook)
        {
            this.wordbook = wordbook;
            FindAtoms();
        }

        private void FindAtoms()
        {
            foreach(AtomType type in LineTypes) // TODO, once implemented all delta tables need to find atoms for all atomtypes, not just line types.
            {
                List<Token> x = AtomDeltaTables.AtomDefinitions[type];
                List<Token> y = wordbook.saccadeTokens;
                Dictionary<DeltaKey, int> delta = AtomDeltaTables.DeltaDefinitions[type]();
                int threshold = AtomDeltaTables.ThresholdDefinitions[type];

                List<Tuple<int, int>> atomLocations = Matching.GetLocationsOfLocalMatches(x, y, delta, threshold);

                foreach(Tuple<int, int> location in atomLocations)
                {
                    Atom atom = new Atom(type, wordbook.saccadesUsedToGenerateBook.GetRange(location.Item1, location.Item2));
                    InsertAtom(atom, atoms);
                }
            }
        }

        private void InsertAtom(Atom atom, Dictionary<AtomType, List<Atom>> dictionary)
        {
            if (dictionary.ContainsKey(atom.type))
            {
                dictionary[atom.type].Add(atom); // just add it to the list already there.
            }
            else
            {
                // hasnt been seen, have to create a list and this atom will be the first item in the list.
                List<Atom> list = new List<Atom>();
                list.Add(atom);
                dictionary[atom.type] = list;
            }
        }

        // AtomBook.NumberOfScans
        // AtomBook.NumberOfLines
        // AtomBook.NumberOfComparisons
        // AtomBook.NumberOfStrings
    }

    public class Atom
    {
        public readonly AtomType type;
        public readonly List<Saccade> saccades;

        // AtomType
        // Corresponding Saccades
        public Atom(AtomType type, List<Saccade> correspondingSaccades)
        {
            this.type = type;
            this.saccades = correspondingSaccades; // Where did we find this atom?
        }
    }

    public enum AtomType
    {
        // scanning
        ScanRightUp, ScanUpLeft, ScanLeftDown, ScanDownRight,
        ScanHorizontal, ScanVertical,
        ScanBox,

        // strings
        StringUp, StringRight, StringDown, StringLeft,

        // comparisons
        CompareHorizontal, CompareVertical,

        // lines
        ShortLine, MediumLine, LongLine
    }

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

    class AtomDeltaTables
    {
        public static Dictionary<AtomType, List<Token>> AtomDefinitions = new Dictionary<AtomType, List<Token>>
        {
            [AtomType.MediumLine] = new List<Token> { Token.ShortRight, Token.ShortRight, Token.ShortRight, Token.ShortRight, Token.MediumLeft },
            [AtomType.LongLine] = new List<Token> { Token.ShortRight, Token.ShortRight, Token.ShortRight, Token.ShortRight, Token.LongLeft }

            // Add other atom definitions here for all atomtypes.
        };

        public delegate Dictionary<DeltaKey, int> TableCreator();

        // need a atomdeltatable dictionary that goes from atomtype to a function of type (void -> dict<DeltaKey, int>) which returns the
        // appropriate weighting table for that atomtype.
        public static Dictionary<AtomType, TableCreator> DeltaDefinitions = new Dictionary<AtomType, TableCreator>
        {
            [AtomType.MediumLine] = CreateMediumLineDeltaTable,
            [AtomType.LongLine] = CreateLongLineDeltaTable

            // Add other atom definitions here for all atomtypes.
        };

        public static Dictionary<AtomType, int> ThresholdDefinitions = new Dictionary<AtomType, int>
        {
            [AtomType.MediumLine] = 21,
            [AtomType.LongLine] = 21

            // Add other atom definitions here for all atomtypes.
        };

        // Tokens we care about for finding atoms.
        public static List<Token> tokens = new List<Token> { Token.ShortRight, Token.MediumRight, Token.LongRight,
                                                          Token.ShortUp, Token.MediumUp, Token.LongUp,
                                                          Token.ShortLeft, Token.MediumLeft, Token.LongLeft,
                                                          Token.ShortDown, Token.MediumDown, Token.LongDown };

        // Returns a delta table all initialised to -1.
        public static Dictionary<DeltaKey, int> CreateInitialTable(int initialValue)
        {
            Dictionary<DeltaKey, int> deltaTable = new Dictionary<DeltaKey, int>();

            // Add all possibilities
            foreach (Token x in tokens)
            {
                foreach (Token y in tokens)
                {
                    DeltaKey key = new DeltaKey(Wordbook.TokenToString(x), Wordbook.TokenToString(y));
                    deltaTable[key] = initialValue;
                }
            }

            // Add epsilons for deletion and insertions
            foreach (Token y in tokens)
            {
                DeltaKey key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -1;
            }

            foreach (Token x in tokens)
            {
                DeltaKey key = new DeltaKey(Wordbook.TokenToString(x), "");
                deltaTable[key] = -1;
            }

            return deltaTable;
        }

        public static Dictionary<DeltaKey, int> CreateShortLineDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            DeltaKey key = new DeltaKey("Sr", "Sr");
            deltaTable[key] = 5;

            key = new DeltaKey("Sr", "Mr");
            deltaTable[key] = 2;

            key = new DeltaKey("Sl", "Sl");
            deltaTable[key] = 10;

            key = new DeltaKey("Sl", "");
            deltaTable[key] = -10;

            foreach (Token y in tokens)
            {
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -10;
            }

            return deltaTable;
        } // SrSrSrSl

        public static Dictionary<DeltaKey, int> CreateMediumLineDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            DeltaKey key = new DeltaKey("Sr", "Sr");
            deltaTable[key] = 5;

            key = new DeltaKey("Sr", "Mr");
            deltaTable[key] = 2;

            key = new DeltaKey("Sr", "Sl");
            deltaTable[key] = 0;

            key = new DeltaKey("Ml", "Ml");
            deltaTable[key] = 10;

            key = new DeltaKey("Ml", "");
            deltaTable[key] = -10;

            foreach (Token y in tokens)
            {
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -10;
            }

            return deltaTable;
        } // SrSrSrSrLl

        public static Dictionary<DeltaKey, int> CreateLongLineDeltaTable()
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
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -10;
            }

            return deltaTable;
        } // SrSrSrSrLl

        public static Dictionary<DeltaKey, int> CreateHorizontalFocalPointDeltaTable()
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
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -10;

                // Allow some deletions
                key = new DeltaKey(Wordbook.TokenToString(y), "");
                deltaTable[key] = -10;
            }

            return deltaTable;
        } // SrSlSlSr

        public static Dictionary<DeltaKey, int> CreateVerticalFocalPointDeltaTable()
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
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -10;
            }

            return deltaTable;
        } // SuSdSdSu

        public static Dictionary<DeltaKey, int> CreateRightStringDeltaTable()
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
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -10;

                // Penalise any kind of deletion
                key = new DeltaKey(Wordbook.TokenToString(y), "");
                deltaTable[key] = -10;
            }

            return deltaTable;
        } // SrSrSrSr

        // scanning

        // scanRightUp
        // scanUpLeft
        // scanLeftDown
        // scanDownRight

        // scanHorizontal
        // scanVertical

        // box-scan

        // comparisons

        // horizontal-compare
        // vertical-compare

        // strings

        // up-string
        // right-string
        // down-string
        // left-string

        // lines

        // long-line
        // medium-line
    }

    class Matching
    {
        // Local Alignment for ATOMS via Smith–Waterman algorithm

        // Returns a list tuples that show the position (startIndex, length) of every match of x in the longer string y, (that is above the threshold).
        static public List<Tuple<int, int>> GetLocationsOfLocalMatches(List<Token> x, List<Token> y, Dictionary<DeltaKey, int> delta, int threshold)
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
                    int left = table[column - 1, row] + delta[new DeltaKey(Wordbook.TokenToString(x[column - 1]), "")]; // deletion
                    int top = table[column, row - 1] + delta[new DeltaKey("", Wordbook.TokenToString(y[row - 1]))]; ; // insertion
                    int topLeft = table[column - 1, row - 1] + delta[new DeltaKey(Wordbook.TokenToString(x[column - 1]), Wordbook.TokenToString(y[row - 1]))]; // match or replace

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

        static private Tuple<int, int> FindMatchingSubstring(int[,] table, Tuple<int, int> maxLocation, List<Token> x, List<Token> y, Dictionary<DeltaKey, int> delta)
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
                int topLeft = table[column - 1, row - 1] + delta[new DeltaKey(Wordbook.TokenToString(x[column - 1]), Wordbook.TokenToString(y[row - 1]))]; // match or replace
                if (table[column, row] == topLeft)
                {
                    column--;
                    row--;
                    continue;
                }

                int left = table[column - 1, row] + delta[new DeltaKey(Wordbook.TokenToString(x[column - 1]), "")]; // deletion
                int top = table[column, row - 1] + delta[new DeltaKey("", Wordbook.TokenToString(y[row - 1]))]; // insertion
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

        static private List<Tuple<int, int>> FindMatchLocationsAboveThreshold(int[,] table, int numColumns, int numRows, int threshold)
        {
            List<Tuple<int, int>> locations = new List<Tuple<int, int>>();

            for (int row = 0; row < numRows; row++)
            {
                for (int column = 0; column < numColumns; column++)
                {
                    if (table[column, row] >= threshold)
                    {
                        locations.Add(Tuple.Create(column, row));
                    }
                }
            }

            return locations;
        }

        static private Tuple<int, int> FindMaxInTable(int[,] table, int numColumns, int numRows)
        {
            Tuple<int, int> maxLocation = new Tuple<int, int>(0, 0);
            int max = 0;

            for (int row = 0; row < numRows; row++)
            {
                for (int column = 0; column < numColumns; column++)
                {
                    if (table[column, row] > max)
                    {
                        max = table[column, row];
                        maxLocation = Tuple.Create(column, row);
                    }
                }
            }

            return maxLocation;
        }

        static private int Max(int w, int x, int y, int z)
        {
            return Math.Max(w, Math.Max(x, Math.Max(y, z)));
        }

        static private void PrintTable(int[,] table, int columns, int rows)
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
