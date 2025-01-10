using System;
using System.Windows;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.UI.Common.ViewModels;
using Missionware.Cognibase.UI.Wpf;
using Missionware.Cognibase.UI.Wpf.Client;
using Missionware.SharedLib.UI.Wpf.Lib;

using PingerDomain.Entities;

using PingerUiCommon.ViewModels;

namespace PingerWpfApp
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WpfStartupHelper _startupHelper;
        private AsyncWpfDialog _dialog;
        private readonly MainViewModel _vm;

        public static MainWindow MainWindowFactory()
        {
            return new MainWindow();
        }

        public MainWindow()
        {
            InitializeComponent();

            _dialog = new AsyncWpfDialog();
            _vm = new MainViewModel(App.Client, _dialog);
            DataContext = _vm;
            menuAdd.Click += MenuAdd_Click;
            menuEdit.Click += MenuEdit_Click;
        }

        public static WpfApplication App { get; set; }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            App.Client?.Dispose();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            _startupHelper = new WpfStartupHelper(this, App.Client);
            _startupHelper.AuthVm = new SimpleAuthDialogVm { DomainFullName = "Basic", Username = "user1", Password = "user1" };
            _startupHelper.QuitAction = () => Close();
            _startupHelper.DataLoadAction = () =>
            {
                // read devices
                DataItemCollection<Device> collection = App.Client.ReadDataItemCollection<Device>();

                // set data source in main thread
                Dispatcher.Invoke(() =>
                {
                    _vm.Devices = collection;
                });
            };

            _startupHelper.ShowAuthDialog();
        }


        private void MenuEdit_Click(object? sender, RoutedEventArgs e)
        {
            // call edit form
            if (_vm.SelectedDevice != null)
            {
                var devVm = new DeviceEditVm(App.Client, _vm.SelectedDevice, _dialog);
                var devView = new DeviceEditWindow();
                devVm.CloseAction = devView.Close;
                devView.SetupView(devVm);
                devView.ShowDialog();
            }
            else
            {
                _dialog.ShowMessage("Info", "Nothing is selected");
            }
        }

        private void MenuAdd_Click(object? sender, RoutedEventArgs e)
        {
            var devVm = new DeviceEditVm(App.Client, null, _dialog);
            var devView = new DeviceEditWindow();
            devVm.CloseAction = devView.Close;
            devView.SetupView(devVm);
            devView.ShowDialog();
        }
    }
}