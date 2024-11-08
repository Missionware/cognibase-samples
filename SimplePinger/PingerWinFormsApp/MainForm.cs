using System.Collections.ObjectModel;

using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.Cognibase.UI.WinForms.Client;

using PingerDomain.Entities;

using Missionware.SharedLib;

using PingerWinFormsApp.Properties;

using SkiaSharp;

using Timer = System.Windows.Forms.Timer;

namespace PingerWinFormsApp
{
    // the main form
    public partial class MainForm : Form
    {
        private readonly Image _errorImg;
        private bool _isAborted;
        private readonly Image _successImg;
        private readonly Timer _timer = new();

        // Data
        private readonly Image _unknownImg;
        private Axis xAxis;
        private Axis yAxis;

        public MainForm()
        {
            InitializeComponent();

            dataGridView1.DataError += DataGridView1_DataError;

            // map values to PingHistoryItem dataitem properties
            LiveCharts.Configure(config => config.HasMap<PingHistoryItem>((histItem, point) =>
            {
                // map the value
                point.PrimaryValue = (float)histItem.Value;
                // convert to file time and map
                point.SecondaryValue = histItem.Time.ToFileTime();
            }));

            // setup chart
            SetupChart();
            chart.Series = Series;
            chart.YAxes = YAxes;
            chart.XAxes = XAxes;

            // setup images
            _unknownImg = (Image)Resources.info_empty.Clone();
            _successImg = (Image)Resources.check_circle2.Clone();
            _errorImg = (Image)Resources.warning_triangle.Clone();

            // setup timer to refresh once in a while for unknow state
            _timer.Tick += (o, e) => dataGridView1.Refresh();
            _timer.Interval = 10000;
            _timer.Start();
        }

        // static App instance to provide easy acces to Client Object Manager
        public static WinFormsApplication App { get; set; }

        // Collection for the chart
        public ObservableCollection<ISeries> Series { get; set; } = new();
        public ObservableCollection<ICartesianAxis> XAxes { get; } = new();
        public ObservableCollection<ICartesianAxis> YAxes { get; } = new();

        private void onDispose()
        {
            if (_timer != null)
                _timer.Dispose();
        }

        private void DataGridView1_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            if (e != null && e.Exception is IndexOutOfRangeException)
                e.Cancel = true;
        }

        // handle adding device
        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new DeviceEditForm();
            frm.SetupEdit(App.Client, null);
            frm.ShowDialog();
        }

        // handle edit device
        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // get current
            var currentObject = dataGridView1.CurrentRow.DataBoundItem as Device;

            // call edit form
            if (currentObject != null)
            {
                var frm = new DeviceEditForm();
                frm.SetupEdit(App.Client, currentObject);
                frm.ShowDialog();
            }
            else
            {
                MessageBox.Show("Nothing is selected");
            }
        }

        // handle delete device
        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // get current
            var currentObject = dataGridView1.CurrentRow.DataBoundItem as Device;

            if (currentObject != null)
            {
                // confirm deletion
                DialogResult result = MessageBox.Show($"You are about to delete the Device {currentObject.Name}. Do you want to Proceed?", "Delete Item?", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                    return;

                // mark for deletion
                currentObject.MarkForDeletion();

                // save
                ClientTxnInfo? saveResult = App.Client.Save();

                // if not success unmark 
                if (!saveResult.WasSuccessfull)
                {
                    MessageBox.Show("Could not delete Item.");
                    currentObject.UnMarkForDeletion();
                }
            }
            else
            {
                // log
                MessageBox.Show("Nothing is selected");
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // on load create the loading form (to be affiliated with this form)
            var frm = new LoaderForm() { Owner = this };

            // in background thread: open the form, load and then close the form
            Task.Factory.StartNew(() =>
            {
                loadData(frm);
            });
        }

        // load data in background thread
        private void loadData(LoaderForm frm)
        {
            // show form (invoke in main thread)
            this.InvokeIfRequired<Control>(o =>
            {
                frm.Show();
            });

            // loop
            while (!_isAborted)
            {
                // wait for the loader to be visible
                if (!frm.Visible)
                {
                    Thread.Sleep(100);
                    continue;
                }

                try
                {
                    // Check if we are connected
                    if (!App.Client.CanProvideData)
                    {
                        // not connected
                        Thread.Sleep(1000);
                        continue;
                    }

                    // read devices
                    DataItemCollection<Device>? collection = App.Client.ReadDataItemCollection<Device>();

                    // set data source (invoke in main thread)
                    this.InvokeIfRequired<Control>(o =>
                    {
                        bsDevices.DataSource = collection;
                    });
                }
                catch (Exception)
                {
                    // error 
                    Thread.Sleep(1000);
                    continue;
                }

                // close form (invoke in main thread)
                this.InvokeIfRequired<Control>(o =>
                {
                    frm.Close();
                });

                break;
            }
        }

        // perform the formatting using the images
        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == resultDataGridViewTextBoxColumn.Index)
            {
                // unknown
                if (e.Value == null || (int)e.Value == 0)
                {
                    e.Value = _unknownImg;
                    return;
                }

                // success
                if ((int)e.Value == 2)
                    e.Value = _successImg;
                else // error
                    e.Value = _errorImg;
            }
        }

        // here we setup the livecharts2 chart
        public void SetupChart()
        {
            yAxis = new Axis
            {
                MinLimit = 0,
                MaxLimit = 10,
                ForceStepToMin = true,
                MinStep = 3,
                TextSize = 14,
                NameTextSize = 10,
                LabelsPaint = new SolidColorPaint { Color = SKColors.LightSlateGray },
                Labeler = value =>
                {
                    if (value == 0)
                        return "Unknown";
                    if (value == 1)
                        return "Down";
                    if (value == 2)
                        return "Up";
                    return "";
                }
            };
            YAxes.Add(yAxis);
            yAxis.MaxLimit = 2.5;
            yAxis.MinLimit = -0.5;
            yAxis.MinStep = 1;

            xAxis = new Axis
            {
                Name = "DateTime",
                UnitWidth = 0.1,
                NameTextSize = 10,
                TextSize = 12,
                LabelsRotation = 20,
                LabelsPaint = new SolidColorPaint { Color = SKColors.LightSlateGray },
                NamePaint = new SolidColorPaint { Color = SKColors.White },
                Labeler = value =>
                {
                    var dt = DateTime.FromFileTime((long)value);
                    return dt.ToString();
                }
            };
            XAxes.Add(xAxis);

            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<PingHistoryItem>
                {
                    LineSmoothness = 0,
                    Fill = null,
                    GeometrySize = 3.0,
                    GeometryStroke = new SolidColorPaint(SKColors.Gray),
                    Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 3 },
                    TooltipLabelFormatter = chartPoint =>
                    {
                        string str = "Unknown";
                        if (chartPoint.PrimaryValue == 1)
                            str = "Down";
                        else if (chartPoint.PrimaryValue == 2)
                            str = "Up";

                        var dt = DateTime.FromFileTime((long)chartPoint.SecondaryValue);
                        return $"{str}{Environment.NewLine}{dt.ToString()}";
                    }
                }
            };
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            // get selected
            var currentObject = dataGridView1.CurrentRow?.DataBoundItem as Device;

            if (currentObject != null)
                try
                {
                    // vet validator for this device
                    IDataItemCollectionValidator<PingHistoryItem> validator = getHistoryQuery(currentObject.Id);

                    // read live collection and apply to chart
                    DataItemCollection<PingHistoryItem>? list = App.Client.ReadDataItemCollection(validator);
                    Series[0].Values = list;
                }
                catch (Exception ex)
                {
                    // show error
                    MessageBox.Show($"Error: {ex.Message}");
                }
        }

        // on close clear everything
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            dataGridView1.CellFormatting -= dataGridView1_CellFormatting;
            dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;
            bsDevices.DataSource = null;
            _isAborted = true;
            App.Client?.Dispose();
            base.OnFormClosed(e);
        }

        // get history query for device
        private IDataItemCollectionValidator<PingHistoryItem> getHistoryQuery(long deviceId)
        {
            // get field
            DataItemField? timeField = DataItem.GetClassInfo(typeof(PingHistoryItem)).GetField(nameof(PingHistoryItem.DeviceId));

            // create where clayse
            var whereActiveExpr = new WhereComparisonExpression(timeField, WhereOperator.Equal, deviceId);

            // create vaidator and return
            var deletionValidator = new DataItemCollectionValidator<PingHistoryItem>(true) as IDataItemCollectionValidator<PingHistoryItem>;
            deletionValidator.QueryExpression = whereActiveExpr;
            deletionValidator.IsForceLoader = true;
            return deletionValidator;
        }
    }
}