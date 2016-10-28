using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Windows;
using System.Net.Mail;
using System.Net;

namespace JLLKirjasto
{
    public class Book
    {
        #region interface
        // constructor
        public Book(String _id, String _author, String _title, String _year, String _language, String _available, String _isbn, String _category)
        {
            id        = _id;
            author    = _author;
            title     = _title;
            year      = _year;
            language  = _language;
            available = _available;
            isbn      = _isbn;
            category  = _category;
        }

        // returns the string of a book object identified by its index (column number in the database)
        public String getStringByIndex(int index)
        {
            switch (index)
            {
                case (int)columnID.ID:
                    return id;
                case (int)columnID.Author:
                    return author;
                case (int)columnID.Title:
                    return title;
                case (int)columnID.Year:
                    return year;
                case (int)columnID.Language:
                    return language;
                case (int)columnID.Available:
                    return available;
                case (int)columnID.ISBN:
                    return isbn;
                case (int)columnID.Category:
                    return category;
                default:
                    return null;
            }
        }
        #endregion interface

        #region implementation
        public String id { get; private set; }
        public String author { get; private set; }
        public String title { get; private set; }
        public String year { get; private set; }
        public String language { get; private set; }
        public String available { get; private set; }
        public String isbn { get; private set; }
        public String category { get; private set; }

        public enum columnID { ID, Author, Title, Year, Language, Available, ISBN, Category, NumColumns};
        public static String[] columnNames = new String[(int)Book.columnID.NumColumns] { "ID", "Author", "Title", "Year", "Language", "Available", "ISBN", "Aineistolaji"};
        #endregion implementation
    }

    public class User
    {
        #region interface
        // constructor
        public User(String _id, String _wilma, String _loans)
        {
            id = _id;
            wilma = _wilma;
            loans = _loans;
        }

        public List<String> getLoans()
        {
            List<String> loanList = new List<String>();
            String[] list = loans.Split(';');

            foreach (string s in list)
            {
                loanList.Add(s);
            }
            
            return loanList;
        }
        #endregion interface

        #region implementation
        private enum columnID { ID, Wilma, Loans, NumColumns};
        public String id { get; private set; }
        public String wilma { get; private set; } // for signup and signin
        public String loans { get; private set; } // A comma separated list of book IDs 
        #endregion implementation
    }

    // information that's needed to be stored during the signup operation is stored in an instance of this class
    public class SignUpOperation
    {
        #region interface
        // constructor
        public SignUpOperation()
        {
            reset();
        }

        // generates a 4 digit random number and stores it as a string in memory
        public void generateCode()
        {
            Random r = new Random();
            string code = ((int)(r.NextDouble() * 9999.0)).ToString();
            Code = code.PadLeft(4, '0');
        }

        // compares a given code to the one stored in memory
        public bool compareCode(string code)
        {
            if (code == Code) return true;
            return false;
        }

        // send code to email using google SMTP servers
        public void sendCode()
        {
            if (Code == null)
                throw new Exception("Error! This function shouldn't be used before the code is generated!");
            if (Email == null)
                throw new Exception("Error! This function shouldn't be used before the email is given!");

            // build email
            MailMessage message = new MailMessage(
                "JLLKirjastoMail@gmail.com",
                Email,
                Properties.Resources.ResourceManager.GetString("SignUpEmailTitle", TranslationSource.Instance.CurrentCulture),
                Code
                );

            // send message using SMTP
            SmtpClient client = new SmtpClient("smtp.gmail.com", 25);
            client.UseDefaultCredentials = true;
            NetworkCredential credentials = new NetworkCredential("JLLKirjastoMail@gmail.com", "b7zr%LbLC!");
            client.Credentials = credentials;
            client.EnableSsl = true;

            try
            {
                client.SendAsync(message, null);
            }
            catch (SmtpException ex)
            {
                MessageBox.Show("SMTP Exception has occured: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Occured: " + ex.Message);
            }
        }

        // set the email address to which the code is sent
        public void addEmail(String email)
        {
            Email = email;
        }

        void resetEmail()
        {
            Email = null;
        }

        // Reset signup operation to its original state
        public void reset()
        {
            Code = null;
            Email = null;
        }
        #endregion interface

        #region implementation
        // Variables
        private String Code { get; set; } // the code sent to user's email
        private String Email { get; set; } // the wilma email address of the user
        #endregion implementation
    }

    public class DatabaseInteraction
    {

        // TODO: clean up MainWindow.xaml.cs and AdminControlsWindow.xaml.cs by moving core functionality here

        // Function Parameter Explanations:
        // - dbconn = SQLiteConnection object to database
        // - table = The name of the table in the database
        // - term = The search term
        // - columns = Determines in what columns of the database the term is searched for. For example input {"Title", "ID"} would
        //   search for the term only in columns "Title" and "ID"
        //
        // Searches the database with connection 'dbconn' where 'columns' contains 'term' in table 'table'.
        // Returns a List of List<String>. List<String> are the rows of the table in which the term has been found
        public List<List<String>> searchDatabaseRows(SQLiteConnection dbconn, String table, String term, List<String> columns)
        {
            // List that contains the results
            List<List<String>> results = new List<List<String>>();

            // Determine the number of columns in the table
            Int32 tableColumnCount = countColumns(dbconn, table);

            // Make sure that you are actually searching for something
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

                            // Read the value if it is not NULL. If it is, give it the value "" (empty string)
                            if (!reader.IsDBNull(i))
                            {
                                current = reader.GetString(i);
                            }
                            else { current = ""; }


                            // Append the current row by current string
                            currentRow.Add(current);
                        }
                        // Append search results by read database row
                        results.Add(currentRow);
                    }
                    reader.Close();
                    dbconn.Close();
                }
                catch (Exception ex)
                {
                    reportError(ex.Message, "searchDatabaseRows");
                }
            }
            return results;
        }

        // TODO: verification that two book id's can't be made equal
        // Modifies a cell in an database
        // Function Parameter Explanations:
        // - dbcomm = SQLiteConnection object to database
        // - table = The name of the table in the database
        // - values = The values that are passed into the database
        // - id = The row to which the edits are made is determined by the book id
        // - column = To determine where the edit should be done.
        public void commitDbChanges(SQLiteConnection dbconn, String table, String value, String id, String column)
        {
            try
            {
                dbconn.Open();
                using (SQLiteTransaction transaction = dbconn.BeginTransaction())
                {
                    using (SQLiteCommand command = new SQLiteCommand(dbconn))
                    {
                        string commandString = String.Format("UPDATE {0} SET {1}=@VALUE WHERE ID=@ID", table, column);
                        command.CommandText = commandString;
                        command.Parameters.AddWithValue("@VALUE", value);
                        command.Parameters.AddWithValue("@ID", id);

                        command.ExecuteNonQuery();

                    }
                    transaction.Commit();
                }
                dbconn.Close();
            }
            catch(Exception ex)
            {
                reportError(ex.Message, "commitDbChanges");
            }        
        }

        public void addDatabaseRow(SQLiteConnection dbconn, String table, String id)
        {
            // Determine the number of columns in the database  
            int columnCount = countColumns(dbconn, table);
            try
            {
                dbconn.Open();
                using (SQLiteTransaction transaction = dbconn.BeginTransaction())
                {
                    using (SQLiteCommand command = new SQLiteCommand(dbconn))
                    {
                        // Parse string columnsString for column adding command.
                        String columnsString = "'"+id+"'";
                        for (int i = 1; i < columnCount; i++)
                        {
                            columnsString += (","+"'-'");
                        }

                        string commandString = String.Format("INSERT INTO {0} VALUES ({1})", table, columnsString);
                        command.CommandText = commandString;

                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                dbconn.Close();
            }
            catch (Exception ex)
            {
                reportError(ex.Message, "addDatabaseRow");
            }
        }

        public void delDatabaseRow(SQLiteConnection dbconn, String table, String id)
        {
            try
            {
                dbconn.Open();
                using (SQLiteTransaction transaction = dbconn.BeginTransaction())
                {
                    using (SQLiteCommand command = new SQLiteCommand(dbconn))
                    {                  
                        string commandString = String.Format("DELETE FROM {0} WHERE ID='{1}'", table, id);
                        command.CommandText = commandString;

                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                dbconn.Close();
            }
            catch (Exception ex)
            {
                reportError(ex.Message, "delDatabaseRow");
            }
        }

        private int countColumns(SQLiteConnection dbconn, String table)
        {
            int count = 0;
            try
            {
                dbconn.Open();
                String sql = String.Format("pragma table_info({0});", table);
                SQLiteCommand countCommand = new SQLiteCommand(sql, dbconn);
                SQLiteDataReader counter = countCommand.ExecuteReader();

                while (counter.Read())
                {
                    count++;
                }

                counter.Close();
                dbconn.Close();
            }
            catch (Exception ex)
            {
                reportError(ex.Message, "countColumns");
            }
            return count;
        }

        private void reportError(String message, String caller)
        {
            System.Windows.MessageBox.Show("Error! " + message + " at "+caller);
        }

        private void a(String c)
        {
            MessageBox.Show(c);
        }
    }
}
