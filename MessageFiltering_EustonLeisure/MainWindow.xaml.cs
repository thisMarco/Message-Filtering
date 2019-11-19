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

        #region Process Messages
        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (RegexCheck("((?:[S|E|T]+[0-9]{9}))", tboxHeader.Text.ToUpper()))
            {
                if (ProcessMessage(tboxHeader.Text, tboxBody.Text))
                {
                    tboxHeader.Text = string.Empty;
                    tboxBody.Text = string.Empty;
                }
            }
            else
                MessageBox.Show("Invalid Header Format!");
        }

        public bool ProcessMessage(string h, string b)
        {
            h = h.ToUpper();
            if (RegexCheck("((?:[S|E|T]+[0-9]{9}))", h))
            {
                if (!messagesList.ContainsKey(h))
                {                    
                    switch (h[0])
                    {
                        case 'S':
                            {
                                return (ProcessSMS(h, b));
                            }

                        case 'E':
                            {
                                return (ProcessEmail(h, b));
                            }
                        case 'T':
                            {
                                return (ProcessTweet(h, b));
                            }
                    }
                }
                else
                    MessageBox.Show("There is already a message with this Header Value!", "Recurring Header", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
                MessageBox.Show("{0} is an incorrect header format", "Header Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        public bool ProcessSMS(string h, string b)
        {
            string RegexPhone = @"^((\+\d{1,3}(-| )?\(?\d\)?(-| )?\d{1,5})|(\(?\d{2,6}\)?))(-| )?(\d{3,4})(-| )?(\d{4})(( x| ext)\d{1,5}){0,1}";
            if (RegexCheck(RegexPhone, b))
            {                
                string senderNumber = ExtractMatch(RegexPhone, ref b);

                if (MessageLegthLimit(b, 140))
                {                    
                    ExpandAbbreviation(ref b, abbreviations, nOfAbbreviations);
                    messagesList.Add(h, new Message(h, senderNumber, b));
                    lboxMessages.Items.Refresh();
                    messagesUnsaved = true;
                    return true;
                }                    
                else
                    BodyOutOfLimit(140);
            }
            else
                MessageBox.Show("Invalid Sender Number!", "Sender Number Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return false;
        }

        public bool ProcessEmail(string h, string b)
        {
            string regexEmail = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))";

            if (RegexCheck(regexEmail, b))
            {
                string senderEmail = ExtractMatch(regexEmail, ref b);

                string RegexSIR = @"(SIR (0[1-9]|1[0-9]|2[0-9]|3[0-1])[\/](0[1-9]|1[0-2])[\/](0[0-9]|1[0-9]|2[0-9]))";

                if (RegexCheck(RegexSIR, b))
                {
                    string IncidentReportSIR = ExtractMatch(RegexSIR, ref b);

                    string RegexCentreCode = @"(\d\d)+(-)+(\d\d\d)+(-)+(\d\d)";
                    string CentreCode = ExtractMatch(RegexCentreCode, ref b);

                    string incidentNat = new StringReader(b).ReadLine();
                    b = EditBody(b, incidentNat);

                    if (ValidIncidentNature(incidentNat))
                    {
                        ExpandAbbreviation(ref b, abbreviations, nOfAbbreviations);
                        QuarantineList(b, ref quarantineed, ref nOfQuarantined);
                        QuarantineURLs(ref b);

                        SIRList.Add(new SIRListEntry(CentreCode, incidentNat));
                        lboxSIR.Items.Add(SIRList.Last().ToString());

                        messagesList.Add(h, new IncidentEmail(h, senderEmail, IncidentReportSIR, CentreCode, incidentNat, b));
                        messagesUnsaved = true;
                        lboxMessages.Items.Refresh();
                        return true;
                    }
                    else
                        MessageBox.Show("Invalid Nature of Incident Found!", "Invalid Nature of Incident", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    string subject = new StringReader(b).ReadLine();
                    b = EditBody(b, subject);

                    if (subject.Length <= 20)
                    {
                        ExpandAbbreviation(ref b, abbreviations, nOfAbbreviations);
                        QuarantineList(b, ref quarantineed, ref nOfQuarantined);
                        QuarantineURLs(ref b);

                        messagesList.Add(h, new Email(h, senderEmail, subject, b));
                        messagesUnsaved = true;
                        lboxMessages.Items.Refresh();
                        return true;
                    }
                    else
                        MessageBox.Show("Invalid Subject!\nSubject must be <= 20 characters.", "Invalid Subject", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
                MessageBox.Show("Invalid Sender's Email", "Email Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        public bool ProcessTweet(string h, string b)
        {
            if (RegexCheck(@"^@?(\w){1,15}", b))
            {
                string tweetAuthor = ExtractMatch(@"^@?(\w){1,15}", ref b);

                if (MessageLegthLimit(b, 140))
                {
                    ExpandAbbreviation(ref b, abbreviations, nOfAbbreviations);
                    UpdateList(b, ref trendingList, @"(#)\w+");
                    UpdateList(b, ref mentionsList, @"(@)+(\w){1,15}");

                    messagesList.Add(h, new Tweet(h, tweetAuthor, b));
                    messagesUnsaved = true;
                    lboxMessages.Items.Refresh();

                    if (mentionsList.Count > 0 || trendingList.Count > 0)
                        UpdateMentionTrending();

                    return true;
                }
                else
                    BodyOutOfLimit(140);
            }
            else
                MessageBox.Show("Tweet Author not Found!", "Author Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
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
            if (mentionsList.Count > 0)
            {
                mentionsList.Sort((x, y) => x.Occurrences.CompareTo(y.Occurrences));
                mentionsList.Reverse();
                lboxMentions.Items.Refresh();

                lboxMentions.Items.Clear();

                foreach (Mention m in mentionsList)
                    lboxMentions.Items.Add(m.ToString());
            }

            if (trendingList.Count > 0)
            {
                trendingList.Sort((x, y) => x.Occurrences.CompareTo(y.Occurrences));
                trendingList.Reverse();
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
            string processedMessages = JsonConvert.SerializeObject(messagesList, Formatting.Indented);
            string filename = string.Format("Euston Leisure{0}.json", DateTime.UtcNow.ToString("ddMMyyyy_HHmmss"));

            File.WriteAllText(filename, processedMessages);
            messagesUnsaved = false;
            MessageBox.Show("Messages Correctly Saved");
        }        

        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "JSON files (*.json)|*.json";//|All files (*.*)|*.*";

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    string messages = File.ReadAllText(openFileDialog.FileName);
                    Dictionary<string, Message> loadedMessages = JsonConvert.DeserializeObject<Dictionary<string, Message>>(messages);

                    int loadCount = 0;
                    foreach (KeyValuePair<string, Message> m in loadedMessages)
                    {
                        if (ProcessMessage(m.Key, m.Value.Body))
                            loadCount++;
                    }

                    if (loadCount >= 0 && loadedMessages.Count > 0)
                        MessageBox.Show(string.Format("{0} Messages Loaded.\n{1} Unloaded.", loadCount, loadedMessages.Count - loadCount), "Load Information", MessageBoxButton.OK, MessageBoxImage.Information);  
                }
            }
            catch
            {
                MessageBox.Show("Something Went Wrong!\nTry with a valid file.", "Invalid File Format", MessageBoxButton.OK, MessageBoxImage.Error);
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

        #region Others
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

        #endregion
    }

}