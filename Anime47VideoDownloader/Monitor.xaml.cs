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

namespace Anime47VideoDownloader
{
    /// <summary>
    /// Interaction logic for Monitor.xaml
    /// </summary>
    public partial class Monitor : Window
    {
        public Monitor()
        {
            InitializeComponent();
            Closing += PreventDispose;
        }

        private void PreventDispose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBox.Show("This will not close the monitor, it just hide");
            e.Cancel = true;
        }

        //private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (Verfity.Text == "25082003 Anime47 :)")
        //        ((Grid)sender).Visibility = Visibility.Collapsed;
        //}
    }
}
