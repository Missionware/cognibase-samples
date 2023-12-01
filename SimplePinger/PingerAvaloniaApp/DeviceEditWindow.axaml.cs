using System.ComponentModel;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using PingerUiCommon.ViewModels;

namespace PingerAvaloniaApp
{
    public partial class DeviceEditWindow : Window
    {
        private bool _isClosing = false;
        private DeviceEditVm _vm;

        public DeviceEditWindow()
        {
            InitializeComponent();
            Closing += DeviceEditWindow_Closing;
        }

        private void DeviceEditWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Dispatcher.UIThread.Post(async () =>
            {
                await checkAndClose();
            });
        }

        private async Task checkAndClose()
        {
            bool close = await _vm.HandleWindowClosing();
            if (close)
            {
                // detach the event handler
                Closing -= DeviceEditWindow_Closing;

                //...and close the window immediately
                Close();
            }
        }

        public void SetupView(DeviceEditVm vm)
        {
            DataContext = vm;
            _vm = vm;
        }
    }
}