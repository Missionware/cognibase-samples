using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

namespace ChatDesktopApp.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();

            itemsPanel.PropertyChanged += ItemsPanel_PropertyChanged; ;
        }

        private void ItemsPanel_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property.Name == nameof(ListBox.ItemCount))
            {
                if(itemsPanel.ItemCount >=1 )
                    itemsPanel.SelectedIndex = itemsPanel.ItemCount - 1;
                // Scroll to the end when the height changes
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    scrollPanel.ScrollToEnd();
                });
            }
        }

        private void ItemsPanel_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                scrollPanel.ScrollToEnd();
            }
        }

        private void TextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                // Prevent the default behavior of the Enter key
                e.Handled = true;
                
                var vm = (DataContext as ViewModels.MainViewModel);
                if (vm != null)
                {
                    // Trigger the send message command
                    vm.SendMessageCommand.Execute();
                }
            }
        }
    }
}
