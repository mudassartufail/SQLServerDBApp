using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SQLServerDBApp
{
    /// <summary>
    /// Interaction logic for SecondaryWindow.xaml
    /// </summary>
    public partial class SecondaryWindow : Window
    {
        public string connectionSring = "";
        public int checkedDatabases = 0;
        //the Signature of the ProgressBar's SetValue method
        private delegate void UpdateProgressBarDelegate(System.Windows.DependencyProperty dp, Object value);
        public ObservableCollection<BoolStringClass> DatabaseLists { get; set; }

        public SecondaryWindow()
        {
            InitializeComponent();
        }

        public SecondaryWindow(string secWin)
        {
            this.connectionSring = secWin;
            InitializeComponent();
            CreateCheckBoxList();
        }

        public void CreateCheckBoxList()
        {
            btnRestore.IsEnabled = false;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            DatabaseLists = new ObservableCollection<BoolStringClass>();

            DataTable ft = null;
            using (var sqlConnection = new SqlConnection(this.connectionSring))
            {
                sqlConnection.Open();
                ft = sqlConnection.GetSchema("Databases");
                sqlConnection.Close();
            }

            DataView dv = ft.DefaultView;
            dv.Sort = "database_name asc";
            DataTable databases = dv.ToTable();

            if (databases != null)
            {
                foreach (DataRow row in databases.Rows)
                {
                    // Ignoring system databases
                    if (row["database_name"].ToString() != "master" && row["database_name"].ToString() != "tempdb" && row["database_name"].ToString() != "model" && row["database_name"].ToString() != "msdb" && row["database_name"].ToString() != "ReportServer" && row["database_name"].ToString() != "ReportServerTempDB")
                    {
                        // Add Database names to a list to be binded to front end
                        DatabaseLists.Add(new BoolStringClass { TheText = row["database_name"].ToString(), isChecked = false });
                    }
                }
            }
            this.DataContext = this;
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            // Open File Dialog box to select only files with .bak or All files filter
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Text files (*.bak)|*.bak|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            List<string> dbList = new List<string>();

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    dbList.Add(filename);
                }
            }

            if (dbList.Count > 0)
            {
                var sqlConStrBuilder = new SqlConnectionStringBuilder(this.connectionSring);

                //Configure the ProgressBar
                progressBar.Minimum = 0;
                progressBar.Value = 0;
                progressBar.Maximum = listDatabases.Items.Count;

                //Stores the value of the ProgressBar
                double value = 0;

                //Create a new instance of our ProgressBar Delegate that points
                // to the ProgressBar's SetValue method.
                UpdateProgressBarDelegate updatePbDelegate =
                    new UpdateProgressBarDelegate(progressBar.SetValue);

                bool flag = false;
                using (var connection = new SqlConnection(sqlConStrBuilder.ConnectionString))
                {
                    connection.Open();

                    if (dbList.Count > 0)
                    {
                        for (int i = 0; i < listDatabases.Items.Count; i++)
                        {
                            int counter = 0;
                            if (((BoolStringClass)listDatabases.Items[i]).isChecked)
                            {
                                string dbName = ((BoolStringClass)listDatabases.Items[i]).TheText;
                                string dbFile = dbList[counter];

                                if (dbFile.Contains(dbName))
                                {
                                    flag = true;
                                    counter++;

                                    // Altering database to set single user
                                    var qry = String.Format("alter database [{0}] set single_user with rollback immediate", dbName);
                                    using (var command = new SqlCommand(qry, connection))
                                    {
                                        command.ExecuteNonQuery();
                                    }

                                    // Restore SQL Server query
                                    var query = String.Format("DECLARE @SQLStatement VARCHAR(2000) SET @SQLStatement = '{0}' RESTORE DATABASE [{1}] FROM DISK = @SQLStatement WITH REPLACE", dbFile, dbName);

                                    using (var command = new SqlCommand(query, connection))
                                    {
                                        command.ExecuteNonQuery();
                                    }

                                    // Changing database back to multi user
                                    var qry2 = String.Format("ALTER DATABASE [{0}] SET MULTI_USER", dbName);
                                    using (var command = new SqlCommand(qry2, connection))
                                    {
                                        command.ExecuteNonQuery();
                                    }

                                    value += 1;

                                    /*Update the Value of the ProgressBar:
                                        1) Pass the "updatePbDelegate" delegate
                                           that points to the ProgressBar1.SetValue method
                                        2) Set the DispatcherPriority to "Background"
                                        3) Pass an Object() Array containing the property
                                           to update (ProgressBar.ValueProperty) and the new value */
                                    Dispatcher.Invoke(updatePbDelegate,
                                        System.Windows.Threading.DispatcherPriority.Background,
                                        new object[] { ProgressBar.ValueProperty, value });
                                }
                                else
                                {
                                    flag = false;
                                    MessageBox.Show("Irrelevant Database file selected!!");
                                }
                            }
                        }

                        connection.Close();
                    }
                }

                if (flag)
                {
                    Dispatcher.Invoke(updatePbDelegate,
                                       System.Windows.Threading.DispatcherPriority.Background,
                                       new object[] { ProgressBar.ValueProperty, progressBar.Maximum });

                    MessageBox.Show("Database Restored Successfully!");

                    this.Close();
                }
            }
        }

        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog browser = new System.Windows.Forms.FolderBrowserDialog();
            var backupFolder = "";

            if (browser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                backupFolder = browser.SelectedPath + "/";
                bool exists = System.IO.Directory.Exists(backupFolder);

                if (!exists)
                    System.IO.Directory.CreateDirectory(backupFolder);

                var sqlConStrBuilder = new SqlConnectionStringBuilder(this.connectionSring);

                //Configure the ProgressBar
                progressBar.Minimum = 0;
                progressBar.Value = 0;
                progressBar.Maximum = listDatabases.Items.Count;

                //Stores the value of the ProgressBar
                double value = 0;
                int counter = 0;

                //Create a new instance of our ProgressBar Delegate that points to the ProgressBar's SetValue method.
                UpdateProgressBarDelegate updatePbDelegate =
                    new UpdateProgressBarDelegate(progressBar.SetValue);

                using (var connection = new SqlConnection(sqlConStrBuilder.ConnectionString))
                {
                    connection.Open();

                    for (int i = 0; i < listDatabases.Items.Count; i++)
                    {
                        string dbName = "";

                        if (((BoolStringClass)listDatabases.Items[i]).isChecked)
                        {
                            try
                            {
                                counter++;
                                dbName = ((BoolStringClass)listDatabases.Items[i]).TheText;

                                lblProgressMsg.Content = "Current Database: " + dbName;

                                // Create Database Backup in a selected directory and assign it with a timestamp
                                var backupFileName = String.Format("{0}{1}-{2}.bak", backupFolder, dbName, DateTime.Now.ToString("yyyyMMddHHmmss"));
                                var query = String.Format("DECLARE @SQLStatement VARCHAR(2000) SET @SQLStatement = '{0}' BACKUP DATABASE [{1}] TO DISK = @SQLStatement", backupFileName, dbName);

                                // Executing query
                                using (var command = new SqlCommand(query, connection))
                                {
                                    command.CommandTimeout = 0;
                                    command.ExecuteNonQuery();
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }

                        value += 1;

                        lblCounterMsg.Content = value.ToString() + "/" + listDatabases.Items.Count;

                        /*Update the Value of the ProgressBar:
                            1) Pass the "updatePbDelegate" delegate
                               that points to the ProgressBar1.SetValue method
                            2) Set the DispatcherPriority to "Background"
                            3) Pass an Object() Array containing the property
                               to update (ProgressBar.ValueProperty) and the new value */
                        Dispatcher.Invoke(updatePbDelegate,
                            System.Windows.Threading.DispatcherPriority.Background,
                            new object[] { ProgressBar.ValueProperty, value });
                    }

                    connection.Close();

                    lblProgressMsg.Content = "";
                    lblCounterMsg.Content = "";
                }

                Dispatcher.Invoke(updatePbDelegate,
                            System.Windows.Threading.DispatcherPriority.Background,
                            new object[] { ProgressBar.ValueProperty, progressBar.Maximum });

                MessageBox.Show("Backup Completed Successfully!");

                this.Close();
            }
        }

        private void chkSelectUnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (chkSelectUnSelect.IsChecked.Value == true)
            {
                for (int i = 0; i < listDatabases.Items.Count; i++)
                {
                    ((BoolStringClass)listDatabases.Items[i]).isChecked = true;
                }
                listDatabases.Items.Refresh();
            }
            else
            {
                for (int i = 0; i < listDatabases.Items.Count; i++)
                {
                    ((BoolStringClass)listDatabases.Items[i]).isChecked = false;
                }
                checkedDatabases = 0;
                listDatabases.Items.Refresh();
            }
        }

        private void chkRestoreMode_Click(object sender, RoutedEventArgs e)
        {
            if (chkRestoreMode.IsChecked.Value == true)
            {
                btnRestore.IsEnabled = true;
                btnBackup.IsEnabled = false;
                listDatabases.SelectionMode = System.Windows.Controls.SelectionMode.Single;
            }
            else
            {
                btnRestore.IsEnabled = false;
                btnBackup.IsEnabled = true;
                listDatabases.SelectionMode = System.Windows.Controls.SelectionMode.Multiple;
            }
        }
    }

    public class BoolStringClass
    {
        public string TheText { get; set; }
        public bool isChecked { get; set; }
    }
}

