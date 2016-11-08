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
using System.Windows.Media.Effects;

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

        private Double _doubleCollapsedGridHeight;
        public Double DoubleCollapsedGridHeight
        {
            get { return _doubleCollapsedGridHeight; }
            set
            {
                if (value != _doubleCollapsedGridHeight)
                {
                    _doubleCollapsedGridHeight = value;
                    Notify("DoubleCollapsedGridHeight");
                }
            }
        }

        private Double _hoverCollapsedGridHeight;
        public Double HoverCollapsedGridHeight
        {
            get { return _hoverCollapsedGridHeight; }
            set
            {
                if (value != _hoverCollapsedGridHeight)
                {
                    _hoverCollapsedGridHeight = value;
                    Notify("HoverCollapsedGridHeight");
                }
            }
        }

        private Double _deHoverCollapsedGridHeight;
        public Double DeHoverCollapsedGridHeight
        {
            get { return _deHoverCollapsedGridHeight; }
            set
            {
                if (value != _deHoverCollapsedGridHeight)
                {
                    _deHoverCollapsedGridHeight = value;
                    Notify("DeHoverCollapsedGridHeight");
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
        // 4 = Logged In Home View (UserInfoGrid + SearchGrid)
        byte currentView = 0;

        // Variables for database interaction
        private SQLiteConnection dbconnection = new SQLiteConnection("Data Source=database.db");
        private DatabaseInteraction dbi = new DatabaseInteraction();
        List<Book> ListBoxItems;
        // Variables required by search
        // Require at least this number of characters before searching to avoid search congestion
        // Global variable, this is used also to control search in AdminControls
        public const uint minSearchChars = 3;

        // Variables for SignUp operation
        SignUpOperation defaultSignUpOperation = new SignUpOperation();

        // Variables for LoginSession
        LoginSession defaultLoginSession = new LoginSession();
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
            Double ExpandedGridHeight = WindowGrid.ActualHeight - (HeaderGrid.ActualHeight);
            Double CollapsedGridHeight = ExpandedGridHeight / 3;
            Double HoverCollapsedGridHeight = CollapsedGridHeight + 20;
            Double DeHoverCollapsedGridHeight = CollapsedGridHeight - 10;
            GridHeightClass.Instance.ExpandedGridHeight = ExpandedGridHeight;
            GridHeightClass.Instance.CollapsedGridHeight = CollapsedGridHeight;
            GridHeightClass.Instance.DoubleCollapsedGridHeight = 2 * CollapsedGridHeight;
            GridHeightClass.Instance.HoverCollapsedGridHeight = HoverCollapsedGridHeight;
            GridHeightClass.Instance.DeHoverCollapsedGridHeight = DeHoverCollapsedGridHeight;

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
                case 4:
                    UserInfoGrid.Height = 2 * CollapsedGridHeight;
                    SearchGrid.Height = CollapsedGridHeight;
                    SignUpGrid.Height = 0;
                    LoginGrid.Height = 0;
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

        private void logOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (defaultLoginSession.loggedIn)
            {
                Storyboard ShowHomeView = this.FindResource("ShowHomeView") as Storyboard;
                ShowHomeView.Begin();
                currentView = 0;
                defaultLoginSession.end();
            }

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //the 'return to home' button has been clicked, so now we have to determine where we are to get back home

            // return the signUp screen to its clean state
            defaultSignUpOperation.reset();
            SignUpField.Text = "";
            SignUpConfirmationField.Text = "";
            SignUpButton.Visibility = Visibility.Visible;
            SignUpField.Visibility = Visibility.Visible;
            SignUpInstruction.Text = Properties.Resources.ResourceManager.GetString("SignUpInstruction1", TranslationSource.Instance.CurrentCulture); // TODO: move this thi xaml
            SignUpEmailLink.Visibility = Visibility.Collapsed;
            SignUpConfirmationField.Visibility = Visibility.Collapsed;
            SignUpConfirmationButton.Visibility = Visibility.Collapsed;

            if (defaultLoginSession.loggedIn)
            {
                Storyboard ShowLoggedInHomeView = this.FindResource("ShowLoggedInHomeView") as Storyboard;
                ShowLoggedInHomeView.Begin();
                currentView = 4;

                // update loans listbox
                updateLoansListbox();
            }
            else
            {
                switch (currentView)
                {
                    case 0:
                    case 4:
                        //In home view or user info view
                        showHazardNotification("Home button should not be accessible in home screen... Go fix the program!");
                        break;
                    case 1:
                    case 2:
                    case 3:
                        //In search view
                        Storyboard ShowHomeView = this.FindResource("ShowHomeView") as Storyboard;
                        ShowHomeView.Begin();
                        break;
                }
                currentView = 0; //we're home now
            }

        }


        #region Storyboard Complete Event Handlers
        void ResetAnimationsAfterArrivingToHomeView()
        {
            //Resets animation so grids can be resized again

            LoginGrid.BeginAnimation(Grid.HeightProperty, null);
            SignUpGrid.BeginAnimation(Grid.HeightProperty, null);
            SearchGrid.BeginAnimation(Grid.HeightProperty, null);
            UserInfoGrid.BeginAnimation(Grid.HeightProperty, null);

            switch (currentView)
            {
                case 0:
                    LoginGrid.Height = GridHeightClass.Instance.CollapsedGridHeight;
                    SignUpGrid.Height = GridHeightClass.Instance.CollapsedGridHeight;
                    SearchGrid.Height = GridHeightClass.Instance.CollapsedGridHeight;
                    UserInfoGrid.Height = 0;
                    break;
                case 4:
                    LoginGrid.Height = 0;
                    SignUpGrid.Height = 0;
                    SearchGrid.Height = GridHeightClass.Instance.CollapsedGridHeight;
                    UserInfoGrid.Height = 2 * GridHeightClass.Instance.CollapsedGridHeight;
                    break;
            }
        }

        private void ShowSearchGridStoryboard_Completed(object sender, EventArgs e)
        {
            SearchGrid.BeginAnimation(Grid.HeightProperty, null); //removes the ShowSearchGrid animation to allow for code-behind height change to take place
            SearchGrid.Height = GridHeightClass.Instance.ExpandedGridHeight; //apply the correct height value to SearchGrid (wihtout this SearchGrid would revert back to original height value
            Keyboard.Focus(SearchBox);
        }

        private void ShowLoginGridStoryboard_Completed(object sender, EventArgs e)
        {
            LoginGrid.BeginAnimation(Grid.HeightProperty, null); //removes the ShowLoginGrid animation to allow for code-behind height change to take place
            LoginGrid.Height = GridHeightClass.Instance.ExpandedGridHeight;
        }

        private void ShowSignUpGridStoryboard_Completed(object sender, EventArgs e)
        {
            //removes the ShowSignUpGrid animation to allow for code-behind height change to take place
            SignUpGrid.BeginAnimation(Grid.HeightProperty, null);
            SignUpGrid.Height = GridHeightClass.Instance.ExpandedGridHeight;
        }

        private void ShowLoggedInHomeViewStoryboard_Completed(object sender, EventArgs e)
        {
            ResetAnimationsAfterArrivingToHomeView();
        }

        private void ShowHomeViewStoryboard_Completed(object sender, EventArgs e)
        {
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
            currentView = 1;
            Storyboard ShowSearchGrid = this.FindResource("ShowSearchGrid") as Storyboard;
            ShowSearchGrid.Begin();
        }

        //starts ShowLoginGrid animation
        void showLoginGrid()
        {
            currentView = 2;
            Storyboard ShowLoginGrid = this.FindResource("ShowLoginGrid") as Storyboard;
            ShowLoginGrid.Begin();
        }

        //starts ShowSignUpGrid animation
        void showSignUpGrid()
        {
            currentView = 3;
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

        //TODO: probably not necessary since keyboard navigation is disabled
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
            if (currentView == 0 || currentView == 4)
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

                UsernameField.Text = "";
                PasswordField.Password = "";
            }
            else if (UsernameField.Text.EndsWith("@edu.jns.fi") && UsernameField.Text.Length > 11)
            {
                // search for the entered wilma account in the database
                List<string> columns = new List<string>();
                columns.Add("Wilma");
                List<List<string>> results = new List<List<string>>();
                results = dbi.searchExactDatabaseRows(dbconnection, "users", UsernameField.Text.ToLower(), columns);
                if (results.Count == 1)
                {
                    // log in
                    defaultLoginSession.begin(results[0][0], results[0][1]);

                    // clear UsernameField
                    UsernameField.Text = "";

                    // greet the user
                    loggedInUserGreeting.Text = Properties.Resources.ResourceManager.GetString("loggedInUserGreeting", TranslationSource.Instance.CurrentCulture);
                    loggedInUserGreeting.Text = String.Format(loggedInUserGreeting.Text, defaultLoginSession.Name);

                    updateLoansListbox();         

                    // go to logged in view
                    Storyboard ShowLoggedInHomeView = this.FindResource("ShowLoggedInHomeView") as Storyboard;
                    ShowLoggedInHomeView.Begin();
                    currentView = 4;
                }
                else
                {
                    showHazardNotification("Error: Account does not exist. Please create an account in the Sign Up page.");
                }
            }
            else
            {
                showHazardNotification("Error: The username does not seem legit. Please use your wilma address for logging in.");
            }

        }
        private void signupButton1_Click(object sender, RoutedEventArgs e)
        {
            defaultSignUpOperation.reset();
            Boolean isLegit = false;
            // verify that email contains proper ending and that it also contains other text
            if (SignUpField.Text.EndsWith("@edu.jns.fi") && SignUpField.Text.Length > 11)
            {
                // search if the user database already contains the same wilma account

                List<string> columns = new List<string>();
                columns.Add("Wilma");
                List<List<string>> results = new List<List<string>>();
                results = dbi.searchExactDatabaseRows(dbconnection, "users", SignUpField.Text.ToLower(), columns);

                // show relevant error message and determine if the wilma account is in use
                if (results.Count > 1)
                {
                    isLegit = false;
                    showHazardNotification("Beep boop! Something went wrong and you should contact the person responsible for managing this!");
                }
                else if (results.Count == 1)
                {
                    isLegit = false;
                    showHazardNotification("Error: This Wilma address is already in use.");
                }
                else if (results.Count == 0)
                {
                    isLegit = true;
                }
            }
            else
            {
                showHazardNotification(Properties.Resources.ResourceManager.GetString("SignUpErrorMessage", TranslationSource.Instance.CurrentCulture));
            }

            if (isLegit)
            {
                // store address
                defaultSignUpOperation.setWilma(SignUpField.Text.ToLower());

                // hide previous UI elements
                SignUpButton.Visibility = Visibility.Hidden;
                SignUpField.Visibility = Visibility.Hidden;

                // send code to the user

                defaultSignUpOperation.generateCode();
                defaultSignUpOperation.addEmail(SignUpField.Text.ToLower());
                defaultSignUpOperation.sendCode();

                // for development purposes only, remove this when finished
                // defaultSignUpOperation.displayCode();

                // show new UI elements
                SignUpInstruction.Text = Properties.Resources.ResourceManager.GetString("SignUpInstruction2", TranslationSource.Instance.CurrentCulture); // TODO: move this thi xaml
                SignUpEmailLink.Visibility = Visibility.Visible;
                SignUpConfirmationField.Visibility = Visibility.Visible;
                SignUpConfirmationButton.Visibility = Visibility.Visible;
            }


        }
        private void SignUpConfirmationButton_Click(object sender, RoutedEventArgs e)
        {
            // if the code given by the user matches the one stored in memory
            if (defaultSignUpOperation.compareCode(SignUpConfirmationField.Text))
            {
                // generate new ID for the user
                defaultSignUpOperation.generateID();

                // verify that the generated ID is not in use
                // if it is, increment the number in ID until an unique ID is found
                Boolean isUnique;
                do
                {
                    isUnique = defaultSignUpOperation.verifyID();
                    if (!isUnique)
                    {
                        defaultSignUpOperation.incrementID();
                    }
                }
                while (!isUnique); // increment the ID until no conflicting IDs are found

                // add the user to user database table
                List<String> columns = new List<String>();
                columns.Add(defaultSignUpOperation.getWilma()); // Wilma
                columns.Add("");                                // Loans
                dbi.addDatabaseRow(dbconnection, "users", defaultSignUpOperation.getID(), columns);       

                // log in and greet the user
                defaultLoginSession.begin(defaultSignUpOperation.getID(), SignUpField.Text.ToLower());
                loggedInUserGreeting.Text = String.Format(loggedInUserGreeting.Text, defaultLoginSession.Name);

                // go to logged in view
                Storyboard ShowLoggedInHomeView = this.FindResource("ShowLoggedInHomeView") as Storyboard;
                ShowLoggedInHomeView.Begin();
                currentView = 4;

                // account creation complete, reset defaultSignUpOperation to its clean state
                defaultSignUpOperation.reset();

                // return the content in signUpGrid to its original state
                SignUpField.Text = "";
                SignUpConfirmationField.Text = "";
                SignUpButton.Visibility = Visibility.Visible;
                SignUpField.Visibility = Visibility.Visible;
                SignUpInstruction.Text = Properties.Resources.ResourceManager.GetString("SignUpInstruction1", TranslationSource.Instance.CurrentCulture); // TODO: move this thi xaml
                SignUpEmailLink.Visibility = Visibility.Hidden;
                SignUpConfirmationField.Visibility = Visibility.Hidden;
                SignUpConfirmationButton.Visibility = Visibility.Hidden;
            }
            else
            {
                // complain to the user
                showHazardNotification(Properties.Resources.ResourceManager.GetString("SignUpWrongCode", TranslationSource.Instance.CurrentCulture));
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

            // update logged in user greeting
            loggedInUserGreeting.Text = Properties.Resources.ResourceManager.GetString("loggedInUserGreeting", TranslationSource.Instance.CurrentCulture);
            loggedInUserGreeting.Text = String.Format(loggedInUserGreeting.Text, defaultLoginSession.Name);
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

        private void updateSearchResults()
        {
            // determine what to search
            List<String> columns = new List<String>();
            if (IDCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("ID");
            if (TitleCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Title");
            if (AuthorCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Author");
            if (YearCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Year");
            if (LanguageCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Language");
            if (AvailabilityCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Available");
            if (CategoryCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Aineistolaji");
            if (ISBNCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("ISBN");

            List<List<String>> searchResults = new List<List<String>>();
            searchResults = dbi.searchDatabaseRows(dbconnection, "books", SearchBox.Text, columns);
            ListBoxItems = new List<Book>();

            foreach (List<String> row in searchResults)
            {
                ListBoxItems.Add(new Book(row[0], row[1], row[2], row[3], row[4], row[5], row[6], row[7]));
            }
            SearchResultsListBox.ItemsSource = ListBoxItems;
        }

        // search using enter key
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                updateSearchResults();
            }
        }

        #endregion

        #region Debug
        private void a(String m)
        {
            MessageBox.Show(m);
        }

        #endregion


        private void LoansListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //TODO: Add Functionality
        }

        private async void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Book currentBook = SearchResultsListBox.SelectedItem as Book;
            if (currentBook != null)
            {
                // check whether the book has been borrowed or not
                if (currentBook.available == "TRUE")
                {
                    availability.Text = Properties.Resources.ResourceManager.GetString("bookAvailable", TranslationSource.Instance.CurrentCulture);
                    loanButton.IsEnabled = true;
                }
                else
                {
                    availability.Text = Properties.Resources.ResourceManager.GetString("bookNotAvailable", TranslationSource.Instance.CurrentCulture);
                    loanButton.IsEnabled = false;
                }

                //show text depending on if the user is logged in or not
                if (defaultLoginSession.loggedIn)
                {
                    logInNotice.Visibility = Visibility.Collapsed;
                    loanButton.Visibility = Visibility.Visible;
                }
                else
                {
                    logInNotice.Visibility = Visibility.Visible;
                    loanButton.Visibility = Visibility.Collapsed;
                }
                if (currentBook.isbn != "")
                {
                    //retrieve cover art
                    string url = await retrieveCoverArtAsync(currentBook.isbn);
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
                else
                {
                    coverArt.Source = null;
                }
            }
            else
            {
                coverArt.Source = null;
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

        private void quickReturnButton_Click(object sender, RoutedEventArgs e)
        {
            Storyboard ShowQuickReturnView = this.FindResource("ShowQuickReturnView") as Storyboard;
            ShowQuickReturnView.Begin();
        }

        private void cancelQuickReturnButton_Click(object sender, RoutedEventArgs e)
        {
            Storyboard HideQuickReturnView = this.FindResource("HideQuickReturnView") as Storyboard;
            HideQuickReturnView.Begin();
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            Keyboard.Focus(loanReturnBox);
        }

        private void loanButton_Click(object sender, RoutedEventArgs e)
        {
            //Storyboard ShowLoadingView = this.FindResource("ShowLoadingView") as Storyboard;
            //ShowLoadingView.Begin();

            if (!defaultLoginSession.loggedIn)
            {
                // The user is trying to borrow a book without being signed in. This message should never be visible.
                showHazardNotification("You are doing it all wrong! Please log in before attempting to borrow books.");
                return;
            }

            // Find out which book is selected
            Book selection = SearchResultsListBox.SelectedItem as Book;

            // Search user's entries in the database
            List<string> columns = new List<string>();
            columns.Add("ID");
            List<List<string>> results = new List<List<string>>();
            results = dbi.searchExactDatabaseRows(dbconnection, "users", defaultLoginSession.ID, columns);

            if (results.Count != 1) throw new Exception("There should be only one user loaning the book");

            User user = new User(results[0][0], results[0][1], results[0][2]);

            // add book to loans
            List<String> loans = user.getLoans();
            loans.Add(selection.id);

            // parse loans to csv
            String loans_csv = loans[0];
            if (loans.Count > 1) // if there are more than one books to add to csv
            {
                for (int i = 1; i < loans.Count; i++)
                {
                    loans_csv += String.Format(";{0}", loans[i]);
                }
            }

            // update user table entry for loans
            dbi.commitDbChanges(dbconnection, "users", loans_csv, defaultLoginSession.ID, "Loans");

            // change availability to false in book database table
            dbi.commitDbChanges(dbconnection, "books", "FALSE", selection.id, "Available");

            // update book displayed info
            availability.Text = Properties.Resources.ResourceManager.GetString("bookNotAvailable", TranslationSource.Instance.CurrentCulture);
            loanButton.IsEnabled = false;
            showHazardNotification("Book borrowed successfully!");
            updateSearchResults();

        }

        private void SearchGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (currentView == 0)
            {
                Storyboard MouseEnterSearchGrid = this.FindResource("MouseEnterSearchGrid") as Storyboard;
                MouseEnterSearchGrid.Begin();
            }
            if (currentView == 4)
            {
                Storyboard MouseEnterLoggedInSearchGrid = this.FindResource("MouseEnterLoggedInSearchGrid") as Storyboard;
                MouseEnterLoggedInSearchGrid.Begin();
            }
        }

        private void LoginGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (currentView == 0)
            {
                Storyboard MouseEnterLoginGrid = this.FindResource("MouseEnterLoginGrid") as Storyboard;
                MouseEnterLoginGrid.Begin();
            }
        }

        private void SignUpGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (currentView == 0)
            {
                Storyboard MouseEnterSignUpGrid = this.FindResource("MouseEnterSignUpGrid") as Storyboard;
                MouseEnterSignUpGrid.Begin();
            }
        }

        private void MouseLeaveGridStoryboard_Completed(object sender, EventArgs e)
        {
            ResetAnimationsAfterArrivingToHomeView();
        }

        //return the grids to default heights when the mouse leaves them completely
        private void NavigationStackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (currentView == 0)
            {
                Storyboard MouseLeaveGrid = this.FindResource("MouseLeaveGrid") as Storyboard;
                MouseLeaveGrid.Begin();
            }
        }

        private void dismissHazardNotificationButton_Click(object sender, RoutedEventArgs e)
        {
            Storyboard HideHazardNotification = this.FindResource("HideHazardNotification") as Storyboard;
            HideHazardNotification.Begin();
        }

        void showHazardNotification(string message)
        {
            hazardNotificationText.Text = message;
            Storyboard ShowHazardNotification = this.FindResource("ShowHazardNotification") as Storyboard;
            ShowHazardNotification.Begin();
        }

        private void SearchGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if(currentView==4)
            {
                Storyboard MouseLeaveLoggedInSearchGrid = this.FindResource("MouseLeaveLoggedInSearchGrid") as Storyboard;
                MouseLeaveLoggedInSearchGrid.Begin();
            }
        }

        private void Storyboard_Completed_1(object sender, EventArgs e)
        {
            ResetAnimationsAfterArrivingToHomeView();
        }

        private void updateLoansListbox()
        {
            // populate the users loans listbox
            List<string> columns = new List<string>();
            columns.Add("ID");
            List<List<string>> results = new List<List<string>>();
            results = dbi.searchExactDatabaseRows(dbconnection, "users", defaultLoginSession.ID, columns);

            if (results.Count != 1) throw new Exception("There should be only one user logged in");

            User user = new User(results[0][0], results[0][1], results[0][2]);

            // add book to loans
            List<String> loanIDs = user.getLoans();
            List<Book> loans = new List<Book>();

            foreach (String ID in loanIDs)
            {
                // fetch the remaining book info
                List<List<String>> searchResults = new List<List<String>>();
                searchResults = dbi.searchDatabaseRows(dbconnection, "books", ID, columns);
                if (searchResults.Count != 1) throw new Exception("Conflicting book IDs!");

                loans.Add(new Book(ID, searchResults[0][1], searchResults[0][2], searchResults[0][3],
                    searchResults[0][4], searchResults[0][5], searchResults[0][6], searchResults[0][7]));
            }

            LoansListBox.ItemsSource = loans;
        }

        private void returnSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (!defaultLoginSession.loggedIn)
            {
                // The user is trying to borrow a book without being signed in. This message should never be visible.
                showHazardNotification("This button should only be available to users logged in.");
                return;
            }

            // Find out which book is selected
            Book selection = LoansListBox.SelectedItem as Book;

            // Search user's entries in the database
            List<string> columns = new List<string>();
            columns.Add("ID");
            List<List<string>> results = new List<List<string>>();
            results = dbi.searchExactDatabaseRows(dbconnection, "users", defaultLoginSession.ID, columns);

            if (results.Count != 1) throw new Exception("There should be only one user returning the book");

            User user = new User(results[0][0], results[0][1], results[0][2]);

            // remove book from loans
            List<String> loans = user.getLoans();
            loans.Remove(selection.id);

            // parse loans to csv
            String loans_csv = "";
            if (loans.Count > 1) // if there are more than one books to add to csv
            {
                loans_csv = loans[0];
                for (int i = 1; i < loans.Count; i++)
                {
                    loans_csv += String.Format(";{0}", loans[i]);
                }
            }

            // update user table entry for loans
            dbi.commitDbChanges(dbconnection, "users", loans_csv, defaultLoginSession.ID, "Loans");

            // change availability to false in book database table
            dbi.commitDbChanges(dbconnection, "books", "TRUE", selection.id, "Available");

            // update book displayed info
            availability.Text = Properties.Resources.ResourceManager.GetString("bookNotAvailable", TranslationSource.Instance.CurrentCulture);
            loanButton.IsEnabled = false;
            showHazardNotification("Book returned successfully!");
            updateLoansListbox();
        }

        private void returnAllButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void returnOtherButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }


}


