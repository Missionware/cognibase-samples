using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using PingerDomain.Entities;

namespace PingerUiCommon.ViewModels
{
    public class DeviceEditVm : ObservableObject
    {
        private readonly IClient _client;
        private readonly IDialogService _dialogService;

        public DeviceEditVm(IClient client, Device device, IDialogService dialogService)
        {
            _dialogService = dialogService;
            SaveCommand = new AsyncRelayCommand(Save);
            CancelCommand = new AsyncRelayCommand(Cancel);
            _client = client;
            Device = device;
            if (Device == null)
            {
                Device = _client.CreateDataItem<Device>();
                Device.Result = _client.CreateDataItem<DevicePingResult>();
                IsNew = true;
            }

            IsEdit = !IsNew;
        }

        public Device Device { get; set; }
        public bool IsEdit { get; set; }
        public bool IsNew { get; set; }
        public ClientTxnInfo SaveResult { get; set; }
        public Action CloseAction { get; set; }


        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand CancelCommand { get; }

        private async Task Cancel()
        {
            // check changed
            if (Device != null && Device.IsChanged)
            {
                // confirm
                DialogResult result = await _dialogService.AskConfirmation("Cancel & Exit?",
                    "There are pending changes. Do you want to cancel these edits and exit?");
                if (result == DialogResult.Confirmed)
                {
                    // if cancel reset and close
                    _client.ResetAllMonitoredItems();
                    Device = null;
                    CloseAction();
                }
            }
            else
            {
                // just close
                CloseAction();
            }
        }


        private async Task Save()
        {
            SaveResult = await _client.SaveAsync(Device);

            // if success close form
            if (SaveResult.WasSuccessfull)
                CloseAction();
            else // else show message
                await _dialogService.ShowError("Error", "Could not save data. Try again or cancel edit.");
        }

        public async Task<bool> HandleWindowClosing()
        {
            if (Device != null && Device.IsChanged)
            {
                // confirm
                DialogResult result = await _dialogService.AskConfirmation("Cancel & Exit?",
                    "There are pending changes. Do you want to cancel these edits and exit?");
                if (result == DialogResult.Confirmed)
                    // undo changes
                    _client.ResetAllMonitoredItems();
                else // cancel closing
                    return false;
            }

            return true;
        }
    }
}