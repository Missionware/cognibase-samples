using System.Threading.Tasks;

namespace PingerUiCommon
{
    public interface IDialogService
    {
        Task ShowMessage(string title, string message);
        Task ShowError(string title, string message);
        Task<DialogResult> AskConfirmation(string title, string message);
    }

    public enum DialogResult
    {
        Confirmed,
        NotConfirmed
    }
}