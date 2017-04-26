using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeTrackingCore
{
    enum Token
    {
        ShortRight, MediumRight, LongRight,
        ShortUp, MediumUp, LongUp,
        ShortLeft, MediumLeft, LongLeft,
        ShortDown, MediumDown, LongDown,

        Brief, Hold
    }

    public class Wordbook
    {
        private string book = "";
        public string Book
        {
            get { return this.book; }
        }

        public Wordbook(List<Saccade> saccades)
        {
            foreach(Saccade saccade in saccades)
            {
                Token saccadeToken = TokenForSaccade(saccade);
                book = string.Concat(book, TokenToString(saccadeToken));
            }
        }

        public Wordbook(List<Fixation> fixations)
        {

        }

        private Token TokenForSaccade(Saccade saccade)
        {
            string tokenName = String.Format("{0}{1}", Enum.GetName(typeof(SaccadeType), saccade.Type), Enum.GetName(typeof(Sector), saccade.Sector4));

            Token result;
            Enum.TryParse<Token>(tokenName, out result);

            return result;
        }

        private Dictionary<Token, string> TokenStringRepresentationTable =
            new Dictionary<Token, string>
            {
                [Token.ShortRight] = "Sr", [Token.MediumRight] = "Mr", [Token.LongRight] = "Lr",
                [Token.ShortUp] = "Su", [Token.MediumUp] = "Mu", [Token.LongUp] = "Lu",
                [Token.ShortLeft] = "Sl", [Token.MediumLeft] = "Ml", [Token.LongLeft] = "Ll",
                [Token.ShortDown] = "Sd", [Token.MediumDown] = "Md", [Token.LongDown] = "Ld",

                [Token.Brief] = "Br", [Token.Hold] = "Ho"
            };

        private string TokenToString(Token token)
        {
            return TokenStringRepresentationTable[token];
        }
    }
}
