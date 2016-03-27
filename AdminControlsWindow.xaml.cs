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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SQLite;

namespace JLLKirjasto
{
    /// <summary>
    /// Interaction logic for AdminControlsWindow.xaml
    /// </summary>
    public partial class AdminControlsWindow : Window
    {

        public AdminControlsWindow()
        {
            InitializeComponent();        
        }

        private void BooksSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update Books datagrid
        }

        private void UsersSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update users datagrid
        }
    }
}
