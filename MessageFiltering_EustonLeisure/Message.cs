using System.Text.RegularExpressions;

namespace MessageFiltering_EustonLeisure
{
    public class Message
    {
        public string Header;
        public string Body;
        public string Sender;

        //Contructor
        public Message(string header, string sender, string body)
        {
            Header = header;
            Sender = sender;
            Body = body;          
        }        
    }
}
