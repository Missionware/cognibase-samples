using Missionware.SharedLib.UI;

using PingerUiCommon;

namespace PingerMauiApp
{
    public class MauiDialog : IAsyncDialogService
    {
        public async Task<AsyncDialogResult> AskConfirmation(string title, string message)
        {
            bool result = await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No")
                .ConfigureAwait(false);

            return result ? AsyncDialogResult.Confirmed : AsyncDialogResult.NotConfirmed;
        }

        public async Task ShowError(string title, string message)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK").ConfigureAwait(false);
        }

        public async Task ShowMessage(string title, string message)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK").ConfigureAwait(false);
        }
    }
}