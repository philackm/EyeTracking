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
    }
}
