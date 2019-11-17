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
using System.Windows.Shapes;

namespace MessageFiltering_EustonLeisure
{
    /// <summary>
    /// Interaction logic for DisplayMessage.xaml
    /// </summary>
    public partial class DisplayMessage : Window
    {
        public DisplayMessage(Message m)
        {
            InitializeComponent();
            HideBoxes();

            tboxHeader.Text = m.Header;
            tboxSender.Text = m.Sender;
            tboxBody.Text = m.Body;

            MessageBox.Show(m.GetType().ToString());

            switch (m.GetType().ToString())
            {
                case "MessageFiltering_EustonLeisure.Email":
                    {
                        Email thisMessage = m as Email;

                        tblockSubject.Visibility = Visibility.Visible;
                        tboxSubject.Visibility = Visibility.Visible;
                        
                        tboxSubject.Text = thisMessage.Subject;                                                                     

                        break;
                    }
                case "MessageFiltering_EustonLeisure.IncidentEmail":
                    {
                        IncidentEmail thisMessage = m as IncidentEmail;

                        ShowBoxes();

                        tboxSubject.Text = thisMessage.Subject;
                        tboxCode.Text = thisMessage.Code;
                        tboxIncident.Text = thisMessage.Incident;

                        break;
                    }
            }
        }

        private void HideBoxes()
        {
            tblockCode.Visibility = Visibility.Hidden;
            tblockIncident.Visibility = Visibility.Hidden;
            tblockSubject.Visibility = Visibility.Hidden;

            tboxCode.Visibility = Visibility.Hidden;
            tboxIncident.Visibility = Visibility.Hidden;
            tboxSubject.Visibility = Visibility.Hidden;
        }

        private void ShowBoxes()
        {
            tblockCode.Visibility = Visibility.Visible;
            tblockIncident.Visibility = Visibility.Visible;
            tblockSubject.Visibility = Visibility.Visible;

            tboxCode.Visibility = Visibility.Visible;
            tboxIncident.Visibility = Visibility.Visible;
            tboxSubject.Visibility = Visibility.Visible;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
