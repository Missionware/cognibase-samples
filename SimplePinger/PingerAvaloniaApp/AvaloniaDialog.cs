using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;

using PingerUiCommon;

namespace PingerAvaloniaApp
{
    public class AvaloniaDialog : IDialogService
    {
        public async Task<DialogResult> AskConfirmation(string title, string message)
        {
            Window? window = getParentWindow();

            IMsBox<ButtonResult>? messageBoxStandardWindow =
                MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.YesNo);

            ButtonResult result = ButtonResult.None;

            if (window != null)
                result = await messageBoxStandardWindow.ShowWindowDialogAsync(window);
            else
                result = await messageBoxStandardWindow.ShowAsync();

            if (result == ButtonResult.Yes)
                return DialogResult.Confirmed;

            return DialogResult.NotConfirmed;
        }

        public async Task ShowError(string title, string message)
        {
            Window? window = getParentWindow();

            IMsBox<ButtonResult>? messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(title, message);

            if (window != null)
                await messageBoxStandardWindow.ShowWindowDialogAsync(window);
            else
                await messageBoxStandardWindow.ShowAsync();
        }

        public async Task ShowMessage(string title, string message)
        {
            Window? window = getParentWindow();

            IMsBox<ButtonResult>? messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(title, message);
            if (window != null)
                await messageBoxStandardWindow.ShowWindowDialogAsync(window);
            else
                await messageBoxStandardWindow.ShowAsync();
        }

        private Window getParentWindow()
        {
            Window retWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                IReadOnlyList<Window> windows =
                    ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).Windows;
                foreach (Window window in windows)
                {
                    retWindow = window;
                    if (window.IsActive)
                        break;
                }
            }

            return retWindow;
        }
    }
}