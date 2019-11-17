using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows;

namespace MessageFiltering_EustonLeisure
{
    class Tweet : Message
    {
        //Contructor
        public Tweet(string header, string sender, string body, ref string[,] abbList, ref int abbNumber, ref List<Mention> mentions, ref List<Mention> trends) : base(header, sender, body, ref abbList, ref abbNumber)
        {
            UpdateList(Body, ref mentions, @"(@)+(\w){1,15}");
            UpdateList(Body, ref trends, @"(#)\w+");
        }

        private void UpdateList(string body, ref List<Mention> trendingL, string pattern)
        {
            //Regex hashtag = new Regex(@"(@)+(\w){1,15}");//(?<=@)(\w){1,15}");//@"@?(\w){1,15}");
            Regex mention = new Regex(pattern);
            foreach (Match mtc in mention.Matches(body))
            {
                bool alreadyMentioned = false;
                foreach (Mention m in trendingL)
                {
                    if (mtc.Value.ToString() == m.MentionText)
                    {
                        m.Occurrences++;
                        alreadyMentioned = true;
                        break;
                    }
                }
                if (!alreadyMentioned)
                    trendingL.Add(new Mention(mtc.Value.ToString()));
            }
        }
        //private void TrendingList()
        //{
        //    Regex hashtag = new Regex(@"(#)\w+");
        //}

        //private void MentionList(string body, ref List<Mention> mentions)
        //{
        //    Regex hashtag = new Regex(@"(@)+(\w){1,15}");//(?<=@)(\w){1,15}");//@"@?(\w){1,15}");
        //    foreach (Match mtc in hashtag.Matches(body))
        //    {
        //        bool alreadyMentioned = false;
        //        foreach(Mention m in mentions)
        //        {                    
        //            if (mtc.Value.ToString() == m.UserID)
        //            {
        //                m.Occurrences++;
        //                alreadyMentioned = true;
        //                break;
        //            }                        
        //        }
        //        if (!alreadyMentioned)
        //            mentions.Add(new Mention(mtc.Value.ToString()));
        //    }
        //}
    }
}
