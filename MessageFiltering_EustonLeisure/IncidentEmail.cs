using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageFiltering_EustonLeisure
{
    class IncidentEmail :Email
    {
        public string Code { get; set; }
        public string Incident { get; set; }
        public IncidentEmail(string header, string sender, string subject, string code, string incident, string body, ref string[,] abbList, ref int abbNumber, ref string[] quarantneed, ref int nOfQuarantneed): base(header, sender, subject, body, ref abbList, ref abbNumber, ref quarantneed, ref nOfQuarantneed)
        {
            Code = code;
            Incident = incident;

            //ExpandAbbreviation(ref Body, Abbreviations, AbbNumber);
            //QuarantineURLs(ref Body);
        }
    }
}
