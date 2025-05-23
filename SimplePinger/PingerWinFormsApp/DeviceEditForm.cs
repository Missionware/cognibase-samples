using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using PingerDomain.Entities;

namespace PingerWinFormsApp
{
    public partial class DeviceEditForm : Form
    {
        private IClient _com;

        // Data
        private Device _DataItem;

        public DeviceEditForm()
        {
            InitializeComponent();
        }

        // setup the edit: add or edit
        public void SetupEdit(IClient com, Device dataItem)
        {
            // cache client object manager
            _com = com;

            // if dataitem is not null then is edit
            if (dataItem != null)
            {
                // set
                _DataItem = dataItem;

                // hide
                txtId.Enabled = false;
            }
            else // else it is add
            {
                // create data item in object manager (important)
                _DataItem = com.CreateDataItem<Device>();
                _DataItem.Result = com.CreateDataItem<DevicePingResult>();

                // hide
                txtId.Visible = false;
                lblId.Visible = false;
            }

            // set to datasource
            bsDevice.DataSource = _DataItem;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // save
            ClientTransactionInfo? saveResult = _com.Save();

            // if success close form
            if (saveResult.WasSuccessful)
                Close();
            else // else show message
                MessageBox.Show(@"Could not save data. Try again or cancel edit.");
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // check changed
            if (_DataItem.IsChanged)
            {
                // confirm
                DialogResult result = MessageBox.Show(
                    "There are pending changes. Do you want to cancel these edits and exit?",
                    "Cancel & Exit?", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    // if cancel reset and close
                    _com.ResetAllMonitoredItems();
                    Close();
                }
            }
            else
            {
                // just close
                Close();
            }
        }

        private void DeviceEditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_DataItem.IsNew && _DataItem.IsChanged)
            {
                // confirm
                DialogResult result = MessageBox.Show(
                    "There are pending changes. Do you want to cancel these edits and exit?",
                    "Cancel & Exit?", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                    // undo changes
                    _com.ResetAllMonitoredItems();
                else // cancel closing
                    e.Cancel = true;
            }
        }
    }
}