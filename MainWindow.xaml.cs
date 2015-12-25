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

namespace JLLKirjasto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TranslateTransform middleFlagTransform;
        private TranslateTransform bottomFlagTransform;

        public MainWindow()
        {
            InitializeComponent();

        }

        // Change language to English
        private void English_Click(object sender, RoutedEventArgs e)
        {
            // Move English flag to front
            Canvas.SetZIndex(Swedish, 0);
            Canvas.SetZIndex(Finnish, 1);
            Canvas.SetZIndex(English, 2);
            // Change language
            setEnglish();
        }

        private void setEnglish()
        {
            // Change messages to English
            Greeting.Text = "Greetings!";
        }

        // Change language to Swedish
        private void Swedish_Click(object sender, RoutedEventArgs e)
        {
            // Move Swedish to front
            Canvas.SetZIndex(Swedish, 2);
            Canvas.SetZIndex(Finnish, 1);
            Canvas.SetZIndex(English, 0);
            // Change language
            setSwedish();
        }

        private void setSwedish()
        {
            // Change messages to Swedish
            Greeting.Text = "Hejsan!";
        }

        // Change language to Finnish (default)
        private void Finnish_Click(object sender, RoutedEventArgs e)
        {
            // Move Swedish to front
            Canvas.SetZIndex(Swedish, 1);
            Canvas.SetZIndex(Finnish, 2);
            Canvas.SetZIndex(English, 0);
            // Change language
            setFinnish();
        }

        private void setFinnish()
        {
            // Change messages to Finnish
            Greeting.Text = "Päivää!";
        }

        private void LanguageGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            // Show languages when mouse enters the LanguageGrid
            showLanguages();         
        }

        private void LanguageGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            hideLanguages(middleFlagTransform.Y, bottomFlagTransform.Y);
        }

        private void showLanguages()
        {
            Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 250));

            middleFlagTransform = new TranslateTransform(0, 0);
            DoubleAnimation anim = new DoubleAnimation(40, duration);
            middleFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim);
            Swedish.RenderTransform = middleFlagTransform;

            bottomFlagTransform = new TranslateTransform(0, 0);
            DoubleAnimation anim2 = new DoubleAnimation(80, duration);
            bottomFlagTransform.BeginAnimation(TranslateTransform.YProperty, anim2);
            English.RenderTransform = bottomFlagTransform;
        }

        private void hideLanguages(double middleY, double bottomY)
        {
            var T = new TranslateTransform(0,middleY);
            Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 250));
            DoubleAnimation anim = new DoubleAnimation(0, duration);
            T.BeginAnimation(TranslateTransform.YProperty, anim);
            Swedish.RenderTransform = T;

            var T2 = new TranslateTransform(0,bottomY);
            DoubleAnimation anim2 = new DoubleAnimation(0, duration);
            T2.BeginAnimation(TranslateTransform.YProperty, anim2);
            English.RenderTransform = T2;
        }
    }
}
