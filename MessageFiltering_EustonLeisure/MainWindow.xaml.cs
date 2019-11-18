using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using Newtonsoft.Json;

namespace MessageFiltering_EustonLeisure
{
    // Marco Picchillo 40340891

    public partial class MainWindow : Window
    {
        List<Mention> mentionsList = new List<Mention>();
        List<Mention> trendingList = new List<Mention>();
        string[] quarantineed = new string[100];
        int nOfQuarantined = 0;
        string[,] abbreviations = new string[256,2];
        int nOfAbbreviations = 0;
        
        List<Message> messagesList = new List<Message>();
        List<SIRListEntry> SIRList = new List<SIRListEntry>();
        public MainWindow()
        {
            InitializeComponent();
            LoadTextAbbreviation(ref abbreviations, ref nOfAbbreviations);
        }

        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (RegexCheck("((?:[S|E|T]+[0-9]{9}))", tboxHeader.Text.ToUpper()))
            {
                string confirmedHeader = tboxHeader.Text.ToUpper();
                switch (confirmedHeader[0])
                {
                    case 'S':
                        {
                            if (MessageLegthLimit(tboxBody.Text, 140))
                            {
                                string senderNumber;
                                string RegexPhone = @"^((\+\d{1,3}(-| )?\(?\d\)?(-| )?\d{1,5})|(\(?\d{2,6}\)?))(-| )?(\d{3,4})(-| )?(\d{4})(( x| ext)\d{1,5}){0,1}";
                                string body = tboxBody.Text;

                                if (RegexCheck(RegexPhone, body))
                                    senderNumber = ExtractMatch(RegexPhone, ref body);
                                else
                                {
                                    MessageBox.Show("ERROR\n\nInvalid Sender Number!", "Sender Number Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    break;
                                }
                                ExpandAbbreviation(ref body, abbreviations, nOfAbbreviations);
                                messagesList.Add(new Message(tboxHeader.Text, senderNumber, body));
                                UploadToFile(messagesList.Last());

                                DisplaySelectedMessage(messagesList.Last());
                            }
                            else
                                BodyOutOfLimit(140);
                            
                            break;
                        }

                    case 'E':
                        {
                            string body = tboxBody.Text;
                            string regexEmail = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                                                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))";

                            if (RegexCheck(regexEmail, body))
                            {
                                bool validFormat = false;
                                string senderEmail = ExtractMatch(regexEmail, ref body);
                                                                
                                string RegexSIR = @"(SIR (0[1-9]|1[0-9]|2[0-9]|3[0-1])[\/](0[1-9]|1[0-2])[\/](0[0-9]|1[0-9]|2[0-9]))";

                                if (RegexCheck(RegexSIR, body))
                                {
                                    string IncidentReportSIR = ExtractMatch(RegexSIR, ref body);

                                    string RegexCentreCode = @"(\d\d)+(-)+(\d\d\d)+(-)+(\d\d)";
                                    string CentreCode = ExtractMatch(RegexCentreCode, ref body);

                                    string incidentNat = new StringReader(body).ReadLine();
                                    body = EditBody(body, incidentNat);

                                    if (ValidIncidentNature(incidentNat))
                                    {
                                        ExpandAbbreviation(ref body, abbreviations, nOfAbbreviations);
                                        QuarantineList(body, ref quarantineed, ref nOfQuarantined);
                                        QuarantineURLs(ref body);
                                        SIRList.Add(new SIRListEntry(CentreCode, incidentNat));

                                        messagesList.Add(new IncidentEmail(tboxHeader.Text, senderEmail, IncidentReportSIR, CentreCode, incidentNat, body));
                                        
                                        UploadToFile(messagesList.Last());

                                        validFormat = true;
                                    }
                                    else
                                        MessageBox.Show("Invalid Nature of Incident Found!", "Invalid Nature of Incident",MessageBoxButton.OK,MessageBoxImage.Error);                                        
                                }
                                else
                                {
                                    string subject = new StringReader(body).ReadLine();
                                    body = EditBody(body, subject);

                                    if (subject.Length <= 20)
                                    {
                                        ExpandAbbreviation(ref body, abbreviations, nOfAbbreviations);
                                        QuarantineList(body, ref quarantineed, ref nOfQuarantined);
                                        QuarantineURLs(ref body);

                                        messagesList.Add(new Email(tboxHeader.Text, senderEmail, subject, body));

                                        UploadToFile(messagesList.Last());

                                        validFormat = true;
                                    }
                                    else
                                        MessageBox.Show("Invalid Subject Lenght!", "Invalid Subject", MessageBoxButton.OK, MessageBoxImage.Error);
                                }

                                if (validFormat)                                 
                                    DisplaySelectedMessage(messagesList.Last());
                                else
                                    MessageBox.Show("Invalid Message Format for Email!", "Email Format Error", MessageBoxButton.OK,MessageBoxImage.Error);                                                                
                            }
                            else
                                MessageBox.Show("Invalid EMAIL Body Format\n\nValid Format: [SENDER EMAIL][BODY]"); //TO BE FIXED

                            break;
                        }
                    case 'T':
                        {
                            string body = tboxBody.Text;

                            if (RegexCheck(@"^@?(\w){1,15}", body))
                            {
                                string tweetAuthor = ExtractMatch(@"^@?(\w){1,15}", ref body);

                                if (MessageLegthLimit(body, 140))
                                {
                                    ExpandAbbreviation(ref body, abbreviations, nOfAbbreviations);
                                    UpdateList(body, ref trendingList, @"(#)\w+");
                                    UpdateList(body, ref mentionsList, @"(@)+(\w){1,15}");

                                    messagesList.Add(new Tweet(tboxHeader.Text, tweetAuthor, body));   

                                    UploadToFile(messagesList.Last());

                                    DisplaySelectedMessage(messagesList.Last());
                                }                                    
                                else
                                    BodyOutOfLimit(140);
                            }
                            else
                                MessageBox.Show("Tweet Author not Found!", "Author Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            break;
                        }
                }
            }
            else
                MessageBox.Show("Invalid Header Format!");
        }

        private bool RegexCheck(string pattern, string message)
        {
            Regex r = new Regex(pattern);
            Match regexMatch = r.Match(message);

            if (regexMatch.Success)
                return true;
            return false;
        }

        private string ExtractMatch(string pattern, ref string message)
        {
            Regex r = new Regex(pattern);
            Match regexMatch = r.Match(message);

            string extractedString = regexMatch.ToString();

            message = EditBody(message, extractedString);

            return extractedString;
        }

        private string EditBody(string message, string toRemove)
        {
            int startIndexPN = message.IndexOf(toRemove);
            int lengthOfPN = toRemove.Length;
            
            string editedMessage = message.Remove(startIndexPN, lengthOfPN);
            RemoveEmptyLine(ref editedMessage);

            return editedMessage;
        }

        private bool MessageLegthLimit(string message, int limit)
        {
            if (message.Length < limit)
                return true;
            return false;
        }

        private void LoadTextAbbreviation(ref string[,] textAbb, ref int nAbb)
        {
            var fileLocation = @"TextWords.csv"; // Habeeb, "Dubai Media City, Dubai"
            using (TextFieldParser cvsFile = new TextFieldParser(fileLocation))
            {
                //Set FiledDelimiter
                cvsFile.SetDelimiters(new string[] { "," });

                //i will keep track of the line number.
                int i = 0;
                while (!cvsFile.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = cvsFile.ReadFields();
                    if(!(string.IsNullOrEmpty(fields[0]) && string.IsNullOrEmpty(fields[1])))
                    {
                        textAbb[i, 0] = fields[0];
                        textAbb[i, 1] = fields[1];
                        nAbb++;
                    }                    
                    i++;
                }
            }
        }                       

        private void BodyOutOfLimit(int limit)
        {
            MessageBox.Show(string.Format("The body of the message exceed the characters limit [{0}]!", limit));
        }

        private void DisplaySelectedMessage(Message m)
        {
            DisplayMessage displayThis = new DisplayMessage(m);
            displayThis.ShowDialog();
        }

        private void RemoveEmptyLine(ref string message)
        {
            while (message[0] == '\n' || message[0] == '\r')
                message = message.Remove(0, 1);
        }

        private bool ValidIncidentNature(string inc)
        {            
            string[] eachIncident = System.IO.File.ReadAllLines("IncidentsList.txt");

            foreach (string i in eachIncident)
            {
                if (i == inc)
                    return true;
            }
            return false;
        }

        private void BtnMentionShow_Click(object sender, RoutedEventArgs e)
        {
            foreach (Mention m in mentionsList)
                MessageBox.Show(string.Format("Author: {0} - Occurrences: {1}", m.MentionText, m.Occurrences));
            foreach(Mention t in trendingList)
                MessageBox.Show(string.Format("Hashtag: {0} - Occurrences: {1}", t.MentionText, t.Occurrences));
            for (int i = 0; i < nOfQuarantined; i++)
                MessageBox.Show(string.Format("Quarantineed-> {0}", quarantineed[i]));

        }

        protected void ExpandAbbreviation(ref string message, string[,] abbs, int nAbb)
        {
            for (int i = 0; i < nAbb; i++)
            {
                Regex anAbbreviation = new Regex(@"(?<![\w])" + abbs[i, 0] + @"(?![\w])", RegexOptions.IgnoreCase);
                foreach (Match m in anAbbreviation.Matches(message))
                    message = message.Insert(m.Index + abbs[i, 0].Length, string.Format(" <{0}> ", abbs[i, 1]));
            }
        }

        private void UpdateList(string body, ref List<Mention> trendingL, string pattern)
        {
            //Regex hashtag = new Regex(@"(@)+(\w){1,15}");//(?<=@)(\w){1,15}");//@"@?(\w){1,15}");
            Regex mention = new Regex(pattern);
            foreach (Match mtc in mention.Matches(body))
            {
                bool alreadyMentioned = false;
                foreach (Mention m in trendingL)
                {
                    if (mtc.Value.ToString() == m.MentionText)
                    {
                        m.Occurrences++;
                        alreadyMentioned = true;
                        break;
                    }
                }
                if (!alreadyMentioned)
                    trendingL.Add(new Mention(mtc.Value.ToString()));
            }
        }
        protected void QuarantineURLs(ref string message)
        {
            string URL = (@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)");
            string replacement = ("<URL Quarantined>");
            message = Regex.Replace(message, URL, replacement);
        }

        private void QuarantineList(string body, ref string[] quarantineed, ref int nOfQuarantineed)
        {
            Regex URL = new Regex(@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)");
            foreach (Match m in URL.Matches(body))
            {
                bool alreadyQuarantineed = false;
                for (int i = 0; i < nOfQuarantineed; i++)
                {
                    if (m.Value.ToString() == quarantineed[i])
                        alreadyQuarantineed = true;
                }
                if (!alreadyQuarantineed)
                {
                    quarantineed[nOfQuarantineed] = m.Value.ToString();
                    nOfQuarantineed++;
                }
            }
        }
        private void UploadToFile(Message m)
        {
            string json = JsonConvert.SerializeObject(m);
            MessageBox.Show(json);

            File.AppendAllText("ProcessedMessage.json", json);
        }
    }
    
}
