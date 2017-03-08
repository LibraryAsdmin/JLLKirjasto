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
            BooksSearch.Focus(); // Set cursor to search box at window startup
        }

        #region UI Handling

        // Search options
       

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

        public void searchBooks()
        {
            // Determine what to search
            List<String> columns = new List<String>();
            if (IDCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("ID");
            if (TitleCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Title");
            if (AuthorCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Author");
            if (YearCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Year");
            if (LanguageCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Language");
            if (AvailableCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Available");
            if (CategoryCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Aineistolaji");
            if (ISBNCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("ISBN");

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
                    row[(int)Book.columnID.Amount],
                    row[(int)Book.columnID.Available],
                    row[(int)Book.columnID.ISBN],
                    row[(int)Book.columnID.Category]
                ));
            }
            BookListBox.ItemsSource = books;
        }

        public void searchUsers()
        {
            //Determine what to search
            List<String> columns = new List<String>();
            if (UserIDCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("ID");
            if (WilmaCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Wilma");
            if (LoansCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Loans");

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
            // dbi.addDatabaseRow(dbconnection, "books", "-");

            // display book edit prompt
            // Create a new window
            BookEditWindow bew = new BookEditWindow();
            bew.Owner = this;

            // show the window
            bew.ShowDialog();

        }
        private void delBookButton_Click(object sender, RoutedEventArgs e)
        {
            
            // verify that some item is selected
            if (!(BookListBox.SelectedItem == null))
            {
                MessageBoxResult confirmBookDeletion = System.Windows.MessageBox.Show("Are you sure you want to delete the selected book?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                if (confirmBookDeletion == MessageBoxResult.Yes)
                {
                    // Get book information from the database item
                    object selection = BookListBox.SelectedItem;
                    PropertyInfo prop = typeof(Book).GetProperty("id");
                    string ID = prop.GetValue(selection, null).ToString();

                    dbi.delDatabaseRow(dbconnection, "books", ID);
                    searchBooks();  // update BookListBox
                }
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

        private void delUserButton_Click(object sender, RoutedEventArgs e)
        {
            // verify that some item is selected
            if (!(UsersListBox.SelectedItem == null))
            {
                MessageBoxResult confirmUserDeletion = System.Windows.MessageBox.Show("Are you sure you want to delete the selected user?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                if (confirmUserDeletion == MessageBoxResult.Yes)
                {
                    // Get book information from the database item
                    object selection = UsersListBox.SelectedItem;
                    PropertyInfo prop = typeof(User).GetProperty("id");
                    string ID = prop.GetValue(selection, null).ToString();

                    dbi.delDatabaseRow(dbconnection, "users", ID);
                    searchUsers();
                }
            }   
        }

        private void UsersSearchButton_Click(object sender, RoutedEventArgs e)
        {
            searchUsers();
        }

        private void UsersSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                searchUsers();
            }
        }

        private void addUserButton_Click(object sender, RoutedEventArgs e)
        {
            SignUpOperation adminCreateUserOperation = new SignUpOperation();
            // generate new ID for the user
            adminCreateUserOperation.generateID();

            // verify that the generated ID is not in use
            // if it is, increment the number in ID until an unique ID is found
            Boolean isUnique;
            do
            {
                isUnique = adminCreateUserOperation.verifyID();
                if (!isUnique)
                {
                    adminCreateUserOperation.incrementID();
                }
            }
            while (!isUnique); // increment the ID until no conflicting IDs are found

            // add the user to user database table
            List<String> columns = new List<String>();
            columns.Add("Wilma-address");                   // Empty wilma for later editing
            columns.Add("");                                // Loans
            dbi.addDatabaseRow(dbconnection, "users", adminCreateUserOperation.getID(), columns);
            searchUsers();
        }

        private void editUser()
        {
            // verify that some item is selected
            if (!(UsersListBox.SelectedItem == null))
            {
                object selection = UsersListBox.SelectedItem;
                PropertyInfo prop = typeof(User).GetProperty("wilma");
                string Wilma = prop.GetValue(selection, null).ToString();
                prop = typeof(User).GetProperty("loans");
                string Loans = prop.GetValue(selection, null).ToString();
                prop = typeof(User).GetProperty("id");
                string ID = prop.GetValue(selection, null).ToString();

                // Create a new window
                UserEditWindow uew = new UserEditWindow();
                uew.Owner = this;

                // store old wilma address to allow for its reuse
                uew.setOldWilma(Wilma);

                // store user id for database identification
                uew.setOldID(ID);

                uew.WilmaBox.Text = Wilma;
                uew.LoansBox.Text = Loans;

                uew.Show();
            }
        }

        private void editUserButton_Click(object sender, RoutedEventArgs e)
        {
            editUser();
        }

        private void UsersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            editUser();
        }
    }
}
