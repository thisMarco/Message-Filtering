using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows;

namespace MessageFiltering_EustonLeisure
{
    public class Message
    {
        public string Header;
        public string Body;
        public string Sender;
        public string[,] Abbreviations;
        public int AbbNumber;

        //Contructor
        public Message(string header, string sender, string body, ref string[,] abbList, ref int abbNumber)
        {
            Header = header;
            Sender = sender;
            Body = body;
            Abbreviations = abbList;
            AbbNumber = abbNumber;
            
            ExpandAbbreviation(ref Body, Abbreviations, AbbNumber);            
        }

        protected void ExpandAbbreviation(ref string message, string[,] abbs, int nAbb)
        {
            for (int i = 0; i < nAbb; i++)
            {
                Regex anAbbreviation = new Regex(@"(?<![\w])" + abbs[i, 0] + @"(?![\w])", RegexOptions.IgnoreCase);
                foreach (Match m in anAbbreviation.Matches(message))
                    message = message.Insert(m.Index + abbs[i, 0].Length, string.Format(" <{0}> ", abbs[i, 1]));
            }
        }
    }
}
