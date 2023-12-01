using PingerUiCommon;

namespace PingerMauiApp
{
    public class MauiDialog : IDialogService
    {
        public async Task<DialogResult> AskConfirmation(string title, string message)
        {
            bool result = await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No")
                .ConfigureAwait(false);

            return result ? DialogResult.Confirmed : DialogResult.NotConfirmed;
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