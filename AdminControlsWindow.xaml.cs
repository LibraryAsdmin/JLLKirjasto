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

    public class Book
    {
        public String BookID { get; set; }
        public String Author { get; set; }
        public String Title { get; set; }
        public String Year { get; set; }
        public String Language { get; set; }
        public String Available { get; set; }
    }

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
            
            List<List<String>> results = searchDatabaseRows(dbconnection, "books", BooksSearch.Text, columns);
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

        // Function Parameter Explanations:
        // - dbconn = SQLiteConnection object to database
        // - table = The name of the table in the database
        // - term = The search term
        // - columns = Determines in what columns of the database the term is searched for. For example input {"Title", "ID"} would
        //   search for the term only in columns "Title" and "ID"
        //
        // Searches the database with connection 'dbconn' where 'columns' contains 'term' in table 'table'.
        // Returns a List of List<String>. List<String> are the rows of the table in which the term has been found
        private List<List<String>> searchDatabaseRows(SQLiteConnection dbconn, String table, String term, List<String> columns)
        {
            // List that contains the results
            List<List<String>> results = new List<List<String>>();

            // Determine the number of columns in the table
            Int32 tableColumnCount = 0;
            try
            {
                dbconn.Open();
                String sql = String.Format("pragma table_info({0});", table);
                SQLiteCommand countCommand = new SQLiteCommand(sql, dbconn);
                SQLiteDataReader counter = countCommand.ExecuteReader();

                while (counter.Read())
                {
                    tableColumnCount++;
                }

                counter.Close();
                dbconn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error! " + ex.Message);
            }


            if (columns.Count > 0)
            {
                try
                {
                    dbconn.Open();

                    // Define Parameter related strings
                    String searchTermParameter = "@SEARCHTERM";
                    String searchTerm = String.Format("%{0}%", term);

                    // create WHERE string
                    String searchColumns = String.Format("{0} LIKE {1} ", columns[0], searchTermParameter);

                    for (int i = 1; i < columns.Count; i++)
                    {
                        searchColumns += String.Format("OR {0} LIKE {1} ", columns[i], searchTermParameter);
                    }


                    // Parse query
                    String query = String.Format("SELECT * FROM {0} WHERE {1};", table, searchColumns);

                    // Reader
                    SQLiteCommand command = new SQLiteCommand(query, dbconn);
                    command.Parameters.AddWithValue(searchTermParameter, searchTerm);
                    SQLiteDataReader reader = command.ExecuteReader();

                    // Read data from the database
                    while (reader.Read())
                    {
                        List<String> currentRow = new List<String>();
                        for (int i = 0; i < tableColumnCount; i++)
                        {
                            // Determine the datatype of the database entry, read it accordingly and convert to string
                            String dataTypeName = reader.GetDataTypeName(i);
                            String current;
                            switch (dataTypeName)
                            {
                                case "int":
                                    current = reader.GetInt32(i).ToString();
                                    break;
                                case "text":
                                    current = reader.GetString(i);
                                    break;
                                case "boolean":
                                    current = reader.GetBoolean(i).ToString();
                                    break;
                                default:
                                    throw new Exception("Could not verify data type!");
                            }
                            // Append the current row by current string
                            currentRow.Add(current);
                        }
                        // Append search results by read database row
                        results.Add(currentRow);
                    }

                    reader.Close();
                    dbconn.Close();
                }
                // Report any errors
                catch (Exception ex)
                {
                    MessageBox.Show("Error!" + ex.Message);
                }
            }
            
            return results;
        }

        // Shows all books
        private void initBooksDataGrid()
        {
            List<String> columns = new List<String>();
            columns.Add("Title");

            List<List<String>> results = searchDatabaseRows(dbconnection, "books", "", columns);
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
