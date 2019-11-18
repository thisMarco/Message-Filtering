namespace MessageFiltering_EustonLeisure
{
    class IncidentEmail :Email
    {
        public string Code { get; set; }
        public string Incident { get; set; }
        public IncidentEmail(string header, string sender, string subject, string code, string incident, string body): base(header, sender, subject, body)
        {
            Code = code;
            Incident = incident;
        }
    }
}
