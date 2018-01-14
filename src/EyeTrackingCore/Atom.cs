using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO:
// Other features: Area of 95% of fixations, ...

namespace EyeTrackingCore
{
    public class AtomBook
    {
        private Wordbook wordbook;

        // Strings
        public int numberOfStringUp = 0;
        public int numberOfStringRight = 0;
        public int numberOfStringDown = 0;
        public int numberOfStringLeft = 0;
        
        // Lines
        public int numberOfMediumLines = 0;
        public int numberOfLongLines = 0;

        // Compares
        public int numberOfCompareHorizontal = 0;
        public int numberOfCompareVertical = 0;
        public int numberOfCompareHorizontalAlt = 0;
        public int numberOfCompareVerticalAlt = 0;
        
        // Scans
        public int numberOfScanRightUp = 0;
        public int numberOfScanUpLeft = 0;
        public int numberOfScanLeftDown = 0;
        public int numberOfScanDownRight = 0;
        public int numberOfScanRightUpAlt = 0;
        public int numberOfScanUpLeftAlt = 0;
        public int numberOfScanLeftDownAlt = 0;
        public int numberOfScanDownRightAlt = 0;
        
        public int numberOfScanHorizontal = 0;
        public int numberOfScanVertical = 0;
        public int numberOfScanHorizontalAlt = 0;
        public int numberOfScanVerticalAlt = 0;




        private int numberOfScans = 0;
        private int numberOfStrings = 0;
        private int numberOfComparisons = 0;
        private int numberOfLines = 0;

        public readonly AtomType[] StringTypes = { AtomType.StringUp, AtomType.StringRight, AtomType.StringDown, AtomType.StringLeft };
        public readonly AtomType[] LineTypes = { AtomType.MediumLine, AtomType.LongLine };
        public readonly AtomType[] ComparisonTypes = { AtomType.CompareHorizontal, AtomType.CompareVertical, AtomType.CompareHorizontalAlt, AtomType.CompareVerticalAlt };
        public readonly AtomType[] ScanningTypes = { AtomType.ScanRightUp, AtomType.ScanUpLeft, AtomType.ScanLeftDown, AtomType.ScanDownRight,
                                                     AtomType.ScanHorizontal, AtomType.ScanVertical,
                                                     AtomType.ScanRightUpAlt, AtomType.ScanUpLeftAlt, AtomType.ScanLeftDownAlt, AtomType.ScanDownRightAlt,
                                                     AtomType.ScanHorizontalAlt, AtomType.ScanVerticalAlt};

        public readonly Dictionary<AtomType, List<Atom>> atoms = new Dictionary<AtomType, List<Atom>>();

        public AtomBook(Wordbook wordbook)
        {
            this.wordbook = wordbook;
            this.numberOfStrings = FindAtoms(StringTypes);
            this.numberOfLines = FindAtoms(LineTypes);
            this.numberOfComparisons = FindAtoms(ComparisonTypes);
            this.numberOfScans = FindAtoms(ScanningTypes);

            // Strings
            numberOfStringUp = FindAtoms(new AtomType[] { AtomType.StringUp });
            numberOfStringRight = FindAtoms(new AtomType[] { AtomType.StringRight }); ;
            numberOfStringDown = FindAtoms(new AtomType[] { AtomType.StringDown }); ;
            numberOfStringLeft = FindAtoms(new AtomType[] { AtomType.StringLeft }); ;
            
            numberOfMediumLines = FindAtoms(new AtomType[] { AtomType.MediumLine }); ;
            numberOfLongLines = FindAtoms(new AtomType[] { AtomType.LongLine }); ;
            
            numberOfCompareHorizontal = FindAtoms(new AtomType[] { AtomType.CompareHorizontal }); ;
            numberOfCompareVertical = FindAtoms(new AtomType[] { AtomType.CompareVertical }); ;
            numberOfCompareHorizontalAlt = FindAtoms(new AtomType[] { AtomType.CompareHorizontalAlt }); ;
            numberOfCompareVerticalAlt = FindAtoms(new AtomType[] { AtomType.CompareVerticalAlt }); ;
            
            numberOfScanRightUp = FindAtoms(new AtomType[] { AtomType.ScanRightUp }); ;
            numberOfScanUpLeft = FindAtoms(new AtomType[] { AtomType.ScanUpLeft }); ;
            numberOfScanLeftDown = FindAtoms(new AtomType[] { AtomType.ScanLeftDown }); ;
            numberOfScanDownRight = FindAtoms(new AtomType[] { AtomType.ScanDownRight }); ;
            numberOfScanRightUpAlt = FindAtoms(new AtomType[] { AtomType.ScanRightUpAlt }); ;
            numberOfScanUpLeftAlt = FindAtoms(new AtomType[] { AtomType.ScanUpLeftAlt }); ;
            numberOfScanLeftDownAlt = FindAtoms(new AtomType[] { AtomType.ScanLeftDownAlt }); ;
            numberOfScanDownRightAlt = FindAtoms(new AtomType[] { AtomType.ScanDownRightAlt }); ;

            numberOfScanHorizontal = FindAtoms(new AtomType[] { AtomType.ScanHorizontal }); ;
            numberOfScanVertical = FindAtoms(new AtomType[] { AtomType.ScanVertical }); ;
            numberOfScanHorizontalAlt = FindAtoms(new AtomType[] { AtomType.ScanHorizontalAlt }); ;
            numberOfScanVerticalAlt = FindAtoms(new AtomType[] { AtomType.ScanVerticalAlt }); ;
        }

        // Stores atoms it finds in the 'atoms' dictionary under its type and returns the total number of atoms it found.
        private int FindAtoms(AtomType[] types)
        {
            int count = 0;

            foreach (AtomType type in types)
            {
                if (AtomDeltaTables.AtomDefinitions.Keys.Contains(type)) {
                    List<Token> x = AtomDeltaTables.AtomDefinitions[type];
                    List<Token> y = wordbook.saccadeTokens;
                    Dictionary<DeltaKey, int> delta = AtomDeltaTables.DeltaDefinitions[type]();
                    int threshold = AtomDeltaTables.ThresholdDefinitions[type];

                    List<Tuple<int, int>> atomLocations = Matching.GetLocationsOfLocalMatches(x, y, delta, threshold);

                    if (atomLocations.Count == 0)
                    {
                        // there were no instances of this atom found in the data.
                        // just set an empty dict
                        atoms[type] = new List<Atom>();
                    }
                    else
                    {
                        // We actually found some instances of this atom.
                        foreach (Tuple<int, int> location in atomLocations)
                        {
                            Atom atom = new Atom(type, wordbook.saccadesUsedToGenerateBook.GetRange(location.Item1, location.Item2));
                            InsertAtom(atom, atoms);
                            count++;
                        }
                    }
                }
            }

            return count;
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

        public int NumberOfStrings
        {
            get { return this.numberOfStrings; }
        }
        public int NumberOfLines
        {
            get { return this.numberOfLines; }
        }
        public int NumberOfComparisons
        {
            get { return this.numberOfComparisons; }
        }
        public int NumberOfScans
        {
            get { return this.numberOfScans; }
        }
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
        // String Atoms
        StringUp, StringRight, StringDown, StringLeft,

        // Line Atoms
        ShortLine, MediumLine, LongLine,
        
        // Compare Atoms
        CompareHorizontal, CompareVertical,
        CompareHorizontalAlt, CompareVerticalAlt,

        // Scanning Atoms
        ScanRightUp, ScanUpLeft, ScanLeftDown, ScanDownRight,
        ScanHorizontal, ScanVertical,
        ScanRightUpAlt, ScanUpLeftAlt, ScanLeftDownAlt, ScanDownRightAlt,
        ScanHorizontalAlt, ScanVerticalAlt
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
            // String Atoms
            [AtomType.StringUp] = new List<Token> { Token.ShortUp, Token.ShortUp, Token.ShortUp, Token.ShortUp },
            [AtomType.StringRight] = new List<Token> { Token.ShortRight, Token.ShortRight, Token.ShortRight, Token.ShortRight },
            [AtomType.StringDown] = new List<Token> { Token.ShortDown, Token.ShortDown, Token.ShortDown, Token.ShortDown },
            [AtomType.StringLeft] = new List<Token> { Token.ShortLeft, Token.ShortLeft, Token.ShortLeft, Token.ShortLeft },

            // Line Atoms
            [AtomType.MediumLine] = new List<Token> { Token.ShortRight, Token.ShortRight, Token.ShortRight, Token.ShortRight, Token.MediumLeft },
            [AtomType.LongLine] = new List<Token> { Token.ShortRight, Token.ShortRight, Token.ShortRight, Token.ShortRight, Token.LongLeft },

            // Compare Atoms
            [AtomType.CompareHorizontal] = new List<Token> { Token.ShortRight, Token.ShortLeft, Token.ShortRight, Token.ShortLeft },
            [AtomType.CompareVertical] = new List<Token> { Token.ShortUp, Token.ShortDown, Token.ShortUp, Token.ShortDown },
            [AtomType.CompareHorizontalAlt] = new List<Token> { Token.ShortLeft, Token.ShortRight, Token.ShortLeft, Token.ShortRight },
            [AtomType.CompareVerticalAlt] = new List<Token> { Token.ShortDown, Token.ShortUp, Token.ShortDown, Token.ShortUp},

            // Scanning Atoms
            [AtomType.ScanHorizontal] = new List<Token> { Token.ShortRight, Token.ShortLeft, Token.ShortLeft, Token.ShortRight },
            [AtomType.ScanVertical] = new List<Token> { Token.ShortUp, Token.ShortDown, Token.ShortDown, Token.ShortUp },
            [AtomType.ScanHorizontalAlt] = new List<Token> { Token.ShortLeft, Token.ShortRight, Token.ShortRight, Token.ShortLeft },
            [AtomType.ScanVerticalAlt] = new List<Token> { Token.ShortDown, Token.ShortUp, Token.ShortUp, Token.ShortDown },

            [AtomType.ScanRightUp] = new List<Token> { Token.ShortRight, Token.ShortLeft, Token.ShortUp, Token.ShortDown },
            [AtomType.ScanRightUpAlt] = new List<Token> { Token.ShortUp, Token.ShortDown, Token.ShortRight, Token.ShortLeft},

            [AtomType.ScanUpLeft] = new List<Token> { Token.ShortUp, Token.ShortDown, Token.ShortLeft, Token.ShortRight },
            [AtomType.ScanUpLeftAlt] = new List<Token> { Token.ShortLeft, Token.ShortRight, Token.ShortUp, Token.ShortDown },

            [AtomType.ScanLeftDown] = new List<Token> { Token.ShortLeft, Token.ShortRight, Token.ShortDown, Token.ShortUp },
            [AtomType.ScanLeftDownAlt] = new List<Token> { Token.ShortDown, Token.ShortUp, Token.ShortLeft, Token.ShortRight },

            [AtomType.ScanDownRight] = new List<Token> { Token.ShortDown, Token.ShortUp, Token.ShortRight, Token.ShortLeft },
            [AtomType.ScanDownRightAlt] = new List<Token> { Token.ShortRight, Token.ShortLeft, Token.ShortDown, Token.ShortUp }
        };

        public delegate Dictionary<DeltaKey, int> TableCreator();

        // need a atomdeltatable dictionary that goes from atomtype to a function of type (void -> dict<DeltaKey, int>) which returns the
        // appropriate weighting table for that atomtype.
        public static Dictionary<AtomType, TableCreator> DeltaDefinitions = new Dictionary<AtomType, TableCreator>
        {
            // String Atoms
            [AtomType.StringUp] = CreateStringUpDeltaTable,
            [AtomType.StringRight] = CreateStringRightDeltaTable,
            [AtomType.StringDown] = CreateStringDownDeltaTable,
            [AtomType.StringLeft] = CreateStringLeftDeltaTable,

            // Line Atoms
            [AtomType.MediumLine] = CreateMediumLineDeltaTable,
            [AtomType.LongLine] = CreateLongLineDeltaTable,

            // Compare Atoms
            [AtomType.CompareHorizontal] = CreateCompareHorizontalDeltaTable,
            [AtomType.CompareVertical] = CreateCompareVerticalDeltaTable,
            [AtomType.CompareHorizontalAlt] = CreateCompareHorizontalDeltaTable,
            [AtomType.CompareVerticalAlt] = CreateCompareVerticalDeltaTable,

            // Scanning Atoms
            [AtomType.ScanHorizontal] = CreateScanHorizontalDeltaTable,
            [AtomType.ScanVertical] = CreateScanVerticalDeltaTable,
            [AtomType.ScanHorizontalAlt] = CreateScanHorizontalDeltaTable,
            [AtomType.ScanVerticalAlt] = CreateScanVerticalDeltaTable,

            [AtomType.ScanRightUp] = CreateScanCornerDeltaTable,
            [AtomType.ScanRightUpAlt] = CreateScanCornerDeltaTable,

            [AtomType.ScanUpLeft] = CreateScanCornerDeltaTable,
            [AtomType.ScanUpLeftAlt] = CreateScanCornerDeltaTable,

            [AtomType.ScanLeftDown] = CreateScanCornerDeltaTable,
            [AtomType.ScanLeftDownAlt] = CreateScanCornerDeltaTable,

            [AtomType.ScanDownRight] = CreateScanCornerDeltaTable,
            [AtomType.ScanDownRightAlt] = CreateScanCornerDeltaTable
        };

        public static Dictionary<AtomType, int> ThresholdDefinitions = new Dictionary<AtomType, int>
        {
            // String Atoms
            [AtomType.StringUp] = 7,
            [AtomType.StringRight] = 7,
            [AtomType.StringDown] = 7,
            [AtomType.StringLeft] = 7,

            // Line Atoms
            [AtomType.MediumLine] = 21,
            [AtomType.LongLine] = 21,

            // Compare Atoms
            [AtomType.CompareHorizontal] = 7,
            [AtomType.CompareVertical] = 7,
            [AtomType.CompareHorizontalAlt] = 7,
            [AtomType.CompareVerticalAlt] = 7,

            // Scanning Atoms
            [AtomType.ScanHorizontal] = 4,
            [AtomType.ScanVertical] = 4,
            [AtomType.ScanHorizontalAlt] = 4,
            [AtomType.ScanVerticalAlt] = 4,

            [AtomType.ScanRightUp] = 4,
            [AtomType.ScanRightUpAlt] = 4,

            [AtomType.ScanUpLeft] = 4,
            [AtomType.ScanUpLeftAlt] = 4,

            [AtomType.ScanLeftDown] = 4,
            [AtomType.ScanLeftDownAlt] = 4,

            [AtomType.ScanDownRight] = 4,
            [AtomType.ScanDownRightAlt] = 4
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

        // String Atoms
        public static Dictionary<DeltaKey, int> CreateStringUpDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            // Want to match only ups. Perfect match would score 8.
            DeltaKey key = new DeltaKey("Su", "Su");
            deltaTable[key] = 2;

            // Replacing Su with any number of Mu is okay too.
            key = new DeltaKey("Su", "Mu");
            deltaTable[key] = 2;

            // Short left rights and downs are okay.
            key = new DeltaKey("Su", "Sl");
            deltaTable[key] = 1;
            
            key = new DeltaKey("Su", "Sr");
            deltaTable[key] = 1;

            key = new DeltaKey("Su", "Sd");
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
        } // SuSuSuSu
        public static Dictionary<DeltaKey, int> CreateStringRightDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            // Want to match only rights. Perfect match would score 8.
            DeltaKey key = new DeltaKey("Sr", "Sr");
            deltaTable[key] = 2;

            // Replacing Sr with any number of Mr is okay too.
            key = new DeltaKey("Sr", "Mr");
            deltaTable[key] = 2;

            // Replacing a short right in the string with a short left is okay.
            key = new DeltaKey("Sr", "Sl");
            deltaTable[key] = 1;

            // Short ups and downs are okay
            key = new DeltaKey("Sr", "Su");
            deltaTable[key] = 1;

            key = new DeltaKey("Sr", "Sd");
            deltaTable[key] = 1;

            // If there is one replacement of an Sr either with Sl Su Sd, and the rest match with Sr, then the matching score would be 7. 6 for two replacements, etc.
            // We want to allow only one replacement somewhere in the string. So we will want a threshold of 7 when finding matches.

            // todo: need a way to specify the first and last tokens in the atom cannot change
            //          perhaps something like Sr - - Sr, where Sr matching with Sr score is high and whatever the dummy - char is has an equally high score for being replace with Sr, whilst slightly lower score for being replaced with Su, Sl, Sd.

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
        } // SrSrSrSr, see notes for matching information
        public static Dictionary<DeltaKey, int> CreateStringDownDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            // Want to match only downs. Perfect match would score 8.
            DeltaKey key = new DeltaKey("Sd", "Sd");
            deltaTable[key] = 2;

            // Replacing Sd with any number of Md is okay too.
            key = new DeltaKey("Sd", "Md");
            deltaTable[key] = 2;

            // Short left rights and ups are okay.
            key = new DeltaKey("Sd", "Sl");
            deltaTable[key] = 1;

            key = new DeltaKey("Sd", "Sr");
            deltaTable[key] = 1;

            key = new DeltaKey("Sd", "Su");
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
        } // SdSdSdSd
        public static Dictionary<DeltaKey, int> CreateStringLeftDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            // Want to match only lefts. Perfect match would score 8.
            DeltaKey key = new DeltaKey("Sl", "Sl");
            deltaTable[key] = 2;

            // Replacing Sl with any number of Ml is okay too.
            key = new DeltaKey("Sl", "Ml");
            deltaTable[key] = 2;

            // Short left rights and ups are okay.
            key = new DeltaKey("Sl", "Sd");
            deltaTable[key] = 1;

            key = new DeltaKey("Sl", "Sr");
            deltaTable[key] = 1;

            key = new DeltaKey("Sl", "Su");
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
        } // SlSlSlSl

        // Line Atoms
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

        // Compare Atoms
        public static Dictionary<DeltaKey, int> CreateCompareHorizontalDeltaTable() // SrSlSrSl
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-2);

            // Want to match lefts and rights mostly, perfect match == 8
            DeltaKey key = new DeltaKey("Sr", "Sr");
            deltaTable[key] = 2;

            key = new DeltaKey("Sl", "Sl");
            deltaTable[key] = 2;

            foreach (Token y in tokens)
            {
                // Penalise any kind of insertions
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -2;

                // Penalise any kind of deletion
                key = new DeltaKey(Wordbook.TokenToString(y), "");
                deltaTable[key] = -2;
            }

            // Insertions of short ups and downs are okay
            key = new DeltaKey("", "Su");
            deltaTable[key] = -1;

            key = new DeltaKey("", "Sd");
            deltaTable[key] = -1;

            // Therefore, for one insertion of an up or down, the threshold will be 7 or higher.
            // Perfect match is 8, minus 1 for 1 insertion of an Su or Sd.

            return deltaTable;
        }
        public static Dictionary<DeltaKey, int> CreateCompareVerticalDeltaTable() // SuSdSuSd
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-2);

            // Want to match lefts and rights mostly, perfect match == 8
            DeltaKey key = new DeltaKey("Su", "Su");
            deltaTable[key] = 2;

            key = new DeltaKey("Sd", "Sd");
            deltaTable[key] = 2;

            foreach (Token y in tokens)
            {
                // Penalise any kind of insertions
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -2;

                // Penalise any kind of deletion
                key = new DeltaKey(Wordbook.TokenToString(y), "");
                deltaTable[key] = -2;
            }

            // Insertions of short lefts and rights are okay
            key = new DeltaKey("", "Sl");
            deltaTable[key] = -1;

            key = new DeltaKey("", "Sr");
            deltaTable[key] = -1;

            // Therefore, for one insertion of an up or down, the threshold will be 7 or higher.
            // Perfect match is 8, minus 1 for 1 insertion of an Su or Sd.

            return deltaTable;
        }

        // Scanning Atoms
        public static Dictionary<DeltaKey, int> CreateScanHorizontalDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            // Want to match lefts and rights mostly
            // Perfect match = 4
            DeltaKey key = new DeltaKey("Sr", "Sr");
            deltaTable[key] = 1;

            key = new DeltaKey("Sl", "Sl");
            deltaTable[key] = 1;

            foreach (Token y in tokens)
            {
                // Penalise any kind of insertions
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -10;

                // and deletions
                key = new DeltaKey(Wordbook.TokenToString(y), "");
                deltaTable[key] = -10;
            }

            return deltaTable;
        } // SrSlSlSr
        public static Dictionary<DeltaKey, int> CreateScanVerticalDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-1);

            // Want to match lefts and rights mostly
            DeltaKey key = new DeltaKey("Su", "Su");
            deltaTable[key] = 1;

            key = new DeltaKey("Sd", "Sd");
            deltaTable[key] = 1;

            foreach (Token y in tokens)
            {
                // Penalise any kind of insertions
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -10;

                // and deletions
                key = new DeltaKey(Wordbook.TokenToString(y), "");
                deltaTable[key] = -10;
            }

            return deltaTable;
        } // SuSdSdSu

        public static Dictionary<DeltaKey, int> CreateScanCornerDeltaTable()
        {
            Dictionary<DeltaKey, int> deltaTable = CreateInitialTable(-5);

            // Want to match lefts and rights, ups and down
            DeltaKey key = new DeltaKey("Su", "Su");
            deltaTable[key] = 1;

            key = new DeltaKey("Sd", "Sd");
            deltaTable[key] = 1;

            key = new DeltaKey("Sl", "Sl");
            deltaTable[key] = 1;

            key = new DeltaKey("Sr", "Sr");
            deltaTable[key] = 1;

            foreach (Token y in tokens)
            {
                // Penalise any kind of insertions
                key = new DeltaKey("", Wordbook.TokenToString(y));
                deltaTable[key] = -10;

                // and deletions
                key = new DeltaKey(Wordbook.TokenToString(y), "");
                deltaTable[key] = -10;
            }

            return deltaTable;
        } // SrSlSuSd, and all variants
        
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
