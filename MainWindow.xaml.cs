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


        //what answer did the user give in the yes no prompt?
        // 0 = no answer yet
        // 1 = yes
        // 2 = no
        byte yesNoPromptAnswer = 0;

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
        private void UsernameField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            if (UsernameField.Text == "admin" && e.Key == Key.Tab)
            {
                Keyboard.Focus(PasswordField);
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

            // empty search results
            SearchResultsListBox.ItemsSource = null;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //the 'return to home' button has been clicked, so now we have to determine where we are to get back home

            // return the signUp screen to its clean state
            defaultSignUpOperation.reset();
            SignUpField.Text = Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture);
            SignUpConfirmationField.Text = "";
            SignUpButton.Visibility = Visibility.Visible;
            SignUpField.Visibility = Visibility.Visible;
            SignUpInstruction.Text = Properties.Resources.ResourceManager.GetString("SignUpInstruction1", TranslationSource.Instance.CurrentCulture);
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

                // clear search results
                SearchResultsListBox.ItemsSource = null;

                //hide the borrow button
                loanButton.Visibility = Visibility.Collapsed;
                loanButton.IsEnabled = false;
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
            // clear search results and search text
            SearchResultsListBox.ItemsSource = null;
            SearchBox.Text = "";

            // clear the book availability message
            availability.Text = "";

            // hide login message
            logInNotice.Visibility = Visibility.Collapsed;

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
                    showHazardNotification(Properties.Resources.ResourceManager.GetString("LoginErrorAccountDoesntExist", TranslationSource.Instance.CurrentCulture));
                }
            }
            else
            {
                showHazardNotification(Properties.Resources.ResourceManager.GetString("LoginErrorIncorrectAddress", TranslationSource.Instance.CurrentCulture));
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
                    showHazardNotification(Properties.Resources.ResourceManager.GetString("SignUpErrorAccountAlreadyInUse", TranslationSource.Instance.CurrentCulture));
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
                SignUpInstruction.Text = Properties.Resources.ResourceManager.GetString("SignUpInstruction2", TranslationSource.Instance.CurrentCulture);
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
                SignUpInstruction.Text = Properties.Resources.ResourceManager.GetString("SignUpInstruction1", TranslationSource.Instance.CurrentCulture);
                SignUpEmailLink.Visibility = Visibility.Hidden;
                SignUpConfirmationField.Visibility = Visibility.Hidden;
                SignUpConfirmationButton.Visibility = Visibility.Hidden;
            }
            else
            {
                // complain to the user
                showHazardNotification(Properties.Resources.ResourceManager.GetString("SignUpWrongCode", TranslationSource.Instance.CurrentCulture));
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
            bool updateSignUpInstructionText1 = false;
            bool updateSignUpInstructionText2 = false;

            if (UsernameField.Text == Properties.Resources.ResourceManager.GetString("DefaultLoginUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
                updateLogInUserNameBoxText = true;
            if (SearchBox.Text == Properties.Resources.ResourceManager.GetString("DefaultSearchBoxContent", TranslationSource.Instance.CurrentCulture))
                updateSearchBoxText = true;
            if (SignUpField.Text == Properties.Resources.ResourceManager.GetString("DefaultSignUpUsernameBoxContent", TranslationSource.Instance.CurrentCulture))
                updateSignupField = true;
            if (SignUpInstruction.Text == Properties.Resources.ResourceManager.GetString("SignUpInstruction2", TranslationSource.Instance.CurrentCulture))
                updateSignUpInstructionText2 = true;
            if (SignUpInstruction.Text == Properties.Resources.ResourceManager.GetString("SignUpInstruction1", TranslationSource.Instance.CurrentCulture))
                updateSignUpInstructionText1 = true;


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

            if (updateSignUpInstructionText2)
            {
                SignUpInstruction.Text = Properties.Resources.ResourceManager.GetString("SignUpInstruction2", TranslationSource.Instance.CurrentCulture);
            }

            if (updateSignUpInstructionText1)
            {
                SignUpInstruction.Text = Properties.Resources.ResourceManager.GetString("SignUpInstruction1", TranslationSource.Instance.CurrentCulture);
            }

            // update logged in user greeting
            loggedInUserGreeting.Text = Properties.Resources.ResourceManager.GetString("loggedInUserGreeting", TranslationSource.Instance.CurrentCulture);
            loggedInUserGreeting.Text = String.Format(loggedInUserGreeting.Text, defaultLoginSession.Name);

            //update availability count. First let's check if there's a book selected
            try
            {
                Book currentBook = SearchResultsListBox.SelectedItem as Book;
                if (currentBook != null)
                {
                    availability.Text = String.Format(Properties.Resources.ResourceManager.GetString("booksAvailable", TranslationSource.Instance.CurrentCulture), Int32.Parse(currentBook.available), Int32.Parse(currentBook.amount));
                }
            }
            catch
            {
                MessageBox.Show("An error occured while trying to determine the number of books available. Please seek help from the library administrator.");
                availability.Text = "ERROR!";
                loanButton.IsEnabled = false;
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
            if (CategoryCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("Category");
            if (ISBNCheckBox.IsChecked.GetValueOrDefault() == true) columns.Add("ISBN");

            List<List<String>> searchResults = new List<List<String>>();
            searchResults = dbi.searchDatabaseRows(dbconnection, "books", SearchBox.Text, columns);
            ListBoxItems = new List<Book>();

            foreach (List<String> row in searchResults)
            {
                ListBoxItems.Add(new Book(row[0], row[1], row[2], row[3], row[4], row[5], row[6], row[7], row[8]));
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
            Book currentBook = LoansListBox.SelectedItem as Book;
            if (currentBook != null)
            {
                returnSelectedButton.IsEnabled = true;
            }
            else
            {
                returnSelectedButton.IsEnabled = false;
            }
        }
        private async void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Book currentBook = SearchResultsListBox.SelectedItem as Book;
            if (currentBook != null)
            {
                //make book info visible
                Storyboard ShowBookInfo = this.FindResource("ShowBookInfo") as Storyboard;
                ShowBookInfo.Begin();

                // check whether there are books available or not
                try
                {
                    if (Int32.Parse(currentBook.available) > 0)
                    {
                        loanButton.IsEnabled = true;
                    }
                    else
                    {
                        loanButton.IsEnabled = false;
                    }
                    availability.Text = String.Format(Properties.Resources.ResourceManager.GetString("booksAvailable", TranslationSource.Instance.CurrentCulture), Int32.Parse(currentBook.available), Int32.Parse(currentBook.amount));
                }
                catch
                {
                    MessageBox.Show("An error occured while trying to determine the number of books available. Please seek help from the library administrator.");
                    availability.Text = "ERROR!";
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
                    //only show logInNotice if there are books available
                    if (Int32.Parse(currentBook.available) > 0)
                    {
                        logInNotice.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        logInNotice.Visibility = Visibility.Collapsed;
                    }
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
                Storyboard HideBookInfo = this.FindResource("HideBookInfo") as Storyboard;
                HideBookInfo.Begin();
                //hide book info list
                //add link to animation which hides the book info list and shows the placeholder

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

        /* This function is obsolete since quick return functionality has been removed
        private void quickReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add functionality for quick return
            showHazardNotification("Warning! This functionality is broken. Please do not use it. If you are Otto, fix the quick return view!");
            return;

            //Storyboard ShowQuickReturnView = this.FindResource("ShowQuickReturnView") as Storyboard;
            //ShowQuickReturnView.Begin();
        }
        */

        //This function is obsolete since quick return functionality has been removed
        private void cancelQuickReturnButton_Click(object sender, RoutedEventArgs e)
        {
            Storyboard HideQuickReturnView = this.FindResource("HideQuickReturnView") as Storyboard;
            HideQuickReturnView.Begin();
            loanReturnBox.Text = "";
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

            // verify that the book is available
            if (Int32.Parse(selection.available) > 0)
            {
                // Verify that the user loaning the book is unique (i.e. only one user loaning the book per login)
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

                // update the number of specified books available
                Int32 totalAmount = Int32.Parse(selection.amount);
                Int32 newAvailable = Int32.Parse(selection.available) - 1;
                Int32 loaned = totalAmount - newAvailable;

                // change availability to state the new number of availble books
                dbi.commitDbChanges(dbconnection, "books", newAvailable.ToString(), selection.id, "Available");

                // update the book info into listbox to prevent further loaning
                selection.available = newAvailable.ToString();

                // update book displayed info
                availability.Text = String.Format(Properties.Resources.ResourceManager.GetString("booksAvailable", TranslationSource.Instance.CurrentCulture), newAvailable, totalAmount);

                if(newAvailable > 0)
                {
                    loanButton.IsEnabled = true;
                }
                else
                {
                    loanButton.IsEnabled = false;
                }
                //showHazardNotification("Book borrowed successfully!");

            }
            else
            {
                showHazardNotification("Error: This book has already been borrowed by someone.");
            }
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
                    searchResults[0][4], searchResults[0][5], searchResults[0][6], searchResults[0][7], searchResults[0][8]));
            }

            LoansListBox.ItemsSource = loans;
            if (loans.Count>0)
            {
                returnAllButton.IsEnabled = true;
            }
            else
            {
                returnAllButton.IsEnabled = false;
            }
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

            if (selection != null)
            {
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

                // update the number of specified books available
                Int32 newAvailable = Int32.Parse(selection.available) + 1;

                // update the number of books available in the database
                dbi.commitDbChanges(dbconnection, "books", newAvailable.ToString(), selection.id, "Available");

                // parse loans to csv
                String loans_csv = "";
                // if there are more than zero books remaining
                if (loans.Count > 0)
                {
                    loans_csv = loans[0];
                    for (int i = 1; i < loans.Count; i++)
                    {
                        loans_csv += String.Format(";{0}", loans[i]);
                    }
                }

                // update user table entry for loans
                dbi.commitDbChanges(dbconnection, "users", loans_csv, defaultLoginSession.ID, "Loans");               

                // update book displayed info
                availability.Text = Properties.Resources.ResourceManager.GetString("bookNotAvailable", TranslationSource.Instance.CurrentCulture);
                loanButton.IsEnabled = false;
                //showHazardNotification("Book returned successfully!");
                updateLoansListbox();
            }
        }

        public async Task<byte> GetYesNoPromptUserResponseAsync()
        {
            while (yesNoPromptAnswer == 0)
            {
                await Task.Delay(200); //let's wait for a while asynchronously. The user hasn't yet answered
            }
            return yesNoPromptAnswer; //there's an answer!
        }

        private async void returnAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (!defaultLoginSession.loggedIn)
            {
                // The user is trying to borrow a book without being signed in. This message should never be visible.
                showHazardNotification("NO!");
                return;
            }

            yesNoPromptAnswer = 0;
            YesNoPromptText.Text = Properties.Resources.ResourceManager.GetString("confirmReturnAll", TranslationSource.Instance.CurrentCulture);
            Storyboard ShowYesNoPrompt = this.FindResource("ShowYesNoPrompt") as Storyboard;
            ShowYesNoPrompt.Begin();
           
            //start listening for the answer
            byte answer = await GetYesNoPromptUserResponseAsync(); //let's get user response asynchronously
            yesNoPromptAnswer = 0; //reset the variable for next time

            if (answer==1)
            {
                //user answered yes

                if (LoansListBox.Items.Count > 0)
                {
                    // Search user's entries in the database
                    List<string> columns = new List<string>();
                    columns.Add("ID");
                    List<List<string>> results = new List<List<string>>();
                    results = dbi.searchExactDatabaseRows(dbconnection, "users", defaultLoginSession.ID, columns);

                    if (results.Count != 1) throw new Exception("There should be only one user returning the book");

                    User user = new User(results[0][0], results[0][1], results[0][2]);

                    // remove book from loans
                    List<String> loans = user.getLoans();

                    foreach (String id in loans)
                    {
                        // find the book matching the id and find how many are available
                        List<String> bookSearchColumns = new List<String>();
                        bookSearchColumns.Add("ID");

                        List<List<String>> searchResults = new List<List<String>>();
                        searchResults = dbi.searchDatabaseRows(dbconnection, "books", id, bookSearchColumns);

                        // if there are other than one matching id, something is wrong (either the id is too short or no matches at all)
                        if (searchResults.Count != 1)
                        {
                            MessageBox.Show(String.Format("Error: An error occured while trying to return the book with the id {0}. Please contact the library administrator.", id));
                            showHazardNotification("Aborting returning books.");
                            return;
                        }

                        // update the number of specified books available
                        Int32 newAvailable = Int32.Parse(searchResults[0][(int)Book.columnID.Available]) + 1;

                        // change availability to false in book database table
                        dbi.commitDbChanges(dbconnection, "books", newAvailable.ToString(), id, "Available");
                    }

                    // parse loans to csv
                    String loans_csv = "";

                    // update user table entry for loans
                    dbi.commitDbChanges(dbconnection, "users", loans_csv, defaultLoginSession.ID, "Loans");

                    // update book displayed info
                    availability.Text = Properties.Resources.ResourceManager.GetString("bookNotAvailable", TranslationSource.Instance.CurrentCulture);
                    loanButton.IsEnabled = false;
                    //showHazardNotification("Books returned successfully!");
                    updateLoansListbox();
                }
            }
            else
            {
                //user answer no
                return;
            }      
        }

        private void returnOtherButton_Click(object sender, RoutedEventArgs e)
        {
            //this is obsolete
            //quickReturnButton_Click(sender, e);
        }


        //The function below is obsolete since quick return has been removed
        private void OKQuickReturnButton_Click(object sender, RoutedEventArgs e)
        {
            String ID = loanReturnBox.Text;
            if (ID.Length != 6)
            {
                showHazardNotification("The book ID entered doesn't seem valid.");
            }
            else
            {
                // determine if the user is lying
                foreach (Book book in LoansListBox.Items)
                {
                    if (book.id == ID)
                    {
                        showHazardNotification("This book is listed you loans window. Please return the from there instead.");
                        return;
                    }
                }

                // Find out which user has borrowed the book
                List<string> columns = new List<string>();
                columns.Add("ID");
                List<List<string>> results = new List<List<string>>();
                results = dbi.searchExactDatabaseRows(dbconnection, "users", defaultLoginSession.ID, columns);

                // an user with this loan was found, return the book and remove it from the users loans
                if (results.Count == 1)
                {
                    User user = new User(results[0][0], results[0][1], results[0][2]);

                    // remove book from loans
                    List<String> loans = user.getLoans();
                    loans.Remove(ID);

                    // change availability to TRUE in book database table
                    dbi.commitDbChanges(dbconnection, "books", "TRUE", ID, "Available");

                    // parse loans to csv
                    String loans_csv = "";
                    // if there are more than zero books remaining
                    if (loans.Count > 0)
                    {
                        loans_csv = loans[0];
                        for (int i = 1; i < loans.Count; i++)
                        {
                            loans_csv += String.Format(";{0}", loans[i]);
                        }
                    }

                    // update user table entry for loans
                    dbi.commitDbChanges(dbconnection, "users", loans_csv, defaultLoginSession.ID, "Loans");    
                }
                // no users have loaned this book, change its Available to TRUE in the books database
                else if (results.Count == 0)
                {
                    // determine if the book exists
                    columns.Add("ID");
                    List<List<string>> bookResults = new List<List<string>>();
                    results = dbi.searchExactDatabaseRows(dbconnection, "users", defaultLoginSession.ID, columns);
                    if (results.Count == 0)
                    {
                        showHazardNotification("Error: There is no book in the database with the ID you entered.");
                    }
                    else if (results.Count > 1)
                    {
                        showHazardNotification("Error: There are multiple books with this ID in the library. Please contact the library administrator.");
                    }

                    // change availability to TRUE in book database table
                    dbi.commitDbChanges(dbconnection, "books", "TRUE", ID, "Available");
                }

                Storyboard HideQuickReturnView = this.FindResource("HideQuickReturnView") as Storyboard;
                HideQuickReturnView.Begin();

                showHazardNotification("Book returned successfully!");
                loanReturnBox.Text = "";
            }
        }

        private void promptYesButton_Click(object sender, RoutedEventArgs e)
        {
            yesNoPromptAnswer = 1;
            YesNoPromptText.Text = "";
            Storyboard HideYesNoPrompt = this.FindResource("HideYesNoPrompt") as Storyboard;
            HideYesNoPrompt.Begin();
        }

        private void promptNoButton_Click(object sender, RoutedEventArgs e)
        {
            yesNoPromptAnswer = 2;
            YesNoPromptText.Text = "";
            Storyboard HideYesNoPrompt = this.FindResource("HideYesNoPrompt") as Storyboard;
            HideYesNoPrompt.Begin();
        }
    }
}


