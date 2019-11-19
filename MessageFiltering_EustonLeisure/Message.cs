using System.Text.RegularExpressions;

namespace MessageFiltering_EustonLeisure
{
    public class Message
    {
        public string Header;
        public string Sender;
        public string Body;
        

        //Contructor
        public Message(string header, string sender, string body)
        {
            Header = header;
            Sender = sender;
            Body = body;          
        }        
    }
}
