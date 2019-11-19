using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace MessageFiltering_EustonLeisure
{
    // Marco Picchillo 40340891

    public partial class MainWindow : Window
    {
        List<Mention> mentionsList = new List<Mention>();
        List<Mention> trendingList = new List<Mention>();
        List<SIRListEntry> SIRList = new List<SIRListEntry>();

        Dictionary<string, Message> messagesList = new Dictionary<string, Message>();

        string[] quarantineed = new string[100];
        int nOfQuarantined = 0;

        string[,] abbreviations = new string[256,2];
        int nOfAbbreviations = 0;

        bool messagesUnsaved = true;  
        
        public MainWindow()
        {
            InitializeComponent();
            lboxMessages.ItemsSource = messagesList.Keys;
            //Load Abbreviation from file
            LoadTextAbbreviation(ref abbreviations, ref nOfAbbreviations);
        }

        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (RegexCheck("((?:[S|E|T]+[0-9]{9}))", tboxHeader.Text.ToUpper()))
            {
                if (!messagesList.ContainsKey(tboxHeader.Text))
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
                                    messagesList.Add(tboxHeader.Text, new Message(tboxHeader.Text, senderNumber, body));
                                    lboxMessages.Items.Refresh();

                                    messagesUnsaved = true;
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
                                            lboxSIR.Items.Add(SIRList.Last().ToString());
                                            messagesList.Add(tboxHeader.Text, new IncidentEmail(tboxHeader.Text, senderEmail, IncidentReportSIR, CentreCode, incidentNat, body));
                                            messagesUnsaved = true;
                                            lboxMessages.Items.Refresh();                                            
                                        }
                                        else
                                            MessageBox.Show("Invalid Nature of Incident Found!", "Invalid Nature of Incident", MessageBoxButton.OK, MessageBoxImage.Error);
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

                                            messagesList.Add(tboxHeader.Text, new Email(tboxHeader.Text, senderEmail, subject, body));
                                            messagesUnsaved = true;
                                            lboxMessages.Items.Refresh();

                                            //validFormat = true;
                                        }
                                        else
                                            MessageBox.Show("Invalid Subject Lenght!", "Invalid Subject", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                                else
                                    MessageBox.Show("Invalid Message Format for Email!", "Email Format Error", MessageBoxButton.OK, MessageBoxImage.Error);

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

                                        messagesList.Add(tboxHeader.Text, new Tweet(tboxHeader.Text, tweetAuthor, body));
                                        messagesUnsaved = true;
                                        lboxMessages.Items.Refresh();

                                        if (mentionsList.Count > 0 || trendingList.Count > 0)
                                            UpdateMentionTrending();

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
                    MessageBox.Show("There is already a message with this Header Value!", "Recurring Header", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                
            }
            else
                MessageBox.Show("Invalid Header Format!");
        }

        #region Validation
        private bool RegexCheck(string pattern, string message)
        {
            Regex r = new Regex(pattern);
            Match regexMatch = r.Match(message);

            if (regexMatch.Success)
                return true;
            return false;
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

        private bool MessageLegthLimit(string message, int limit)
        {
            if (message.Length < limit)
                return true;
            return false;
        }

        private void BodyOutOfLimit(int limit)
        {
            MessageBox.Show(string.Format("The body of the message exceed the characters limit [{0}]!", limit));
        }
        #endregion

        #region BodyEditing
        private string EditBody(string message, string toRemove)
        {
            int startIndexPN = message.IndexOf(toRemove);
            int lengthOfPN = toRemove.Length;

            string editedMessage = message.Remove(startIndexPN, lengthOfPN);
            RemoveEmptyLine(ref editedMessage);

            return editedMessage;
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

        protected void QuarantineURLs(ref string message)
        {
            string URL = (@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)");
            string replacement = ("<URL Quarantined>");
            message = Regex.Replace(message, URL, replacement);
        }

        private void RemoveEmptyLine(ref string message)
        {
            while (message[0] == '\n' || message[0] == '\r')
                message = message.Remove(0, 1);
        }

        private string ExtractMatch(string pattern, ref string message)
        {
            Regex r = new Regex(pattern);
            Match regexMatch = r.Match(message);

            string extractedString = regexMatch.ToString();

            message = EditBody(message, extractedString);

            return extractedString;
        }
        #endregion

        #region UpdateLists
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
                    lboxQuarantined.Items.Add(quarantineed[nOfQuarantined]);
                    nOfQuarantineed++;                    
                }
            }
        }
        #endregion

        #region UIUpdate
        public void UpdateMentionTrending()
        {
            if (mentionsList.Count > 1)
            {
                mentionsList.Sort((x, y) => x.Occurrences.CompareTo(y.Occurrences));
                lboxMentions.Items.Refresh();

                lboxMentions.Items.Clear();

                foreach (Mention m in mentionsList)
                    lboxMentions.Items.Add(m.ToString());
            }

            if (trendingList.Count > 1)
            {
                trendingList.Sort((x, y) => x.Occurrences.CompareTo(y.Occurrences));
                lboxTrending.Items.Refresh();

                lboxTrending.Items.Clear();

                foreach (Mention m in trendingList)
                    lboxTrending.Items.Add(m.ToString());
            }
        }           
        #endregion

        #region SaveLoad Files
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
                    if (!(string.IsNullOrEmpty(fields[0]) && string.IsNullOrEmpty(fields[1])))
                    {
                        textAbb[i, 0] = fields[0];
                        textAbb[i, 1] = fields[1];
                        nAbb++;
                    }
                    i++;
                }
            }
        }

        private void BtnSaveMessages_Click(object sender, RoutedEventArgs e)
        {
            string json = JsonConvert.SerializeObject(messagesList, Formatting.Indented);
            string filename = string.Format("Euston Leisure{0}.json", DateTime.UtcNow.ToString("ddMMyyyy_HHmmss"));

            File.WriteAllText(filename, json);
            messagesUnsaved = false;
            MessageBox.Show("Messages Correctly Saved");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (!messagesUnsaved || messagesList.Count() == 0)
                System.Windows.Application.Current.Shutdown();
            else
            {
                var selectedOption = MessageBox.Show("The processed file have not been saved. Do you want to save them?", "Unsaved Messages", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (selectedOption == MessageBoxResult.Yes)
                    BtnSaveMessages_Click(this, e);
                else if (selectedOption == MessageBoxResult.No)
                    System.Windows.Application.Current.Shutdown();
            }
        }

        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            //openFileDialog.ShowDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                //string jsonString = File.ReadAllText(openFileDialog.FileName);
                //dynamic messages = JsonConvert.DeserializeObject(jsonString);

                //List<Message> loadedMessages = new List<Message>();
                //foreach (var m in messages)
                //    loadedMessages.Add(m as Message);

                //List<Message> loadedMessages = JsonConvert.DeserializeObject<List<Message>>(File.ReadAllText(openFileDialog.FileName));
            }
        }
        #endregion

        #region DisplayMessage
        private void LboxMessages_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            DisplayMessage displayMessage = new DisplayMessage(messagesList[lboxMessages.SelectedItem.ToString()]);
            displayMessage.ShowDialog();
        }
        #endregion
    }

}