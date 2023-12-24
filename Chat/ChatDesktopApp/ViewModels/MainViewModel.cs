using System;
using System.Threading.Tasks;

using Missionware.Cognibase.Library;
using Missionware.SharedLib.UI;

using ReactiveUI;

namespace ChatDesktopApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        IClient _client;
        private readonly IAsyncDialogService _dialogService;

        public string Greeting => "Welcome to Cognibase!";

        public MainViewModel() { }

        public MainViewModel(IClient client, IAsyncDialogService dialogService)
        {
            _client = client;
            _dialogService = dialogService;
        }
    }


}
