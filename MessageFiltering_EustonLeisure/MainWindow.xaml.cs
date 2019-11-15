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

namespace MessageFiltering_EustonLeisure
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[,] abbrevations = new string[256,2];
        public MainWindow()
        {
            InitializeComponent();
            LoadTextAbbreviation(ref abbrevations);
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
                            string body = tboxBody.Text;

                            if (RegexCheck(@"^((\+\d{1,3}(-| )?\(?\d\)?(-| )?\d{1,5})|(\(?\d{2,6}\)?))(-| )?(\d{3,4})(-| )?(\d{4})(( x| ext)\d{1,5}){0,1}", body))
                            {
                                string senderNumber = ExtractMatch(@"^((\+\d{1,3}(-| )?\(?\d\)?(-| )?\d{1,5})|(\(?\d{2,6}\)?))(-| )?(\d{3,4})(-| )?(\d{4})(( x| ext)\d{1,5}){0,1}", body);

                                int startIndexPN = body.IndexOf(senderNumber);
                                int lengthOfPN = senderNumber.Length;

                                body = body.Remove(startIndexPN, lengthOfPN);

                                if (!MessageLegthLimit(body, 140))
                                {
                                    ExpandAbbreviation(ref body,ref abbrevations);
                                    MessageBox.Show(body);
                                }
                            }
                            else
                                MessageBox.Show("Invalid SMS Body Format\n\nVAlid Format: [SENDER NUMBER][BODY]");
                            break;
                        }

                    case 'E':
                        MessageBox.Show("Email");
                        break;
                    case 'T':
                        MessageBox.Show("Tweet");
                        break;
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

        private string ExtractMatch(string pattern, string message)
        {
            Regex r = new Regex(pattern);
            Match regexMatch = r.Match(message);

            return regexMatch.ToString();
        }

        private bool MessageLegthLimit(string message, int limit)
        {
            if (message.Length > limit)
                return true;
            return false;
        }

        private void LoadTextAbbreviation(ref string[,] textAbb)
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
                    textAbb[i,0] = fields[0];
                    textAbb[i,1] = fields[1];
                    i++;
                }
            }
        }

        private void ExpandAbbreviation(ref string message, ref string[,] abbreviations)
        {
            int count = 0;
            int start = 0;
            int end = message.Length;
            int indexOfAbbreviation = 0;
            while((start < end) && (indexOfAbbreviation > -1))
            {
                count = message.Length - start;
                indexOfAbbreviation = message.IndexOf(abbreviations[0, 0], start, count);
                if (indexOfAbbreviation == -1)
                    break;
                else
                {
                    message = message.Insert(indexOfAbbreviation + abbreviations[0, 0].Length, string.Format(" <{0}> ",abbreviations[0,1]));
                }
                start = indexOfAbbreviation + abbrevations[0, 0].Length + abbrevations[0, 1].Length + 3;
                end = message.Length;
            }
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
