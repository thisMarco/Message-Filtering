namespace MessageFiltering_EustonLeisure
{
    class Mention
    {
        public string  MentionText{ get; set; }
        public int Occurrences { get; set; }

        public Mention(string user)
        {
            MentionText = user;
            Occurrences = 1;
        }

        public override string ToString()
        {
            return string.Format("{0} Occurrences: {1}", Occurrences, MentionText);
        }
    }
}
