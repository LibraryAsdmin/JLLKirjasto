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
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Net.Http;

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

    class GridHeightClass : INotifyPropertyChanged
    {
        //The next bit handles the INotifyPropertyChanged implementation - in essence, it allows for bindings to notice when the property changes and automatically update

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //The Instance declaration handles the Singleton approach to ExpandedGridHeightClass - there's only one instance of the class,
        //and it can be called from anywhere using ExpandedGridHeightClass.Instance.[...]

        private static GridHeightClass _instance;
        public static GridHeightClass Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GridHeightClass();
                return _instance;
            }
        }

        //The Height of the maximized grid (which is either SearchGrid, LoginGrid or SignupGrid)
        private Double _expandedGridHeight;
        public Double ExpandedGridHeight
        {
            get { return _expandedGridHeight; }
            set
            {
                if (value != _expandedGridHeight)
                {
                    _expandedGridHeight = value;
                    Notify("ExpandedGridHeight");
                }
            }
        }

        //The Height of a collapsed grid in the home view (i.e. a third of the availalble space)
        private Double _collapsedGridHeight;
        public Double CollapsedGridHeight
        {
            get { return _collapsedGridHeight; }
            set
            {
                if (value != _collapsedGridHeight)
                {
                    _collapsedGridHeight = value;
                    Notify("CollapsedGridHeight");
                }
            }
        }

    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public partial class MainWindow : Window
    {
        #region Variables
        private TranslateTransform middleFlagTransform;
        private TranslateTransform bottomFlagTransform;

        TimeSpan transformationTimeSpan = TimeSpan.FromSeconds(0.5); //this adjusts the speed of the animations in the home view

        AdminControlsWindow adminwindow;

        //Which view are we in?
        // 0 = StartPageGrid
        // 1 = SearchGrid
        // 2 = LoginGrid
        // 3 = SignUpGrid
        byte currentView = 0;

        // Variables for database interaction
        private SQLiteConnection dbconnection = new SQLiteConnection("Data Source=database.db");
        private DatabaseInteraction dbi = new DatabaseInteraction();
        List<Book> ListBoxItems;
        // Variables required by search
        // Require at least this number of characters before searching to avoid search congestion
        // Global bariable, this is used also to control search in AdminControls
        public const uint minSearchChars = 3;

        // Variables for SignUp operation
        SignUpOperation defaultSignUpOperation = new SignUpOperation();
        #endregion


        public MainWindow()
        {
            InitializeComponent();

            //changes the flag to correspond with the system culture
            string culture = CultureInfo.CurrentUICulture.ToString();
            changeUILanguage(culture);

            // Initializes flag transformations
            middleFlagTransform = new TranslateTransform(0, 0);
            bottomFlagTransform = new TranslateTransform(0, 0);
        }

        #region UI Handling

        // Calculates NavigationStackPanel grid sizes
        private void RootWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Double ExpandedGridHeight = WindowGrid.ActualHeight - HeaderGrid.ActualHeight;
            Double CollapsedGridHeight = ExpandedGridHeight / 3;
            GridHeightClass.Instance.ExpandedGridHeight = ExpandedGridHeight;
            GridHeightClass.Instance.CollapsedGridHeight = CollapsedGridHeight;

            switch (currentView)
            {
                case 0:
                    // Whenever RootWindow's size is changed, calculate proper height for the grids in NavigationStackPanel.
                    // This event fires also when the window is initialised, so the grids are always the correct size
                    // Update the height of the grids
                    LoginGrid.Height = CollapsedGridHeight;
                    SignUpGrid.Height = CollapsedGridHeight;
                    SearchGrid.Height = CollapsedGridHeight;
                    break;
                case 1:
                    SearchGrid.Height = ExpandedGridHeight;
                    break;
                case 2:
                    LoginGrid.Height = ExpandedGridHeight;
                    break;
                case 3:
                    SignUpGrid.Height = ExpandedGridHeight;
                    break;
            }
        }

        // UI navigation
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // TODO: Create a nice and smooth animation for the visibility of tooltips
        private void UsernameField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }
        private void PasswordField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //the 'return to home' button has been clicked, so now we have to determine where we are to get back home

            switch (currentView)
            {
                case 0:
                    //In home view
                    System.Windows.MessageBox.Show("Home button should not be accessible in home screen... Go fix the program!");
                    break;
                case 1:
                    //In search view
                    Storyboard HideSearchGrid = this.FindResource("HideSearchGrid") as Storyboard;
                    HideSearchGrid.Begin();
                    break;
                case 2:
                    //In login view
                    Storyboard HideLoginGrid = this.FindResource("HideLoginGrid") as Storyboard;
                    HideLoginGrid.Begin();
                    break;
                case 3:
                    //In signup view
                    Storyboard HideSignUpGrid = this.FindResource("HideSignUpGrid") as Storyboard;
                    HideSignUpGrid.Begin();
                    break;
            }

            currentView = 0; //we're home now
        }

        #region Storyboard Complete Event Handlers
        void ResetAnimationsAfterArrivingToHomeView()
        {
            //Read the name of the function to know what it does
            LoginGrid.BeginAnimation(Grid.HeightProperty, null);
            LoginGrid.Height = GridHeightClass.Instance.CollapsedGridHeight;
            SignUpGrid.BeginAnimation(Grid.HeightProperty, null);
            SignUpGrid.Height = GridHeightClass.Instance.CollapsedGridHeight;
            SearchGrid.BeginAnimation(Grid.HeightProperty, null);
            SearchGrid.Height = GridHeightClass.Instance.CollapsedGridHeight;
        }

        private void ShowSearchGridStoryboard_Completed(object sender, EventArgs e)
        {
            currentView = 1;
            SearchGrid.BeginAnimation(Grid.HeightProperty, null); //removes the ShowSearchGrid animation to allow for code-behind height change to take place
            SearchGrid.Height = GridHeightClass.Instance.ExpandedGridHeight; //apply the correct height value to SearchGrid (wihtout this SearchGrid would revert back to original height value
        }

        private void ShowLoginGridStoryboard_Completed(object sender, EventArgs e)
        {
            currentView = 2;
            LoginGrid.BeginAnimation(Grid.HeightProperty, null); //removes the ShowLoginGrid animation to allow for code-behind height change to take place
            LoginGrid.Height = GridHeightClass.Instance.ExpandedGridHeight;
        }

        private void ShowSignUpGridStoryboard_Completed(object sender, EventArgs e)
        {
            currentView = 3;  //removes the ShowSignUpGrid animation to allow for code-behind height change to take place
            SignUpGrid.BeginAnimation(Grid.HeightProperty, null);
            SignUpGrid.Height = GridHeightClass.Instance.ExpandedGridHeight;
        }

        private void HideSearchGridStoryboard_Completed(object sender, EventArgs e)
        {
            currentView = 0; //we're home
            ResetAnimationsAfterArrivingToHomeView();
        }

        private void HideLoginGridStoryboard_Completed(object sender, EventArgs e)
        {
            currentView = 0; //we're home
            ResetAnimationsAfterArrivingToHomeView();
        }

        private void HideSignUpGridStoryboard_Completed(object sender, EventArgs e)
        {
            currentView = 0; //we're home
            ResetAnimationsAfterArrivingToHomeView();
        }
        #endregion


        // navigation between home screen and search, login and signup
        // The user can navigate using the mouse. This is handled by the functions *Grid_MouseUp
        // Additionally the user can use the keyboard to navigate by tabbing into a tooltip and pressing enter. This is handled by *Tooltip_KeyDown
        // KeyDown is used as if there is a popup notification box, the box could be shut down by pressing the key down and KeyUp would thus respawn the notification.

        //starts ShowSearchGrid animation
        void showSearchGrid()
        {
            Storyboard ShowSearchGrid = this.FindResource("ShowSearchGrid") as Storyboard;
            ShowSearchGrid.Begin();
        }

        //starts ShowLoginGrid animation
        void showLoginGrid()
        {
            Storyboard ShowLoginGrid = this.FindResource("ShowLoginGrid") as Storyboard;
            ShowLoginGrid.Begin();
        }

        //starts ShowSignUpGrid animation
        void showSignUpGrid()
        {
            Storyboard ShowSignUpGrid = this.FindResource("ShowSignUpGrid") as Storyboard;
            ShowSignUpGrid.Begin();
        }

        private void LoginGrid_MouseUp(object sender, MouseButtonEventArgs e)   // expand LoginGrid if we're home
        {
            if (currentView == 0)
            {
                showLoginGrid();
            }
        }
        private void LoginGridTooltip_KeyDown(object sender, KeyEventArgs e)    //expand LoginGrid if we're home
        {
            if (e.Key == Key.Enter && currentView == 0)
            {
                showLoginGrid();
            }
        }

        private void SignUpGrid_MouseUp(object sender, MouseButtonEventArgs e)  //expand SignUpGrid if we're home
        {
            if (currentView == 0)
            {
                showSignUpGrid();
            }
        }
        private void SignUpGridTooltip_KeyDown(object sender, KeyEventArgs e) //expand SignUpGrid if we're home
        {
            if (e.Key == Key.Enter && currentView == 0)
            {
                showSignUpGrid();
            }
        }
        private void SearchGrid_MouseUp(object sender, MouseButtonEventArgs e)  //expand SearchGrid if we're home
        {
            if (currentView == 0)
            {
                showSearchGrid();
            }
        }
        private void SearchGridTooltip_KeyDown(object sender, KeyEventArgs e) //expand SearchGrid if we're home
        {
            if (e.Key == Key.Enter && currentView == 0)
            {
                showSearchGrid();
            }
        }

        // Language
        private void LanguageGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            // Show languages when mouse enters the LanguageGrid
            showLanguages(middleFlagTransform.Y, bottomFlagTransform.Y);
        }
        private void LanguageGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            hideLanguages(middleFlagTransform.Y, bottomFlagTransform.Y);
        }

        // search behaviour
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                SearchBox.Text = "";
            }
        }
        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "")
            {
                SearchBox.Text = Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture);
            }
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Text == Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                SearchBox.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
            }
            else
            {
                // Console.Beep(); // just for fun. EDIT: not really fun since it makes the search laggy
                SearchBox.Foreground = new SolidColorBrush(Colors.Black);

                // Search for books if the search term is long enough
                if (SearchBox.Text.Length >= minSearchChars)
                {
                    updateSearchResults();
                }
                // If the search term is shorter, display nothing
                else
                {
                    SearchResultsListBox.ItemsSource = null;
                }

            }
        }

        // Login UsernameField input field behaviour
        private void UsernameField_GotFocus(object sender, RoutedEventArgs e)
        {
            if (UsernameField.Text == Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                UsernameField.Text = "";
            }
        }
        private void UsernameField_LostFocus(object sender, RoutedEventArgs e)
        {
            if (UsernameField.Text == "")
            {
                UsernameField.Text = Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture);
            }
        }
        private void UsernameField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UsernameField.Text == Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                UsernameField.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
            }
            else
            {
                UsernameField.Foreground = new SolidColorBrush(Colors.Black);

                //making sure password is hidden if admin credentials not present
                PasswordField.Visibility = Visibility.Hidden;
                Grid.SetRowSpan(LoginButton, 1);

                //making password field visible when admin credentials are present
                if (UsernameField.Text == "admin")
                {
                    PasswordField.Visibility = Visibility.Visible;
                    Grid.SetRowSpan(LoginButton, 2);
                }
            }
        }

        // Sign up input field behaviour
        private void SignUpField_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SignUpField.Text == "")
            {
                SignUpField.Text = Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture);
            }
        }
        private void SignUpField_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SignUpField.Text == Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                SignUpField.Text = "";
            }
        }
        private void SignUpField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SignUpField.Text == Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
            {
                SignUpField.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
            }
            else
            {
                SignUpField.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        // User database related handling (login / signing up)
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsernameField.Text == "admin" && PasswordField.Password == "kuulkala")
            {
                adminwindow = new AdminControlsWindow();
                adminwindow.Show();
            }
        }
        private void signupButton1_Click(object sender, RoutedEventArgs e)
        {
            if (SignUpField.Text.EndsWith("@edu.jns.fi") && SignUpField.Text.Length > 11) // verify that email contains proper ending and that it also contains other text
            {
                // hide previous UI elements
                SignUpButton.Visibility = Visibility.Hidden;
                SignUpField.Visibility = Visibility.Hidden;

                // send code to the user
                defaultSignUpOperation.generateCode();
                defaultSignUpOperation.addEmail(SignUpField.Text);
                defaultSignUpOperation.sendCode();

                // show new UI elements
                SignUpInstruction.Text = Properties.Resources.ResourceManager.GetString("SignUpInstruction2", TranslationSource.Instance.CurrentCulture); // TODO: move this thi xaml
                SignUpEmailLink.Visibility = Visibility.Visible;
                SignUpConfirmationField.Visibility = Visibility.Visible;
                SignUpConfirmationButton.Visibility = Visibility.Visible;
            }
            else
            {
                defaultSignUpOperation.reset();
                MessageBox.Show(Properties.Resources.ResourceManager.GetString("SignUpErrorMessage", TranslationSource.Instance.CurrentCulture));
            }

        }
        private void SignUpConfirmationButton_Click(object sender, RoutedEventArgs e)
        {
            // if the code given by the user matches the one stored in memory
            if (defaultSignUpOperation.compareCode(SignUpConfirmationField.Text))
            {
                // add the new user to the database and return to main view
                MessageBox.Show("TODO: Add user to the database and return to the main view for login");
                // TODO: Add user to the database
                // TODO: Return to main view
            }
            else
            {
                // complain to the user
                MessageBox.Show(Properties.Resources.ResourceManager.GetString("SignUpWrongCode", TranslationSource.Instance.CurrentCulture));
                defaultSignUpOperation.reset();
                // reset to default view
                // TODO: Reset to default view
            }
        }

        // Program exit behaviour
        private void RootWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // close children window when the main window is closed
                adminwindow.Close();
            }
            catch
            {
                // nothing wrong here.
            }
        }

        // Required for hyperlinks to work
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
        #endregion

        // Language button behaviour and language selection 
        #region Language

        // Change language
        private void English_Click(object sender, RoutedEventArgs e)
        {
            changeUILanguage("en-GB");
        }
        private void Swedish_Click(object sender, RoutedEventArgs e)
        {
            changeUILanguage("sv-SE");
        }
        private void Finnish_Click(object sender, RoutedEventArgs e)
        {
            changeUILanguage("fi-FI");
        }
        void changeUILanguage(string language)
        {
            //has to be done manyally because we assign it string values elsewhere, which replaces the automatic switching
            bool updateSearchBoxText = false;
            bool updateLogInUserNameBoxText = false;
            bool updateSignupField = false;

            if (UsernameField.Text == Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
                updateLogInUserNameBoxText = true;
            if (SearchBox.Text == Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture))
                updateSearchBoxText = true;
            if (SignUpField.Text == Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
                updateSignupField = true;

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
                SearchBox.Text = Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture);
            }

            if (updateLogInUserNameBoxText)
            {
                UsernameField.Text = Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture);
            }

            if (updateSignupField)
            {
                SignUpField.Text = Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture);
            }
        }

        // Language box animations
        private void showLanguages(double middleY, double bottomY)
        {
            Duration duration = new Duration(TimeSpan.FromMilliseconds(250));

            // transform the middle flag from current position to middle
            middleFlagTransform = new TranslateTransform(0, middleY);
            DoubleAnimation anim = new DoubleAnimation(40, duration);
            middleFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim);
            Swedish.RenderTransform = middleFlagTransform;

            // transform the bottom flag from current position to bottom
            bottomFlagTransform = new TranslateTransform(0, bottomY);
            DoubleAnimation anim2 = new DoubleAnimation(80, duration);
            bottomFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim2);
            English.RenderTransform = bottomFlagTransform;
        }
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

        //TODO: Add support for advanced search, currently hard-coded for ID, Author and Title only
        private void updateSearchResults()
        {
            List<String> columns = new List<String>();
            columns.Add("ID");
            columns.Add("Author");
            columns.Add("Title");
            /**
            // Determine what to search        
            if (IDChecked) columns.Add("BookID");
            if (TitleChecked) columns.Add("Title");
            if (AuthorChecked) columns.Add("Author");
            if (YearChecked) columns.Add("Year");
            if (LanguageChecked) columns.Add("Language");
            if (AvailableChecked) columns.Add("Available");
            **/

            List<List<String>> searchResults = new List<List<String>>();
            searchResults = dbi.searchDatabaseRows(dbconnection, "books", SearchBox.Text, columns);
            ListBoxItems = new List<Book>();

            foreach (List<String> row in searchResults)
            {
                ListBoxItems.Add(new Book() { ID = row[0], Author = row[1], Title = row[2], Year = row[3], Language = row[4], Available = row[5], ISBN = row[6], Category = row[7] });
            }
            SearchResultsListBox.ItemsSource = ListBoxItems;
        }

        #endregion

        #region Debug
        private void a(String m)
        {
            MessageBox.Show(m);
        }

        #endregion


        private async void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Book currentBook = SearchResultsListBox.SelectedItem as Book;
            if (currentBook != null)
            {
                string url = await retrieveCoverArtAsync(currentBook.ISBN);
                if (url != String.Empty)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(url, UriKind.Absolute);
                    bitmap.EndInit();
                    coverArt.Source = bitmap;
                }
                else
                {
                    coverArt.Source = null;
                }
            }

        }

        async Task<string> retrieveCoverArtAsync(string isbn)
        {

            // Create a request for the URL. 

            using (var client = new HttpClient())
            {
                HttpResponseMessage response = new HttpResponseMessage();
                try
                {
                    response = await client.GetAsync("https://www.googleapis.com/books/v1/volumes?q=+isbn:" + isbn);
                }
                catch
                {
                    //No internet connection (or some other problem)
                    return String.Empty;
                }

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    JObject bookInfoObject = JObject.Parse(content);

                    JToken volumeCountItem = bookInfoObject["totalItems"];

                    if (volumeCountItem.Value<int>() > 0)
                    {
                        //then we have to get the image links separately
                        try
                        {
                            JToken imageLinkItem = bookInfoObject["items"].First["volumeInfo"]["imageLinks"]["thumbnail"];
                            return imageLinkItem.ToString();

                        }
                        catch
                        {
                            //No cover art
                        }

                    }
                    else
                    {
                        //no data available
                    }
                }
                client.Dispose();
            }

            return String.Empty;
        }

    }

}


