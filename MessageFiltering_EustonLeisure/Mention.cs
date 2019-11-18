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
    }
}
