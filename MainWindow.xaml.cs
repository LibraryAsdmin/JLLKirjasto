﻿using System;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Data.SQLite;
using System.Windows.Controls.Primitives;

namespace JLLKirjasto
{
    public class TranslationSource : INotifyPropertyChanged
    {
        private static readonly TranslationSource instance = new TranslationSource();

        public static TranslationSource Instance
        {
            get { return instance; }
        }

        private readonly ResourceManager resManager = Properties.Resources.ResourceManager;
        private CultureInfo currentCulture = null;

        public string this[string key]
        {
            get { return this.resManager.GetString(key, this.currentCulture); }
        }

        public CultureInfo CurrentCulture
        {
            get { return this.currentCulture; }
            set
            {
                if (this.currentCulture != value)
                {
                    this.currentCulture = value;
                    var @event = this.PropertyChanged;
                    if (@event != null)
                    {
                        @event.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class LocExtension : Binding
    {
        public LocExtension(string name) : base("[" + name + "]")
        {
            this.Mode = BindingMode.OneWay;
            this.Source = TranslationSource.Instance;
        }
    }

    public class bookListItem
    {
        public string BookName { get; set; }
        public string AuthorName { get; set; }
        public int bookID { get; set; }   
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        private TranslateTransform middleFlagTransform;
        private TranslateTransform bottomFlagTransform;
        private Storyboard gradientStoryboard;
        private Storyboard ShowSearchResultsGrid;

        //all the background colours used in gradients

        AdminControlsWindow adminwindow;

        bool atHome = true; //are we currently in home view?

        // Changes the behaviour of GoHome
        // 0 = StartPageGrid
        // 1 = Search
        // 2 = LoginGrid
        // 3 = SignUpGrid
        short currentView = 0;

        // Search variables
        List<String> searchResultTitles;
        List<String> searchResultAuthors;
        List<Int32> searchResultIDs;

        private SQLiteConnection dbconnection = new SQLiteConnection("Data Source=database.db");
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Resources["negativeScreenWidth"] = -(SystemParameters.FullPrimaryScreenWidth);
            Resources["screenWidth"] = SystemParameters.FullPrimaryScreenWidth;
            Resources["negativeScreenHeight"] = -(SystemParameters.FullPrimaryScreenHeight);
            Resources["screenHeight"] = SystemParameters.FullPrimaryScreenHeight;


            //changes the flag to correspond with the system culture
            string culture = CultureInfo.CurrentUICulture.ToString();
            changeUILanguage(culture);

            // Initializes flag transformations
            middleFlagTransform = new TranslateTransform(0, 0);
            bottomFlagTransform = new TranslateTransform(0, 0);
            gradientStoryboard = new Storyboard();

            //initializes and begins the gradient animation
            PointAnimation gradientTurn = new PointAnimation(new Point(0.2, 1), new Point(0.8, 1), TimeSpan.FromSeconds(10));
            gradientStoryboard.Children.Add(gradientTurn);
            gradientStoryboard.RepeatBehavior = RepeatBehavior.Forever;
            gradientStoryboard.AutoReverse = true;
            gradientStoryboard.DecelerationRatio = 0.1;
            gradientStoryboard.AccelerationRatio = 0.1;
            Storyboard.SetTarget(gradientTurn, WindowGrid);
            Storyboard.SetTargetProperty(gradientTurn, new PropertyPath("Background.EndPoint"));
            gradientStoryboard.Begin();

            // Show all books in the beginning
            search("");
            updateSearchResults();


            //initializes ShowSearchResultsGrid animation storyboard
            //WORK IN PROGRESS
            ShowSearchResultsGrid = new Storyboard();
            

        }

        #region UI Handling

        private void username_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                loginButton1.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private void password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                loginButton1.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private void LanguageGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            // Show languages when mouse enters the LanguageGrid
            showLanguages(middleFlagTransform.Y, bottomFlagTransform.Y);
        }

        private void LanguageGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            hideLanguages(middleFlagTransform.Y, bottomFlagTransform.Y);
        }

        private void searchButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (atHome) //only works if we're in the home view
            {
                atHome = false;
                currentView = 1;
                //fire up the animation by finding it from the xaml resources
                Storyboard BringUpSearchResults = this.FindResource("BringUpSearchResults") as Storyboard;
                BringUpSearchResults.Begin();

                //searchBox and searchButton are moved from the parenthood of StartPageContentGrid to that of windowGrid.
                //To make their coordinates stay constant, we fetch them relative to the window and then position them again.
                GeneralTransform transformButton = searchButton.TransformToAncestor(this);
                GeneralTransform transformBox = searchBox.TransformToAncestor(this);
                StartPageContentGrid.Children.Remove(searchButton);
                StartPageContentGrid.Children.Remove(searchBox);
                WindowGrid.Children.Add(searchButton);
                WindowGrid.Children.Add(searchBox);
                Point whereToTransformButton = transformButton.Transform(new Point(0, 0));
                TranslateTransform tt1 = new TranslateTransform(whereToTransformButton.X, whereToTransformButton.Y);
                searchButton.RenderTransform = tt1;
                searchButton.HorizontalAlignment = HorizontalAlignment.Left;
                searchButton.Margin = new Thickness(0);
                Point whereToTransformBox = transformBox.Transform(new Point(0, 0));
                TranslateTransform tt2 = new TranslateTransform(whereToTransformBox.X, whereToTransformBox.Y);
                searchBox.RenderTransform = tt2;
                searchBox.HorizontalAlignment = HorizontalAlignment.Left;
                searchBox.Margin = new Thickness(0);

            }
        }

        private void searchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchBox.Text == Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                searchBox.Text = "";
            }
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchBox.Text == Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                searchBox.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
            }
            else
            {
                // Console.Beep(); // just for fun. EDIT: not really fun since it makes the search laggy
                searchBox.Foreground = new SolidColorBrush(Colors.Black);

                // Search for books             
                search(searchBox.Text);
                updateSearchResults();

            }



        }

        // Home screen button visual appearance handling
        private void searchButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (0 == currentView)
            {
                Storyboard HighlightSearchButton = this.FindResource("HighlightSearchButton") as Storyboard;
                HighlightSearchButton.Begin();
            }
        }

        private void searchButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (0 == currentView)
            {
                Storyboard LeaveSearchButton = this.FindResource("LeaveSearchButton") as Storyboard;
                LeaveSearchButton.Begin();
            }
        }

        private void signupButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Storyboard HighlightSignUpButton = this.FindResource("HighlightSignUpButton") as Storyboard;
            HighlightSignUpButton.Begin();
        }

        private void signupButton_MouseLeave(object sender, MouseEventArgs e)
        {
            Storyboard LeaveSignUpButton = this.FindResource("LeaveSignUpButton") as Storyboard;
            LeaveSignUpButton.Begin();
        }

        private void loginButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Storyboard HighlightLoginButton = this.FindResource("HighlightLoginButton") as Storyboard;
            HighlightLoginButton.Begin();
        }

        private void loginButton_MouseLeave(object sender, MouseEventArgs e)
        {
            Storyboard LeaveLoginButton = this.FindResource("LeaveLoginButton") as Storyboard;
            LeaveLoginButton.Begin();
        }

        // Login username input field behaviour
        private void username_GotFocus(object sender, RoutedEventArgs e)
        {
            if (username.Text == Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                username.Text = "";
            }
        }

        private void username_LostFocus(object sender, RoutedEventArgs e)
        {
            if (username.Text == "")
            {
                username.Text = Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture);
            }
        }

        private void username_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (username.Text == Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                username.Foreground = new SolidColorBrush(Colors.DarkSlateGray);

            }
            else
            {
                username.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void signupField_LostFocus(object sender, RoutedEventArgs e)
        {
            if (signupField.Text == "")
            {
                signupField.Text = Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture);
            }
        }

        private void signupField_GotFocus(object sender, RoutedEventArgs e)
        {
            if (signupField.Text == Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                signupField.Text = "";
            }
        }

        private void signupField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (signupField.Text == Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                signupField.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
            }
            else
            {
                signupField.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        // User login handling here
        private void loginButton1_Click(object sender, RoutedEventArgs e)
        {
            if (username.Text == "admin" && password.Password == "kuulkala")
            {
                adminwindow = new AdminControlsWindow();
                adminwindow.Show();
            }
        }

        private void RootWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                adminwindow.Close();
            }
            catch
            {
                // nothing wrong here.
            }
        }

        private void signupButton1_Click(object sender, RoutedEventArgs e)
        {
            if (signupField.Text.Contains("@edu.jns.fi"))
            {
                try
                {
                    dbconnection.Open();

                    // Calculate UserID
                    SQLiteCommand countUserIDs = new SQLiteCommand("SELECT COUNT(UserID) FROM users;", dbconnection);
                    SQLiteDataReader readCount = countUserIDs.ExecuteReader();

                    int userCount = 0;
                    while (readCount.Read())
                    {
                        userCount = readCount.GetInt32(0);
                    }

                    // SQL command.
                    String sql = "INSERT INTO users VALUES (@UserID, @Username);";

                    // execute command and close conection
                    SQLiteCommand command = new SQLiteCommand(sql, dbconnection);
                    command.Parameters.AddWithValue("UserID", userCount);
                    command.Parameters.AddWithValue("Username", signupField.Text);
                    command.ExecuteNonQuery();
                    dbconnection.Close();
                }
                catch
                {
                    dbconnection.Close();
                    System.Windows.MessageBox.Show(Properties.Resources.ResourceManager.GetString("SignUpErrorMessage", TranslationSource.Instance.CurrentCulture));
                }
            }
            else
            {
                System.Windows.MessageBox.Show(Properties.Resources.ResourceManager.GetString("SignUpErrorMessage", TranslationSource.Instance.CurrentCulture));
            }

        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox.SelectedItem != null)
            {
                bookListItem book = (bookListItem)listBox.SelectedItem;
                int id = book.bookID;
                //TODO: do something with the id
            }
        }

        private void searchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (searchBox.Text == "")
            {
                searchBox.Text = Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture);
            }
        }

        //Go home-button
        private void button_Click(object sender, RoutedEventArgs e)
        {
            //In home view
            if (0 == currentView)
            {
                System.Windows.MessageBox.Show("Home button should not be accessible in home screen... Go fix the program!");
            }
            //In search view
            else if (1 == currentView)
            {
                Storyboard GoBackHome = this.FindResource("GoBackHome") as Storyboard;
                GoBackHome.Begin();
            }
            //In login view
            else if (2 == currentView)
            {
                Storyboard HideLoginGrid = this.FindResource("HideLoginGrid") as Storyboard;
                HideLoginGrid.Begin();
            }
            //In sign up view
            else if (3 == currentView)
            {
                Storyboard HideSignUpGrid = this.FindResource("HideSignUpGrid") as Storyboard;
                HideSignUpGrid.Begin();
            }
            currentView = 0; //we're home now
        }

        private void signupButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            currentView = 3;
            Storyboard ShowSignUpGrid = this.FindResource("ShowSignUpGrid") as Storyboard;
            ShowSignUpGrid.Begin();
        }

        private void loginButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            currentView = 2;
            Storyboard ShowLoginGrid = this.FindResource("ShowLoginGrid") as Storyboard;
            ShowLoginGrid.Begin();

        }

        private void GoHomeStoryboardCompleted(object sender, EventArgs e)
        {
            //searchBox and searchButton are returned to be children of StartPageContentGrid
            WindowGrid.Children.Remove(searchButton);
            WindowGrid.Children.Remove(searchBox);
            StartPageContentGrid.Children.Add(searchButton);
            StartPageContentGrid.Children.Add(searchBox);
            searchButton.RenderTransform = new TranslateTransform(0, 0);
            searchButton.VerticalAlignment = VerticalAlignment.Top;
            searchButton.HorizontalAlignment = HorizontalAlignment.Center;
            searchButton.Margin = new Thickness(0, 50, 0, 0);
            searchBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            searchBox.Margin = new Thickness(44, 59, 44, 0);
            searchBox.RenderTransform = new TranslateTransform(0, 0);

            atHome = true; //we're home, so the searchButton can now trigger another animation if the user so desires
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }


        #endregion

        #region Language
        // Change language to English
        private void English_Click(object sender, RoutedEventArgs e)
        {
            changeUILanguage("en-GB");
        }

        // Change language to Swedish
        private void Swedish_Click(object sender, RoutedEventArgs e)
        {
            changeUILanguage("sv-SE");
        }

        // Change language to Finnish (default)
        private void Finnish_Click(object sender, RoutedEventArgs e)
        {
            changeUILanguage("fi-FI");
        }
        void changeUILanguage(string language)
        {
            bool updateSearchBoxText = false; //do we have to update searchBox's text 
                                              //(has to be done manyally because we assign it string values elsewhere, which replaces the automatic switching)
            bool updateLogInUserNameBoxText = false;
            bool updateSignupField = false;

            if (username.Text == Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            { updateLogInUserNameBoxText = true; }

            if (searchBox.Text == Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture))
            { updateSearchBoxText = true; }

            if (signupField.Text == Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            { updateSignupField = true; }

            switch (language)
            {
                case "en-GB":
                case "en-US":
                    // Move English flag to front
                    Canvas.SetZIndex(Swedish, 0);
                    Canvas.SetZIndex(Finnish, 1);
                    Canvas.SetZIndex(English, 2);
                    // Change language 
                    TranslationSource.Instance.CurrentCulture = new System.Globalization.CultureInfo("en-GB");
                    break;

                case "fi-FI":
                    // Move Finnish to front
                    Canvas.SetZIndex(Swedish, 1);
                    Canvas.SetZIndex(Finnish, 2);
                    Canvas.SetZIndex(English, 0);
                    // Change language
                    TranslationSource.Instance.CurrentCulture = new System.Globalization.CultureInfo("fi-FI");
                    break;

                case "sv-SE":
                    // Move Swedish to front
                    Canvas.SetZIndex(Swedish, 2);
                    Canvas.SetZIndex(Finnish, 1);
                    Canvas.SetZIndex(English, 0);
                    // Change language
                    TranslationSource.Instance.CurrentCulture = new System.Globalization.CultureInfo("sv-SE");
                    break;
            }

            if (updateSearchBoxText)
            {
                searchBox.Text = Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture);
            }

            if (updateLogInUserNameBoxText)
            {
                username.Text = Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture);
            }

            if (updateSignupField)
            {
                signupField.Text = Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture);
            }
        }

        // Extends the list of languages
        private void showLanguages(double middleY, double bottomÝ)
        {
            Duration duration = new Duration(TimeSpan.FromMilliseconds(250));

            // transform the middle flag from current position to middle
            middleFlagTransform = new TranslateTransform(0, middleY);
            DoubleAnimation anim = new DoubleAnimation(40, duration);
            middleFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim);
            Swedish.RenderTransform = middleFlagTransform;

            // transform the bottom flag from current position to bottom
            bottomFlagTransform = new TranslateTransform(0, bottomÝ);
            DoubleAnimation anim2 = new DoubleAnimation(80, duration);
            bottomFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim2);
            English.RenderTransform = bottomFlagTransform;
        }

        // Hides the list of languages
        private void hideLanguages(double middleY, double bottomY)
        {
            Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 250));

            // transform  the middle flag from its current positions to the default position
            middleFlagTransform = new TranslateTransform(0, middleY);
            DoubleAnimation anim = new DoubleAnimation(0, duration);
            middleFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim);
            Swedish.RenderTransform = middleFlagTransform;

            // transform the bottom flag from its current positions to the default position
            bottomFlagTransform = new TranslateTransform(0, bottomY);
            DoubleAnimation anim2 = new DoubleAnimation(0, duration);
            bottomFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim2);
            English.RenderTransform = bottomFlagTransform;
        }




        #endregion

        #region Search
        // Search Titles in the book database. Hard coded to use searchBox as search bar
        private void search(String term)
        {

            searchResultTitles = new List<String>();
            searchResultAuthors = new List<String>();
            searchResultIDs = new List<Int32>();
            
            try
            {
                dbconnection.Open();

                String searchTerm = '%' + term + '%';

                // Search the database for a specific title
                String sql = "SELECT BookID, Title, Author FROM books WHERE Title LIKE @SearchTerm AND Available=1 OR Author LIKE @SearchTerm AND Available=1 OR BookID LIKE @SearchTerm AND Available=1;";
                SQLiteCommand command = new SQLiteCommand(sql, dbconnection);
                command.Parameters.AddWithValue("SearchTerm", searchTerm);

                SQLiteDataReader reader = command.ExecuteReader();

                // Read search results
                while (reader.Read())
                {
                    searchResultIDs.Add(reader.GetInt32(0));
                    searchResultTitles.Add(reader.GetString(1));
                    searchResultAuthors.Add(reader.GetString(2));
                }
                reader.Close();
                dbconnection.Close();

                // Write matching books to console (for debugging)
                foreach (String result in searchResultTitles)
                {
                    Console.WriteLine(result);
                }
                Console.WriteLine();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Something happened! " + ex.Message);
            }              
        }

        // hard coded for searchBox
        private void updateSearchResults()
        {
            //Example: items.Add(new bookListItem() { BookName = "The Name of The Book 1", AuthorName = "The Name of The Author 1", bookID = 314159 });
            List<bookListItem> items = new List<bookListItem>();

            for (int i = 0; i < searchResultTitles.Count; i++)
            {
                items.Add(new bookListItem() { BookName = searchResultTitles[i], AuthorName = searchResultAuthors[i], bookID = searchResultIDs[i] });
            }
            listBox.ItemsSource = items;
        }

        #endregion
    }
}
