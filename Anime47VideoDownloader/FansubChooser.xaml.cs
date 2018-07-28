using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
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
    /// Interaction logic for FansubChooser.xaml
    /// </summary>
    public partial class FansubChooser : Window
    {
        public event ClickFansubHandler ClickFansub;
        public string[] Fansub { get; set; }
        public bool IsClicked { get; set; }

        public FansubChooser(string[] fansub)
        {
            InitializeComponent();
            Closing += NotifyClosing;
            Fansub = fansub;
            IsClicked = false;
            GenerateControlsForFansub();
        }

        private void NotifyClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsClicked)
                return;
            MessageBoxResult result = MessageBox.Show("Please Choose a Fansub to process, If want to quit, click yes, otherwise click No", "Please Choose a Fansub to process", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                Environment.Exit(0);
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void GenerateControlsForFansub()
        {
            for(int i = 0; i < Fansub.Length; i++)
            {
                fansubContainer.ColumnDefinitions.Add(new ColumnDefinition());
                Button btn = new Button();
                btn.Content = Fansub[i];
                btn.Tag = i;
                btn.Click += (s, e) => 
                {
                    IsClicked = true;
                    OnClickFansub(((Button)s).Tag.ToString());
                };
                fansubContainer.Children.Add(btn);
                Grid.SetColumn(btn, i);
            }
        }

        private void OnClickFansub(string message)
        {
            ClickFansub?.Invoke(this, new ResultEventArgs(message));
        }
    }
    public delegate void ClickFansubHandler(object sender, ResultEventArgs e);
    public class ResultEventArgs : EventArgs
    {
        public object Result { get; set; }
        public ResultEventArgs(object e)
        {
            Result = e;
        }
    }
}
