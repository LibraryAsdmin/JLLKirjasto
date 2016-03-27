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
    /// Interaction logic for AdminControlsWindow.xaml
    /// </summary>
    public partial class AdminControlsWindow : Window
    {

        #region Variables
        bool IDChecked = true;
        bool TitleChecked = true;
        bool AuthorChecked = true;
        bool YearChecked = false;
        bool AvailableChecked = false;
        bool LanguageChecked = false;

        SQLiteConnection dbconnection = new SQLiteConnection("Data Source=database.db");
        DatabaseInteraction dbi = new DatabaseInteraction();
        #endregion

        public AdminControlsWindow()
        {
            InitializeComponent();
            initBooksDataGrid();
            initCheckBoxes();
        }

        private void BooksSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update Books datagrid

            // Determine what to search
            List<String> columns = new List<String>();
            if (IDChecked) columns.Add("BookID");
            if (TitleChecked) columns.Add("Title");
            if (AuthorChecked) columns.Add("Author");
            if (YearChecked) columns.Add("Year");
            if (LanguageChecked) columns.Add("Language");
            if (AvailableChecked) columns.Add("Available");
            
            List<List<String>> results = dbi.searchDatabaseRows(dbconnection, "books", BooksSearch.Text, columns);
            List<Book> books = new List<Book>();
            // Populate ItemsSource with book details
            foreach (List<String> row in results)
            {
                books.Add(new Book { BookID = row[0], Author=row[1], Title=row[2], Year=row[3], Language=row[4], Available=row[5]});
            }
            BookDataGrid.ItemsSource = books;
        }

        #region UI Handling
        private void IDCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IDChecked = true;
        }

        private void AuthorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AuthorChecked = true;
        }

        private void TitleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TitleChecked = true;
        }

        private void YearCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            YearChecked = true;
        }

        private void LanguageCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LanguageChecked = true;
        }

        private void AvailableCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AvailableChecked = true;
        }

        private void IDCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IDChecked = false;

        }

        private void AuthorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AuthorChecked = false;
        }

        private void TitleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            TitleChecked = false;
        }

        private void YearCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            YearChecked = false;
        }

        private void LanguageCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LanguageChecked = false;
        }

        private void AvailableCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AvailableChecked = false;
        }

        private void initCheckBoxes()
        {
            if (IDChecked) IDCheckBox.IsChecked = true;
            if (TitleChecked) TitleCheckBox.IsChecked = true;
            if (AuthorChecked) AuthorCheckBox.IsChecked = true;
            if (YearChecked) YearCheckBox.IsChecked = true;
            if (LanguageChecked) LanguageCheckBox.IsChecked = true;
            if (AvailableChecked) AvailableCheckBox.IsChecked = true;
        }
        #endregion

        private void UsersSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update users datagrid
        }

        #region Search

        // Shows all books
        private void initBooksDataGrid()
        {
            List<String> columns = new List<String>();
            columns.Add("Title");

            List<List<String>> results = dbi.searchDatabaseRows(dbconnection, "books", "", columns);
            List<Book> books = new List<Book>();
            // Populate ItemsSource with book details
            foreach (List<String> row in results)
            {
                books.Add(new Book { BookID = row[0], Author = row[1], Title = row[2], Year = row[3], Language = row[4], Available = row[5] });
            }
            BookDataGrid.ItemsSource = books;
        }

        #endregion

        // Convenient abbreviations of useful methods
        #region Degub
        private void a(String m)
        {
            MessageBox.Show(m);
        }
        
        private void c(String m)
        {
            Console.WriteLine(m);
        }


        #endregion

    }
}
