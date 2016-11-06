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
using System.Reflection;

namespace JLLKirjasto
{
    /// <summary>
    /// Interaction logic for AdminControlsWindow.xaml
    /// </summary>
    public partial class AdminControlsWindow : Window
    {
        #region Variables

        // Variables for advanced search in admin control window
        bool IDChecked = true;
        bool TitleChecked = true;
        bool AuthorChecked = true;
        bool YearChecked = false;
        bool AvailableChecked = false;
        bool LanguageChecked = false;

        // Variables for interaction with the database
        SQLiteConnection dbconnection = new SQLiteConnection("Data Source=database.db");
        DatabaseInteraction dbi = new DatabaseInteraction();
        #endregion

        public AdminControlsWindow()
        {
            InitializeComponent();
            initCheckBoxes();
            BooksSearch.Focus(); // Set cursor to search box at window startup
        }

        #region UI Handling

        // Search options
        private void IDCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IDChecked = true;
        }
        private void IDCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IDChecked = false;
        }

        private void AuthorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AuthorChecked = true;
        }
        private void AuthorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AuthorChecked = false;
        }

        private void TitleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TitleChecked = true;
        }
        private void TitleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            TitleChecked = false;
        }

        private void YearCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            YearChecked = true;
        }
        private void YearCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            YearChecked = false;
        }

        private void LanguageCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LanguageChecked = true;
        }
        private void LanguageCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LanguageChecked = false;
        }

        private void AvailableCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AvailableChecked = true;
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

        private void BooksSearchButton_Click(object sender, RoutedEventArgs e)
        {
            searchBooks();
        }
        private void BooksSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                searchBooks();
            }
        } // search books by hitting enter when in search box
        #endregion

        #region Search

        private void BooksSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BooksSearch.Text.Length >= MainWindow.minSearchChars)
            {
                searchBooks();
            }         
        }
        //private void updateUsersDataGrid(){}
        private void UsersSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UsersSearch.Text.Length >= MainWindow.minSearchChars)
            {
                searchUsers();
            }
        }

        private void searchBooks()
        {
            // Determine what to search
            List<String> columns = new List<String>();
            if (IDChecked) columns.Add("ID");
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
                (
                    row[(int)Book.columnID.ID],
                    row[(int)Book.columnID.Author],
                    row[(int)Book.columnID.Title],
                    row[(int)Book.columnID.Year],
                    row[(int)Book.columnID.Language],
                    row[(int)Book.columnID.Available],
                    row[(int)Book.columnID.ISBN],
                    row[(int)Book.columnID.Category]
                ));
            }
            BookListBox.ItemsSource = books;
        }

        private void searchUsers()
        {
            //Determine what to search
            List<String> columns = new List<String>();
            // TODO: Add advanced search functionality. Currently hard-coded to search all columns
            columns.Add("ID");
            columns.Add("Wilma");
            columns.Add("Loans");

            List<List<String>> results = dbi.searchDatabaseRows(dbconnection, "users", UsersSearch.Text, columns);
            List<User> users = new List<User>();
            // Populate ItemsSource with book details
            foreach (List<String> row in results)
            {
                users.Add(new User
                (
                    row[(int)User.columnID.ID],
                    row[(int)User.columnID.Wilma],
                    row[(int)User.columnID.Loans]
                ));
            }
            UsersListBox.ItemsSource = users;
        }

        #endregion

        #region Database Manipulation
        /*
        private void BookDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                Book selectedBook = (Book)BookDataGrid.SelectedItem;
                int columnIndex = BookDataGrid.CurrentCell.Column.DisplayIndex;
                dbi.commitDbChanges(dbconnection, bookstable, ((TextBox)e.EditingElement).Text, selectedBook.getStringByIndex((int)Book.columnID.ID), Book.columnNames[columnIndex]);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        */

        // Functionality for adding and removing whole books
        private void addBookButton_Click(object sender, RoutedEventArgs e)
        {
            dbi.addDatabaseRow(dbconnection, "books", "-");
        }
        private void delBookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Book selectedBook = (Book)BookDataGrid.SelectedItem;
                //dbi.delDatabaseRow(dbconnection, "books", selectedBook.getStringByIndex((int)Book.columnID.ID));
            }
            catch
            {
                Console.Beep();
            }
        }
        private void editBookButton_Click(object sender, RoutedEventArgs e)
        {
            editBook();
        }
        private void BookListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            editBook();
        }

        private void editBook()
        {
            // verify that some item is selected
            if (!(BookListBox.SelectedItem == null))
            {
                // Get book information from the database item
                object selection = BookListBox.SelectedItem;
                PropertyInfo prop = typeof(Book).GetProperty("id");
                string ID = prop.GetValue(selection, null).ToString();
                prop = typeof(Book).GetProperty("author");
                string Author = prop.GetValue(selection, null).ToString();
                prop = typeof(Book).GetProperty("title");
                string Title = prop.GetValue(selection, null).ToString();
                prop = typeof(Book).GetProperty("year");
                string Year = prop.GetValue(selection, null).ToString();
                prop = typeof(Book).GetProperty("available");
                string Available = prop.GetValue(selection, null).ToString();
                prop = typeof(Book).GetProperty("language");
                string Language = prop.GetValue(selection, null).ToString();
                prop = typeof(Book).GetProperty("isbn");
                string ISBN = prop.GetValue(selection, null).ToString();
                prop = typeof(Book).GetProperty("category");
                string Category = prop.GetValue(selection, null).ToString();



                // Create a new window
                BookEditWindow bew = new BookEditWindow();
                bew.Owner = this;

                // store the current book id in bew.oldID
                bew.setOldID(ID);

                // Assign book information to new edit window
                bew.IDBox.Text = ID;
                bew.AuthorBox.Text = Author;
                bew.TitleBox.Text = Title;
                bew.YearBox.Text = Year;
                bew.LanguageBox.Text = Language;
                bew.AvailableBox.Text = Available;
                bew.ISBNBox.Text = ISBN;
                bew.CategoryBox.Text = Category;

                // show the window
                bew.ShowDialog();
            }
        }
        #endregion


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
