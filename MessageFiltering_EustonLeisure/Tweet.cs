using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MessageFiltering_EustonLeisure
{
    class Tweet : Message
    {
        //Contructor
        public Tweet(string header, string sender, string body) : base(header, sender, body) { }
    }
}
