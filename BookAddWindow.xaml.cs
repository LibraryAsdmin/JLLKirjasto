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
    /// Interaction logic for BookAddWindow.xaml
    /// </summary>
    public partial class BookAddWindow : Window
    {
        public BookAddWindow()
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
        String bookstable = "books";

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            // Verify that the ID isn't taken

            // specifies that searching IDs
            List<string> columns = new List<string>();
            columns.Add("ID");

            // search id in IDBox from the database
            List<List<string>> results = new List<List<string>>();
            results = dbi.searchDatabaseRows(dbconnection, bookstable, IDBox.Text, columns);

            // verify that the ID is properly formatted
            if (IsDigitsOnly(IDBox.Text) == false || IDBox.Text.Length != 6)
            {
                MessageBox.Show(String.Format(Properties.Resources.ResourceManager.GetString("BookEditIDError", TranslationSource.Instance.CurrentCulture), IDBox.Text));
                return;
            }

            // The ID has to be unique
            if (results.Count == 0)
            {
                String[] addBookColumns = new String[(int)Book.columnID.NumColumns - 1];
                addBookColumns[0] = AuthorBox.Text;
                addBookColumns[1] = TitleBox.Text;
                addBookColumns[2] = YearBox.Text;
                addBookColumns[3] = LanguageBox.Text;
                addBookColumns[4] = AmountBox.Text;
                addBookColumns[5] = AvailableBox.Text;
                addBookColumns[6] = ISBNBox.Text;
                addBookColumns[7] = CategoryBox.Text;


                List<String> addBookColumnsList = new List<String>(addBookColumns);

                dbi.addDatabaseRow(dbconnection, bookstable, IDBox.Text, addBookColumnsList);

                var parentWindow = this.Owner as AdminControlsWindow;
                parentWindow.searchBooks();
                this.Close();
            }
            else
            {
                // Notify the user that the ID conflicts with existing ones
                MessageBox.Show(String.Format(Properties.Resources.ResourceManager.GetString("BookEditIDConflict", TranslationSource.Instance.CurrentCulture), IDBox.Text));
                return;
            }
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
