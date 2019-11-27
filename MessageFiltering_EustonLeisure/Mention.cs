namespace MessageFiltering_EustonLeisure
{
    //This class is uset to create objects that will contain Tweet mentions or Hashtags
    class Mention
    {
        public string  MentionText{ get; set; } //MentionId or Hashtag
        public int Occurrences { get; set; } //Number of Mention Occurrences

        //Constructor
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
