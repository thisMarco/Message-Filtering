using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace MessageFiltering_EustonLeisure
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
            if (RegexCheck("((?:[S|E|T|s|e|t]+[0-9]{9}))", tboxHeader.Text))
            {
                string confirmedHeader = tboxHeader.Text;
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
                                messagesList.Add(new Message(tboxHeader.Text, senderNumber, body, ref abbreviations, ref nOfAbbreviations));
                                DisplaySelectedMessage(messagesList.Last());
                            }
                            else
                                BodyOutOfLimit(140);

                            //string body = tboxBody.Text;

                            //if (RegexCheck(@"^((\+\d{1,3}(-| )?\(?\d\)?(-| )?\d{1,5})|(\(?\d{2,6}\)?))(-| )?(\d{3,4})(-| )?(\d{4})(( x| ext)\d{1,5}){0,1}", body))
                            //{
                            //    string senderNumber = ExtractMatch(@"^((\+\d{1,3}(-| )?\(?\d\)?(-| )?\d{1,5})|(\(?\d{2,6}\)?))(-| )?(\d{3,4})(-| )?(\d{4})(( x| ext)\d{1,5}){0,1}", body);

                            //    //int startIndexPN = body.IndexOf(senderNumber);
                            //    //int lengthOfPN = senderNumber.Length;

                            //    //body = body.Remove(startIndexPN, lengthOfPN);

                            //    body = EditBody(body, senderNumber);

                            //    if (MessageLegthLimit(body, 140))
                            //        ExpandAbbreviation(ref body, abbrevations, nOfAbbreviations);
                            //    else
                            //        BodyOutOfLimit(140);
                            //}
                            //else
                            //    MessageBox.Show("Invalid SMS Body Format\n\nValid Format: [SENDER NUMBER][BODY]");
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
                                        messagesList.Add(new IncidentEmail(tboxHeader.Text, senderEmail, IncidentReportSIR, CentreCode, incidentNat, body, ref abbreviations, ref nOfAbbreviations, ref quarantineed, ref nOfQuarantined));
                                        SIRList.Add(new SIRListEntry(CentreCode, incidentNat));
                                        
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
                                        messagesList.Add(new Email(tboxHeader.Text, senderEmail, subject, body, ref abbreviations, ref nOfAbbreviations, ref quarantineed, ref nOfQuarantined));
                                        validFormat = true;
                                    }
                                    else
                                        MessageBox.Show("Invalid Subject Lenght!", "Invalid Subject", MessageBoxButton.OK, MessageBoxImage.Error);
                                }

                                if (validFormat)                                 
                                    DisplaySelectedMessage(messagesList.Last());
                                else
                                    MessageBox.Show("Invalid Message Format for Email!", "Email Format Error", MessageBoxButton.OK,MessageBoxImage.Error);

                                //if (RegexCheck(@"^?((\d{2})+(-)+(\d{3})+(-)+(\d{2}))", body))
                                //{
                                //    string incidentCode = ExtractMatch(@"^?((\d{2})+(-)+(\d{3})+(-)+(\d{2}))", body);
                                //    body = EditBody(body, incidentCode);

                                //    if (MessageLegthLimit(body, 1028))
                                //    {
                                //        ExpandAbbreviation(ref body, abbreviations, nOfAbbreviations);
                                //        QuarantineURLs(ref body);
                                //    }
                                //    else
                                //        BodyOutOfLimit(1028);                                }
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
                                    messagesList.Add(new Tweet(tboxHeader.Text, tweetAuthor, body, ref abbreviations, ref nOfAbbreviations, ref mentionsList, ref trendingList));
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

        //private void ExpandAbbreviation(ref string message, string[,] abbreviations, int nAbb)
        //{
        //    for(int i = 0; i < nAbb; i++)
        //    {
        //        //int count = 0;
        //        //int start = 0;
        //        //int end = message.Length;
        //        //int indexOfAbbreviation = 0;

        //        //string abb = (@"(?<![\w])" + abbrevations[i, 0] + @"(?![\w])");
        //        //string replacement = string.Format("$& {0}",abbrevations[i,1]);
        //        //message = Regex.Replace(message, abb, replacement);

        //        Regex abb = new Regex(@"(?<![\w])" + abbreviations[i, 0] + @"(?![\w])", RegexOptions.IgnoreCase);
        //        foreach (Match m in abb.Matches(message))
        //            message = message.Insert(m.Index + abbreviations[i, 0].Length, string.Format(" <{0}> ", abbreviations[i, 1]));

        //        //while ((start < end) && (indexOfAbbreviation > -1))
        //        //{
        //        //    count = message.Length - start;
        //        //    if (Regex.IsMatch(message, string.Format(@"\b{0}\b", abbrevations[i, 0])))
        //        //    {
        //        //        indexOfAbbreviation = message.IndexOf(abbreviations[i, 0], start, count);
        //        //        if (indexOfAbbreviation == -1)
        //        //            break;
        //        //        else
        //        //        {
        //        //            message = message.Insert(indexOfAbbreviation + abbreviations[i, 0].Length, string.Format(" <{0}> ", abbreviations[i, 1]));
        //        //        }
        //        //    }

        //        //    start = indexOfAbbreviation + abbrevations[i, 0].Length + abbrevations[i, 1].Length + 3;
        //        //    end = message.Length;
        //        //}
        //    }
        //    //MessageBox.Show(string.Format("Updated Body: {0}",message));
        //}        

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

        //private bool HeaderCheck(string header)
        //{
        //    string headerPattern = "((?:[S|E|T|s|e|t]+[0-9]{9}))";
        //    Regex r = new Regex(headerPattern);

        //    Match headerMatch = r.Match(header);

        //    if (!headerMatch.Success)
        //    {
        //        MessageBox.Show("Invalid Header");
        //        return false;
        //    }
        //    return true;            
        //}

        //private string PhoneNumberCheck(string body)
        //{
        //    string phoneNumber = @"^((\+\d{1,3}(-| )?\(?\d\)?(-| )?\d{1,5})|(\(?\d{2,6}\)?))(-| )?(\d{3,4})(-| )?(\d{4})(( x| ext)\d{1,5}){0,1}";
        //    Regex r = new Regex(phoneNumber);

        //    Match phoneMatch = r.Match(tboxBody.Text);
        //    if (phoneMatch.Success)
        //        return phoneMatch.ToString();
        //    return ("INVALID");
        //}
    }
    
}
