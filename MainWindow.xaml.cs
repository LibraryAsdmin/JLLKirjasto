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

        public string this [string key]
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

    public class LocExtension
        : Binding
    {
        public LocExtension(string name)
            : base("[" + name + "]")
        {
            this.Mode = BindingMode.OneWay;
            this.Source = TranslationSource.Instance;
        }
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TranslateTransform middleFlagTransform;
        private TranslateTransform bottomFlagTransform;
        private Storyboard searchStoryboard;
        private Storyboard gradientStoryboard;

        public MainWindow()
        {
            InitializeComponent();

            //changes the flag to correspond with the system culture
            string culture = CultureInfo.CurrentUICulture.ToString();
            changeUILanguage(culture);

            // Initializes flag transformations
            middleFlagTransform = new TranslateTransform(0, 0);
            bottomFlagTransform = new TranslateTransform(0, 0);
            searchStoryboard = new Storyboard();
            gradientStoryboard = new Storyboard();
            DoubleAnimation searchButtonRectangulation = new DoubleAnimation(0,TimeSpan.FromSeconds(0.5));
            DoubleAnimation searchButtonHeightDecrease = new DoubleAnimation(50, TimeSpan.FromSeconds(1));
            
            PointAnimation gradientTurn = new PointAnimation(new Point(0.2, 1),new Point(0.8, 1),TimeSpan.FromSeconds(10));
            searchStoryboard.Children.Add(searchButtonRectangulation);
            searchStoryboard.Children.Add(searchButtonHeightDecrease);
            gradientStoryboard.Children.Add(gradientTurn);
            gradientStoryboard.RepeatBehavior = RepeatBehavior.Forever;
            gradientStoryboard.AutoReverse = true;
            searchStoryboard.DecelerationRatio = 0.7;
            gradientStoryboard.DecelerationRatio = 0.1;
            gradientStoryboard.AccelerationRatio = 0.1;
            Storyboard.SetTarget(searchButtonRectangulation, searchButton);
            Storyboard.SetTarget(searchButtonHeightDecrease, searchButton);
            Storyboard.SetTargetProperty(searchButtonRectangulation, new PropertyPath(Rectangle.RadiusXProperty));
            Storyboard.SetTargetProperty(searchButtonHeightDecrease, new PropertyPath(Rectangle.HeightProperty));
            Storyboard.SetTarget(gradientTurn, WindowGrid);
            Storyboard.SetTargetProperty(gradientTurn, new PropertyPath ("Background.EndPoint"));
            gradientStoryboard.Begin();
            searchStoryboard.Completed += new EventHandler(searchButtonAnimationCompleted);
        }

        void searchButtonAnimationCompleted (object sender, EventArgs e)
        {
            this.searchBox.Visibility = Visibility.Visible;
            DoubleAnimation searchBoxOpacity = new DoubleAnimation(1.0, TimeSpan.FromSeconds(0.3));
            Storyboard searchFadeIn = new Storyboard();
            searchFadeIn.Children.Add(searchBoxOpacity);
            Storyboard.SetTarget(searchFadeIn, searchBox);
            Storyboard.SetTargetProperty(searchFadeIn, new PropertyPath(TextBox.OpacityProperty));
            searchFadeIn.DecelerationRatio = 0.7;
            searchFadeIn.Begin();
            Canvas.SetZIndex(searchBox, 1);
            Canvas.SetZIndex(searchButton, 0);
            searchBox.SelectAll();
        }
           
        void changeUILanguage(string language)
        {
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
        }


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

        private void LanguageGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            // Show languages when mouse enters the LanguageGrid
            showLanguages(middleFlagTransform.Y, bottomFlagTransform.Y);         
        }

        private void LanguageGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            hideLanguages(middleFlagTransform.Y, bottomFlagTransform.Y);
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
            middleFlagTransform = new TranslateTransform(0,middleY);
            DoubleAnimation anim = new DoubleAnimation(0, duration);
            middleFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim);
            Swedish.RenderTransform = middleFlagTransform;

            // transform the bottom flag from its current positions to the default position
            bottomFlagTransform = new TranslateTransform(0,bottomY);
            DoubleAnimation anim2 = new DoubleAnimation(0, duration);
            bottomFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim2);
            English.RenderTransform = bottomFlagTransform;
        }

        private void searchButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            searchStoryboard.Begin(this);   
        }

        private void searchBox_KeyUp(object sender, KeyEventArgs e)
        {
            //if(firstTime)
            GeneralTransform transformButton = searchButton.TransformToAncestor(this);
            GeneralTransform transformBox = searchBox.TransformToAncestor(this);
            StartPageContentGrid.Children.Remove(searchButton);
            StartPageContentGrid.Children.Remove(searchBox);
            WindowGrid.Children.Add(searchButton);
            WindowGrid.Children.Add(searchBox);
            Point whereToTransformButton = transformButton.Transform(new Point(0, 0));
            TranslateTransform tt1 = new TranslateTransform(whereToTransformButton.X, whereToTransformButton.Y);
            searchButton.RenderTransform = tt1;
            searchButton.VerticalAlignment = VerticalAlignment.Top;
            searchButton.HorizontalAlignment = HorizontalAlignment.Left;
            searchButton.Margin = new Thickness(0);
            Point whereToTransformBox = transformBox.Transform(new Point(0, 0));
            TranslateTransform tt2 = new TranslateTransform(whereToTransformBox.X, whereToTransformBox.Y);
            searchBox.RenderTransform = tt2;
            searchBox.VerticalAlignment = VerticalAlignment.Top;
            searchBox.HorizontalAlignment = HorizontalAlignment.Left;
            searchBox.Margin = new Thickness(0);



        }
    }

}
