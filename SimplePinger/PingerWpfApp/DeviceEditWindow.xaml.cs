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

using Google.Protobuf.WellKnownTypes;

using PingerUiCommon.ViewModels;

namespace PingerWpfApp
{
    /// <summary>
    /// Interaction logic for DeviceEditWindow.xaml
    /// </summary>
    public partial class DeviceEditWindow : Window
    {
        private DeviceEditVm _vm;
        public DeviceEditWindow()
        {
            InitializeComponent();
            this.Closing += DeviceEditWindow_Closing;
        }

        private async void DeviceEditWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            await Dispatcher.InvokeAsync(async () =>
            {
                await checkAndClose().ConfigureAwait(true);
            });
        }

        private async Task checkAndClose()
        {
            bool close = await _vm.HandleWindowClosing().ConfigureAwait(true);
            if (close)
            {
                // detach the event handler
                Closing -= DeviceEditWindow_Closing;

                //...and close the window immediately
                Close();
            }
        }

        internal void SetupView(DeviceEditVm devVm)
        {
            DataContext = devVm;
            _vm = devVm;
        }


        private void Increment_Click(object sender, RoutedEventArgs e)
        {
            _vm.Device.PingInterval++;
        }

        private void Decrement_Click(object sender, RoutedEventArgs e)
        {
            if(_vm.Device.PingInterval >= 2)
                _vm.Device.PingInterval--;
        }
    }
}
