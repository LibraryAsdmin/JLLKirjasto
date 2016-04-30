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
using System.Data;

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
        String bookstable = "books";
        #endregion

        public AdminControlsWindow()
        {
            InitializeComponent();
            updateBooksDataGrid();
            initCheckBoxes();
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
        private void updateBooksDataGrid()
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
                books.Add(new Book
                {
                    BookID = row[(int)Book.columnID.BookID], Author = row[(int)Book.columnID.Author],
                    Title = row[(int)Book.columnID.Title], Year = row[(int)Book.columnID.Year],
                    Language = row[(int)Book.columnID.Language], Available = row[(int)Book.columnID.Available]
                });
            }
            BookDataGrid.ItemsSource = books;
        }

        #endregion

        #region Database Manipulation
        private void BookDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                Book selectedBook = (Book)BookDataGrid.SelectedItem;
                int columnIndex = BookDataGrid.CurrentCell.Column.DisplayIndex;
                dbi.commitDbChanges(dbconnection, bookstable, ((TextBox)e.EditingElement).Text, selectedBook.getStringByIndex((int)Book.columnID.BookID), Book.columnNames[columnIndex]);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void addBookButton_Click(object sender, RoutedEventArgs e)
        {

            dbi.addDatabaseRow(dbconnection, "books", "-");
            updateBooksDataGrid();
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

        private void delBookButton_Click(object sender, RoutedEventArgs e)
        {
            Book selectedBook = (Book)BookDataGrid.SelectedItem;
            dbi.delDatabaseRow(dbconnection, "books", selectedBook.getStringByIndex((int)Book.columnID.BookID));
            updateBooksDataGrid();
        }
    }
}
