namespace MessageFiltering_EustonLeisure
{
    //This class will create objects that contain SIR List Entries.
    class SIRListEntry
    {
        public string Code { get; set; } //Center Code
        public string IncidentNature { get; set; } //Nature of Incident

        //Costructor
        public SIRListEntry(string code, string incident)
        {
            Code = code;
            IncidentNature = incident;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}",Code,IncidentNature);
        }
    }
}
