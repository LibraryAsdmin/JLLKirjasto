using System;
using System.Collections.Generic;
using System.Data.SQLite;
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

namespace JLLKirjasto
{
    /// <summary>
    /// Interaction logic for UserEditWindow.xaml
    /// </summary>
    public partial class UserEditWindow : Window
    {
        public UserEditWindow()
        {
            InitializeComponent();
        }

        // Variables for interaction with the database
        SQLiteConnection dbconnection = new SQLiteConnection("Data Source=database.db");
        DatabaseInteraction dbi = new DatabaseInteraction();
        String userstable = "users";

        // store previous wilma address
        private String oldWilma;
        private String ID;

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            // Verify that the Wilma address isn't taken

            // specifies that searching Wilma addresses
            List<string> columns = new List<string>();
            columns.Add("Wilma");

            // search id in IDBox from the database
            List<List<string>> results = new List<List<string>>();
            results = dbi.searchDatabaseRows(dbconnection, userstable, WilmaBox.Text, columns);


            // The Wilma address has to either match the old address or be unique
            if (results.Count == 0 || results.Count == 1 && results[0][(int)User.columnID.Wilma] == oldWilma)
            {
                // Commit changes to each column individually by ID
                dbi.commitDbChanges(dbconnection, userstable, WilmaBox.Text, ID, User.columnNames[(int)User.columnID.Wilma]);
                dbi.commitDbChanges(dbconnection, userstable, LoansBox.Text, ID, User.columnNames[(int)User.columnID.Loans]);

                var parentWindow = this.Owner as AdminControlsWindow;
                parentWindow.searchUsers();
                this.Close();
            }
            else
            {
                // notify the user that there is something wrong with the new book ID
                MessageBox.Show(String.Format(Properties.Resources.ResourceManager.GetString("UserEditWilmaError", TranslationSource.Instance.CurrentCulture), WilmaBox.Text));

            }
        }

        public void setOldWilma(String address)
        {
            oldWilma = address;
        }

        public void setOldID(String id)
        {
            ID = id;
        }

    }
}
