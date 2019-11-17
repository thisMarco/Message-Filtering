using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MessageFiltering_EustonLeisure
{
    public class Email : Message
    {
        public string Subject { get; set; }
        public Email(string header, string sender, string subject, string body, ref string[,] abbList, ref int abbNumber, ref string[] quarantneed, ref int nOfQuarantneed): base(header, sender, body, ref abbList, ref abbNumber)
        {
            Subject = subject;

            //ExpandAbbreviation(ref Body, Abbreviations, AbbNumber);
            QuarantineList(body, ref quarantneed, ref nOfQuarantneed);
            QuarantineURLs(ref Body);
        }

        protected void QuarantineURLs(ref string message)
        {
            string URL = (@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)");
            string replacement = ("<URL Quarantined>");
            message = Regex.Replace(message, URL, replacement);
        }
        
        private void QuarantineList(string body, ref string[] quarantineed, ref int nOfQuarantineed)
        {
            Regex URL = new Regex(@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)");
            foreach (Match m in URL.Matches(body))
            {
                bool alreadyQuarantineed = false;
                for(int i = 0; i < nOfQuarantineed; i++)
                {
                    if (m.Value.ToString() == quarantineed[i])
                        alreadyQuarantineed = true;
                }
                if (!alreadyQuarantineed)
                {
                    quarantineed[nOfQuarantineed] = m.Value.ToString();
                    nOfQuarantineed++;
                }                    
            }
        }
    }
}
