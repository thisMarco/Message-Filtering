using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageFiltering_EustonLeisure
{
    class SIRListEntry
    {
        public string Code { get; set; }
        public string IncidentNature { get; set; }

        public SIRListEntry(string code, string incident)
        {
            Code = code;
            IncidentNature = incident;
        }
    }
}
