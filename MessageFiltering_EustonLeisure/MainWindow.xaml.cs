using System;
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
        List<Mention> mentionsList = new List<Mention>(); //List containint Tweets ID Mentions
        List<Mention> trendingList = new List<Mention>(); //List containing Hashtags
        List<SIRListEntry> SIRList = new List<SIRListEntry>(); //SIR list

        //This  dictionary wil contain the processed messages. The Key to access the dictionary is the message Header
        Dictionary<string, Message> messagesList = new Dictionary<string, Message>();

        string[] quarantineed = new string[100]; //This array will contain the list of URLs quarantineed
        int nOfQuarantined = 0; //Number of element in the array.

        string[,] abbreviations = new string[256,2]; //List of abbreviations loaded from file
        int nOfAbbreviations = 0; //Number of abbreviations loaded.

        bool messagesUnsaved = true; //Checks if the user has saved the processed messages.
        
        public MainWindow()
        {
            InitializeComponent();
            lboxMessages.ItemsSource = messagesList.Keys; //Link a ListBox to the Dictionary containing the processed messages.
                                                          //The message header will be showed.
            //Load Abbreviation from file
            LoadTextAbbreviation(ref abbreviations, ref nOfAbbreviations);
        }

        //Validation Methods
        #region Validation
        //This method is used to validate strings. If the string matches the Regex pattern TRUE is returned.
        //e.g. T123456789 matches pattern ((?:[S|E|T]+[0-9]{9})) returns TRUE
        //The same method i used to velidate Header, Sender, SIR, Hashtags and Tweet ID (using different patterns)
        public bool RegexCheck(string pattern, string message)
        {
            Regex r = new Regex(pattern);
            Match regexMatch = r.Match(message);

            if (regexMatch.Success)
                return true;
            return false;
        }

        //This method check if the Email message contains a valid Nature of Incident.
        //If the Nature of Incident in the email is contaned in the File the method returns TRUE.
        public bool ValidIncidentNature(string inc)
        {
            //Load Incidents List from file
            string[] eachIncident = System.IO.File.ReadAllLines("IncidentsList.txt");

            foreach (string i in eachIncident)
            {
                if (i == inc)
                    return true;
            }
            return false;
        }

        //This method checks if the body of the message respect the length limit described in the functional requirements
        private bool MessageLegthLimit(string message, int limit)
        {
            if (message.Length < limit)
                return true;
            return false;
        }

        //This method display a message to the user informing that the body length limit has been reached.
        private void BodyOutOfLimit(int limit)
        {
            MessageBox.Show(string.Format("The body of the message exceed the characters limit [{0}]!", limit));
        }
        #endregion

        //Process Messages Methods
        #region Process Messages
        //This method start the Message Process when the button has been pressed
        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (RegexCheck("((?:[S|E|T]+[0-9]{9}))", tboxHeader.Text.ToUpper())) //Validating Header
            {
                if (ProcessMessage(tboxHeader.Text, tboxBody.Text)) //Process Message
                {
                    //If the message has been correctly processed, the process result is displayed in a new window
                    //the two textboxes ar emptied.
                    DisplayMessage displayMessage = new DisplayMessage(messagesList[tboxHeader.Text.ToUpper()]);
                    displayMessage.ShowDialog();
                    tboxHeader.Text = string.Empty;
                    tboxBody.Text = string.Empty;
                }
            }
            else
                MessageBox.Show("Invalid Header Format!");
        }

        //This method process the message
        public bool ProcessMessage(string h, string b)
        {
            h = h.ToUpper(); //Header. I use ToUpper() so the first letter ofthe HEader will be capital
            if (RegexCheck("((?:[S|E|T]+[0-9]{9}))", h)) //If header is valid
            {
                if (!messagesList.ContainsKey(h)) //Check if the dictionary have already a message with this header code.
                {                    
                    switch (h[0]) //Process method depend from the first letter fo the header.
                    {
                        case 'S':
                            {
                                return (ProcessSMS(h, b)); //Process SMS
                            }

                        case 'E':
                            {
                                return (ProcessEmail(h, b)); //Process Email
                            }
                        case 'T':
                            {
                                return (ProcessTweet(h, b)); //Process Tweet
                            }
                    }
                }
                else //The message header is already in the Disctionary. (dupliate message)
                    MessageBox.Show("There is already a message with this Header Value!", "Recurring Header", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
                MessageBox.Show("{0} is an incorrect header format", "Header Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        //Method to process a SMS message
        public bool ProcessSMS(string h, string b)
        {
            //String containing the regex pattern to validate the phone number
            string RegexPhone = @"^((\+\d{1,3}(-| )?\(?\d\)?(-| )?\d{1,5})|(\(?\d{2,6}\)?))(-| )?(\d{3,4})(-| )?(\d{4})(( x| ext)\d{1,5}){0,1}";
            if (RegexCheck(RegexPhone, b)) //If the sender number is valid
            {                
                string senderNumber = ExtractMatch(RegexPhone, ref b); //Remove phone number from the message body

                if (MessageLegthLimit(b, 140)) //Check if the message length is inside the limits
                {                    
                    ExpandAbbreviation(ref b, abbreviations, nOfAbbreviations); //Expand abbreviations
                    messagesList.Add(h, new Message(h, senderNumber, b)); //Adding processed message to dictionary.
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

        //Method to process an Email message
        public bool ProcessEmail(string h, string b)
        {
            //Regex pattern to validate email address
            string regexEmail = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))";

            if (RegexCheck(regexEmail, b)) //If sender email is valid
            {
                string senderEmail = ExtractMatch(regexEmail, ref b); //Removing sender email from body

                //Regex pattern to validate SIR Incident e.g. SIR 12/02/2019
                string RegexSIR = @"(SIR (0[1-9]|1[0-9]|2[0-9]|3[0-1])[\/](0[1-9]|1[0-2])[\/](0[0-9]|1[0-9]|2[0-9]))";

                if (RegexCheck(RegexSIR, b)) //If we found a valid SIR
                {
                    string IncidentReportSIR = ExtractMatch(RegexSIR, ref b); //Removing SIR from the message body

                    string RegexCentreCode = @"(\d\d)+(-)+(\d\d\d)+(-)+(\d\d)"; //Regex pattern to validate the Centro Code e.g. 11-252-014
                    string CentreCode = ExtractMatch(RegexCentreCode, ref b); //Removing the Centre code from the mesage body

                    string incidentNat = new StringReader(b).ReadLine(); //Reading the Nature of Incident
                    b = EditBody(b, incidentNat); //Removing the NAture of Incident from the body

                    if (ValidIncidentNature(incidentNat)) //Validating the Nature of Incident
                    {
                        if (MessageLegthLimit(b, 1028))
                        {
                            ExpandAbbreviation(ref b, abbreviations, nOfAbbreviations); //Expanding abbreviations
                            QuarantineList(b, ref quarantineed, ref nOfQuarantined); //Adding URLs to Quarantined list
                            QuarantineURLs(ref b); //Replacing URLs with "<URL Quarantineed>"

                            SIRList.Add(new SIRListEntry(CentreCode, incidentNat)); //Creating objec SIREntry
                            lboxSIR.Items.Add(SIRList.Last().ToString()); //Adding SIR to listBox

                            //Adding processed message to dictionary
                            messagesList.Add(h, new IncidentEmail(h, senderEmail, IncidentReportSIR, CentreCode, incidentNat, b));
                            messagesUnsaved = true;
                            lboxMessages.Items.Refresh();
                            return true;
                        }
                        else
                            BodyOutOfLimit(1028);
                        
                    }
                    else
                        MessageBox.Show("Invalid Nature of Incident Found!", "Invalid Nature of Incident", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    //If message does not contain a SIR, this should be a standard email.
                    string subject = new StringReader(b).ReadLine(); //Contains the email subject
                    b = EditBody(b, subject); //Check if the subject length limit is met
                
                    if (subject.Length <= 20 && subject.Length > 0)
                    {
                        if (MessageLegthLimit(b, 1028))
                        {
                            ExpandAbbreviation(ref b, abbreviations, nOfAbbreviations); //Expanding abbreviations
                            QuarantineList(b, ref quarantineed, ref nOfQuarantined); //Adding URLs to Quarantined list
                            QuarantineURLs(ref b); //Replacing URLs with "<URL Quarantineed>"

                            messagesList.Add(h, new Email(h, senderEmail, subject, b)); //Adding processed message to dictionary
                            messagesUnsaved = true;
                            lboxMessages.Items.Refresh();
                            return true;
                        }
                        else
                            BodyOutOfLimit(1028);
                        
                    }
                    else
                        MessageBox.Show("Invalid Subject!\nSubject must be <= 20 characters.", "Invalid Subject", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
                MessageBox.Show("Invalid Sender's Email", "Email Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        //Method to process a Tweet message
        public bool ProcessTweet(string h, string b)
        {
            if (RegexCheck(@"^(@)+(\w){1,15}", b)) //Check is the author is a valid Tweet ID
            {
                string tweetAuthor = ExtractMatch(@"^@?(\w){1,15}", ref b); //Removing the tweet author from the message body

                if (MessageLegthLimit(b, 140)) //Check is the message length limit is met
                {
                    ExpandAbbreviation(ref b, abbreviations, nOfAbbreviations); //Expanding abbreviations
                    UpdateList(b, ref trendingList, @"(#)\w+"); //Updating trending list
                    UpdateList(b, ref mentionsList, @"(@)+(\w){1,15}"); //Updating mentions list

                    messagesList.Add(h, new Tweet(h, tweetAuthor, b)); //Adding message to the dictionary
                    messagesUnsaved = true;
                    lboxMessages.Items.Refresh();

                    if (mentionsList.Count > 0 || trendingList.Count > 0)
                        UpdateMentionTrending(); //Update listBoxes if new mentions have been found in the message

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

        //Message Body Editing
        #region BodyEditing
        //This method remove a matched string fro the body
        private string EditBody(string message, string toRemove)
        {
            int startIndexPN = message.IndexOf(toRemove);
            int lengthOfPN = toRemove.Length;

            string editedMessage = message.Remove(startIndexPN, lengthOfPN);
            RemoveEmptyLine(ref editedMessage);

            return editedMessage;
        }

        //This method expand the textspeak abbreviations
        protected void ExpandAbbreviation(ref string message, string[,] abbs, int nAbb)
        {
            for (int i = 0; i < nAbb; i++)
            {
                Regex anAbbreviation = new Regex(@"(?<![\w])" + abbs[i, 0] + @"(?![\w])", RegexOptions.IgnoreCase);
                foreach (Match m in anAbbreviation.Matches(message))
                    message = message.Insert(m.Index + abbs[i, 0].Length, string.Format(" <{0}> ", abbs[i, 1]));
            }
        }

        //This method mathes a URL and remove it from the message body.
        //The URL is replaced by "<URL Quarantined>"
        protected void QuarantineURLs(ref string message)
        {
            string URL = (@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)");
            string replacement = ("<URL Quarantined>");
            message = Regex.Replace(message, URL, replacement);
        }

        //This method removes empty lines.
        private void RemoveEmptyLine(ref string message)
        {
            while (message[0] == '\n' || message[0] == '\r')
                message = message.Remove(0, 1);
        }

        //This method return a string that matched a regex pattern
        //This is used to extract strings like sender number, email address, etc to be saved in a variable
        private string ExtractMatch(string pattern, ref string message)
        {
            Regex r = new Regex(pattern);
            Match regexMatch = r.Match(message);

            string extractedString = regexMatch.ToString();

            message = EditBody(message, extractedString);

            return extractedString;
        }
        #endregion

        //Methods to update the mention and trending list
        #region UpdateLists
        private void UpdateList(string body, ref List<Mention> trendingL, string pattern)
        {            
            Regex mention = new Regex(pattern); //Create a list of mathes
            foreach (Match mtc in mention.Matches(body))
            {
                bool alreadyMentioned = false;
                foreach (Mention m in trendingL) //For every match
                {
                    if (mtc.Value.ToString() == m.MentionText) //Check if the mention is already in the list
                    {
                        m.Occurrences++;
                        alreadyMentioned = true;
                        break;
                    }
                }
                if (!alreadyMentioned)
                    trendingL.Add(new Mention(mtc.Value.ToString())); //Adds a new mention to the mention list
            }
        }

        //This method adds matched URL to the quarantine list
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

        //UI Updates (listBoxes)
        #region UIUpdate
        //This method updates mention and trending listBox
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

        //Methids to Load and Save files
        #region SaveLoad Files
        //Load abreviation from File
        public void LoadTextAbbreviation(ref string[,] textAbb, ref int nAbb)
        {
            try
            {
                var fileLocation = @"C:\Users\Marco\source\repos\MessageFiltering_EustonLeisure\MessageFiltering_EustonLeisure\TextWords.csv";
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
            catch
            {
                MessageBox.Show("Impossible to Load the Abbreviations File. Please Fix the FilePath on Line 415.\n\nThe program will be now closed.", "Missing File", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        //Sae processed messages to JSON file
        private void BtnSaveMessages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Serialize the Dictionary to a JSON string
                string processedMessages = JsonConvert.SerializeObject(messagesList, Formatting.Indented);
                //Create a filename e.g. Euston Leisure26112019_1220.json
                string filename = string.Format("Euston Leisure{0}.json", DateTime.UtcNow.ToString("ddMMyyyy_HHmmss"));

                //Writing the content of the JSON string into the file
                File.WriteAllText(filename, processedMessages);
                messagesUnsaved = false;
                MessageBox.Show(string.Format("Messages Correctly Saved\nFIleName: {0}", filename));
            }
            catch
            {
                MessageBox.Show("An error occurred while creating the file!", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }        

        //This method load the unprocessed messages from file (TXT or JSON)
        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            //Open a FileDialog at the current location (Where the EXE file is)
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Multiselect = false; //Only one file at the time can be selected
            //Set filter to allow the user to open only TXT or JSON
            openFileDialog.Filter = "JSON files (*.json)|*.json|TXT files (*.txt)|*.txt";

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {                    
                    int loadCount = 0;

                    if (openFileDialog.FileName.EndsWith("json"))
                    {
                        string messages = File.ReadAllText(openFileDialog.FileName);

                        Dictionary<string, Message> loadedMessages = JsonConvert.DeserializeObject<Dictionary<string, Message>>(messages);
                        foreach (KeyValuePair<string, Message> m in loadedMessages)
                        {
                            if (ProcessMessage(m.Key, m.Value.Body))
                            {
                                DisplayMessage displayMessage = new DisplayMessage(messagesList[m.Key]);
                                displayMessage.ShowDialog();
                                loadCount++;
                            }
                        }
                        if (loadCount >= 0 && loadedMessages.Count > 0)
                            MessageBox.Show(string.Format("{0} Messages Loaded.\n{1} Unloaded.", loadCount, loadedMessages.Count - loadCount), "Load Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (openFileDialog.FileName.EndsWith("txt"))
                    {
                        string[] messages = File.ReadAllLines(openFileDialog.FileName);

                        int i = 0;
                        while (i < messages.Count())
                        {
                            if (messages[i] == "***START***")
                            {
                                string newMessage = string.Empty;
                                i++;
                                string messageHeader = messages[i];
                                i++;
                                while (messages[i] != "***END***" && i < messages.Count())
                                {
                                    newMessage += messages[i] + "\n";
                                    i++;
                                }
                                if (ProcessMessage(messageHeader, newMessage))
                                {
                                    DisplayMessage displayMessage = new DisplayMessage(messagesList[messageHeader]);
                                    displayMessage.ShowDialog();
                                    loadCount++;
                                }
                            }
                            i++;
                        }
                        MessageBox.Show(string.Format("{0} Messages Loaded from the TXT file.", loadCount), "Load Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                        MessageBox.Show("File Not Processed!");
                    
                }
            }
            catch
            {
                MessageBox.Show("Something Went Wrong!\nTry with a valid file.", "Invalid File Format", MessageBoxButton.OK, MessageBoxImage.Error);
            }           
        }
        #endregion

        //Methods to display messages to the user
        #region DisplayMessage
        //This method shows a new window to the user with the processed message
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