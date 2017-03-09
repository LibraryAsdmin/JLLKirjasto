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

        // verify that a string contains only digits
        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
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

                // verify that the loan book IDs are valid
                
                // create a User object for extracting individual IDs with getLoans
                User user = new User(ID, WilmaBox.Text, LoansBox.Text);
                List<String> loans = user.getLoans();

                List<int> removeIndex = new List<int>();
                for (int i = 0; i < loans.Count; i++)
                {
                    // verify that the loan id has correct length and is numeric value
                    if (IsDigitsOnly(loans[i]) == false || loans[i].Length != 6)
                    {
                        MessageBox.Show(String.Format("Warning: The book ID {0} is invalid. This entry will be ignored..", loans[i]));
                        removeIndex.Add(i);
                    } 
                }

                // Filter invalid IDs marked to removeIndex from loans
                // Has to be done this way because removing an entry changes the indices of the rest
                List<String> legitIDs = new List<String>();
                for (int i = 0; i < loans.Count; i++)
                {
                    if (removeIndex.Contains(i))
                    {
                        continue;
                    }
                    legitIDs.Add(loans[i]);
                }

                // convert the filtered list to a csv string
                String loans_csv = "";
                // if there are more than zero books remaining
                if (legitIDs.Count > 0)
                {
                    loans_csv = legitIDs[0];
                    for (int i = 1; i < legitIDs.Count; i++)
                    {
                        loans_csv += String.Format(";{0}", legitIDs[i]);
                    }
                }

                dbi.commitDbChanges(dbconnection, userstable, loans_csv, ID, User.columnNames[(int)User.columnID.Loans]);

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
