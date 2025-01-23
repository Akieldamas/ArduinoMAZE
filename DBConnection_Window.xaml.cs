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

namespace ArduinoMAZE
{
    /// <summary>
    /// Logique d'interaction pour DBConnection_Window.xaml
    /// </summary>
    public partial class DBConnection_Window : Window
    {
        public DBConnection_Window()
        {
            InitializeComponent();
        }

        private void BTN_Connect_Click(object sender, RoutedEventArgs e)
        {
            string server = TB_Server.Text;
            string database = TB_DB.Text;
            string user = TB_User.Text;
            string password = TB_Password.Text;
            string port = TB_Port.Text;

            string connectionstring = $"server={server};database={database};user={user};password={password};port={port};";
           // Application.Current.MainWindow.Load_Model();
            //try
            //{
            //    using (MySQLConnection connection = new MySqlConnection(connectionString))
            //    {
            //        connection.Open();
            //        MessageBox.Show("Connection successful");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Connection failed: " + ex.Message);
            //}

        }
    }
}
