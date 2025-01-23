using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArduinoMAZE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> Options = new List<string>(
            new string[] { "Manuel", "Aléatoire", "IA" }
        );
        public MainWindow()
        {
            InitializeComponent();
            CB_Options.Items.Add(Options);
        }

        public void Load_Model()
        {
            // Load the model
        }

        private void BTN_Start_Click(object sender, RoutedEventArgs e)
        {
            BTN_Start.IsEnabled = false;
            BTN_Stop.IsEnabled = true;
            BTN_Reset.IsEnabled = false;


        }

        private void BTN_Stop_Click(object sender, RoutedEventArgs e)
        {
            BTN_Start.IsEnabled = true;
            BTN_Stop.IsEnabled = false;
            BTN_Reset.IsEnabled = true;

        }

        private void BTN_Reset_Click(object sender, RoutedEventArgs e)
        {
            BTN_Reset.IsEnabled = false;
            BTN_Start.IsEnabled = true;
            BTN_Stop.IsEnabled = true;
        }

        private void BTN_Load_Click(object sender, RoutedEventArgs e)
        {
            BTN_Reset.IsEnabled = false;
            BTN_Start.IsEnabled = true;
            BTN_Stop.IsEnabled = false;

            DBConnection_Window db = new DBConnection_Window();
            db.Show();

        }
    }
}