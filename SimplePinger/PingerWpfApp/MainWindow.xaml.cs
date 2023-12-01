using System.Windows;

using Missionware.Cognibase.UI.Wpf.Client;

namespace PingerApp
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public static WpfApplication App { get; set; }
    }
}