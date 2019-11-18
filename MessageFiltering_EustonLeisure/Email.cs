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
        public Email(string header, string sender, string subject, string body): base(header, sender, body)
        {
            Subject = subject;
        }

        
        
        
    }
}
