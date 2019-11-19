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

        public override string ToString()
        {
            return string.Format("{0} {1}",Code,IncidentNature);
        }
    }
}
