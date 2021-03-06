﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Windows;

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

        public enum columnID { BookID, Author, Title, Year, Language, Available, NumColumns};
        public static String[] columnNames = new String[(int)Book.columnID.NumColumns] { "BookID", "Author", "Title", "Year", "Language", "Available" };

        
        // returns the string of a book object identified by it's index
        public String getStringByIndex(int index)
        {
            switch(index)
            {
                case (int)columnID.BookID:
                    return BookID;
                case (int)columnID.Author:
                    return Author;
                case (int)columnID.Title:
                    return Title;
                case (int)columnID.Year:
                    return Year;
                case (int)columnID.Language:
                    return Language;
                case (int)columnID.Available:
                    return Available;
                default:
                    return null;
            }
        }
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

                            current = reader.GetString(i); // All datatypes are treated as strings as there is no intent of sorting by value functionality
                            /**
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
                            **/

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
                        string commandString = String.Format("UPDATE {0} SET {1}=@VALUE WHERE BookID=@ID", table, column);
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
                        string commandString = String.Format("DELETE FROM {0} WHERE BookID='{1}'", table, id);
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
            System.Windows.MessageBox.Show("Error! " + message+caller);
        }

        private void a(String c)
        {
            MessageBox.Show(c);
        }
    }
}
