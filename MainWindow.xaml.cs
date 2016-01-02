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
        private Storyboard myStoryboard;

        public MainWindow()
        {
            InitializeComponent();

            //TODO: Here, the culture settings of the system could be read and the language of the application set accordingly

            // Initializes flag transformations
            middleFlagTransform = new TranslateTransform(0, 0);
            bottomFlagTransform = new TranslateTransform(0, 0);
            myStoryboard = new Storyboard();
            DoubleAnimation searchButtonRectangulation = new DoubleAnimation(0,TimeSpan.FromSeconds(0.2));
            myStoryboard.Children.Add(searchButtonRectangulation);
            myStoryboard.AutoReverse = true;
            searchButtonRectangulation.DecelerationRatio = 0.7;
            Storyboard.SetTargetName(searchButtonRectangulation,searchButton.Name);
            Storyboard.SetTargetProperty(searchButtonRectangulation, new PropertyPath(Rectangle.RadiusXProperty));
            

        }

        // Change language to English
        private void English_Click(object sender, RoutedEventArgs e)
        {
            // Move English flag to front
            Canvas.SetZIndex(Swedish, 0);
            Canvas.SetZIndex(Finnish, 1);
            Canvas.SetZIndex(English, 2);
            // Change language 
            TranslationSource.Instance.CurrentCulture = new System.Globalization.CultureInfo("en-GB");
        }

        // Change language to Swedish
        private void Swedish_Click(object sender, RoutedEventArgs e)
        {
            // Move Swedish to front
            Canvas.SetZIndex(Swedish, 2);
            Canvas.SetZIndex(Finnish, 1);
            Canvas.SetZIndex(English, 0);
            // Change language
            TranslationSource.Instance.CurrentCulture = new System.Globalization.CultureInfo("sv-SE");
        }

        // Change language to Finnish (default)
        private void Finnish_Click(object sender, RoutedEventArgs e)
        {
            // Move Swedish to front
            Canvas.SetZIndex(Swedish, 1);
            Canvas.SetZIndex(Finnish, 2);
            Canvas.SetZIndex(English, 0);
            // Change language
            TranslationSource.Instance.CurrentCulture = new System.Globalization.CultureInfo("fi-FI");
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
            Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 250));

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
            myStoryboard.Begin(this);   
        }
    }

}
