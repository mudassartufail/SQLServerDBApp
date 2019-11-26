using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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

namespace SQLServerDBApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string serverName = "";

            // In case you don't write a SQL Server name, it will look for local SQL server
            if (txtServerName.Text == "")
            {
                serverName = "(local)";
            }
            else
            {
                serverName = txtServerName.Text;
            }

            string userName = txtUsername.Text;
            string password = txtPassword.Password;

            if (userName == "")
            {
                MessageBox.Show("Please enter username.");
            }

            if (password == "")
            {
                MessageBox.Show("Please enter password.");
            }

            if (serverName != "" && userName != "" && password != "")
            {
                var connectionString = string.Format("Data Source={0};User ID={1};Password={2};", serverName, userName, password);

                try
                {
                    DataTable databases = null;
                    using (var sqlConnection = new SqlConnection(connectionString))
                    {
                        sqlConnection.Open();
                        databases = sqlConnection.GetSchema("Databases");
                        sqlConnection.Close();
                    }

                    if (databases != null)
                    {
                        SecondaryWindow objSecWin = new SecondaryWindow(connectionString);
                        objSecWin.Show();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
        }
    }
}