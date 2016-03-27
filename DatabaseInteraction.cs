using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                reportError(ex.Message);
            }

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
                catch (Exception ex)
                {
                    reportError(ex.Message);
                }
            }

            return results;
        }

        private void reportError(String message)
        {
            System.Windows.MessageBox.Show("Error! " + message);
        }
    }
}
