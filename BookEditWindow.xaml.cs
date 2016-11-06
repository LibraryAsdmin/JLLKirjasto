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
using System.Data.SQLite;

namespace JLLKirjasto
{
    /// <summary>
    /// Interaction logic for BookEditWindow.xaml
    /// </summary>
    public partial class BookEditWindow : Window
    {
        public BookEditWindow()
        {
            InitializeComponent();
        }

        // Variables for interaction with the database
        SQLiteConnection dbconnection = new SQLiteConnection("Data Source=database.db");
        DatabaseInteraction dbi = new DatabaseInteraction();
        String bookstable = "books";

        // ID in the database before any edits are made
        private string oldID;

        #region UI Handling
        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            // Verify that the ID isn't taken

            // specifies that searching IDs
            List<string> columns = new List<string>();
            columns.Add("ID");

            // search id in IDBox from the database
            List<List<string>> results = new List<List<string>>();
            results = dbi.searchDatabaseRows(dbconnection, bookstable, IDBox.Text, columns);
            

            // The ID has to either match the old ID or be unique
            if (results.Count == 0 || results.Count == 1 && results[0][(int)Book.columnID.ID]==oldID)
            {
                // Commit changes to each column individually by ID
                dbi.commitDbChanges(dbconnection, bookstable, AuthorBox.Text, IDBox.Text, Book.columnNames[(int)Book.columnID.Author]);
                dbi.commitDbChanges(dbconnection, bookstable, TitleBox.Text, IDBox.Text, Book.columnNames[(int)Book.columnID.Title]);
                dbi.commitDbChanges(dbconnection, bookstable, YearBox.Text, IDBox.Text, Book.columnNames[(int)Book.columnID.Year]);
                dbi.commitDbChanges(dbconnection, bookstable, LanguageBox.Text, IDBox.Text, Book.columnNames[(int)Book.columnID.Language]);
                dbi.commitDbChanges(dbconnection, bookstable, AvailableBox.Text, IDBox.Text, Book.columnNames[(int)Book.columnID.Available]);
                dbi.commitDbChanges(dbconnection, bookstable, ISBNBox.Text, IDBox.Text, Book.columnNames[(int)Book.columnID.ISBN]);
                dbi.commitDbChanges(dbconnection, bookstable, CategoryBox.Text, IDBox.Text, Book.columnNames[(int)Book.columnID.Category]);
                this.Close();
            }
            else
            {
                // notify the user that there is something wrong with the new book ID
                MessageBox.Show(String.Format(Properties.Resources.ResourceManager.GetString("BookEditIDError", TranslationSource.Instance.CurrentCulture),IDBox.Text));
                
            }



        }
        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public void setOldID(string id)
        {
            oldID = id;
        }

        #endregion
    }
}
