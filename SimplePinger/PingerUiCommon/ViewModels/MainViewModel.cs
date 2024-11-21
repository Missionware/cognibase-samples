using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using Missionware.Cognibase.Client;
using Missionware.Cognibase.Library;
using Missionware.SharedLib.UI;

using PingerDomain.Entities;

using SkiaSharp;

namespace PingerUiCommon.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IClient _client;
        public DataItemCollection<Device> _devices;
        private readonly IAsyncDialogService _dialogService;
        private Device _selectedDevice;
        private ObservableCollection<ISeries> _series = new ObservableCollection<ISeries>();
        private ObservableCollection<ICartesianAxis> _xAxes = new ObservableCollection<ICartesianAxis>();
        private ObservableCollection<ICartesianAxis> _yAxes = new ObservableCollection<ICartesianAxis>();
        private Axis xAxis;
        private Axis yAxis;

        public MainViewModel(IClient client, IAsyncDialogService dialogService)
        {
            PropertyChanged += MainViewModel_PropertyChanged;

            // map values to PingHistoryItem dataitem properties
            LiveCharts.Configure(config =>
                config
                    .HasMap<PingHistoryItem>((histItem, point) =>
                    {
                        // map the value
                        point.PrimaryValue = (float)histItem.Value;
                        // convert to file time and map
                        point.SecondaryValue = histItem.Time.ToFileTime();
                    }));

            SetupChart();

            _client = client;
            _dialogService = dialogService;
            RemoveDeviceCommand = new AsyncRelayCommand(RemoveDevice);
        }

        public IAsyncRelayCommand RemoveDeviceCommand { get; }

        public Device SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        public DataItemCollection<Device> Devices
        {
            get => _devices;
            set => SetProperty(ref _devices, value);
        }

        public ObservableCollection<ISeries> Series { get => _series; set => SetProperty(ref _series, value); }
        public ObservableCollection<ICartesianAxis> XAxes { get => _xAxes; set => SetProperty(ref _xAxes, value); }
        public ObservableCollection<ICartesianAxis> YAxes { get => _yAxes; set => SetProperty(ref _yAxes, value); }

        private async void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedDevice))
            {
                if (SelectedDevice != null)
                    try
                    {
                        // vet validator for this device
                        IDataItemCollectionValidator<PingHistoryItem> validator = getHistoryQuery(SelectedDevice.Id);

                        // read live collection and apply to chart
                        DataItemCollection<PingHistoryItem> list = await _client.ReadDataItemCollectionAsync(validator);
                        Series[0].Values = list;
                    }
                    catch (Exception ex)
                    {
                        // show error
                        await _dialogService.ShowMessage("Info", $"Error: {ex.Message}");
                    }
                else
                    Series[0].Values = new List<PingHistoryItem>();
            }
            else if (e.PropertyName == nameof(Devices) && Devices.Count > 0)
            {
                SelectedDevice = Devices[0];
            }
        }

        private async Task RemoveDevice()
        {
            if (SelectedDevice != null)
            {
                // confirm deletion
                AsyncDialogResult result = await _dialogService.AskConfirmation("Delete Item?",
                    $"You are about to delete the Device {SelectedDevice.Name}. Do you want to Proceed?");
                if (result == AsyncDialogResult.NotConfirmed)
                    return;

                // mark for deletion
                SelectedDevice.MarkForDeletion();

                // save
                ClientTransactionInfo saveResult = await _client.SaveAsync();

                // if not success unmark 
                if (!saveResult.WasSuccessful)
                {
                    await _dialogService.ShowError("Error", "Could not delete Item.");
                    SelectedDevice.UnMarkForDeletion();
                }
            }
            else
            {
                // log
                await _dialogService.ShowMessage("Info", "Nothing is selected");
            }
        }

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
                    GeometrySize = 7,
                    GeometryStroke =
                        new LinearGradientPaint(new[] { new SKColor(255, 0, 0), new SKColor(0, 255, 0) },
                            new SKPoint(0, 1),
                            new SKPoint(0, 0))
                        {
                            StrokeThickness = 3
                        }, //new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                    GeometryFill =
                        new LinearGradientPaint(new[] { new SKColor(255, 0, 0), new SKColor(0, 255, 0) },
                            new SKPoint(0, 1), new SKPoint(0, 0)), //new SolidColorPaint(SKColors.AliceBlue),
                    Stroke = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 2 },
                    TooltipLabelFormatter =
                        chartPoint =>
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

        private IDataItemCollectionValidator<PingHistoryItem> getHistoryQuery(long deviceId)
        {
            // get field
            DataItemField timeField = DataItem.GetClassInfo(typeof(PingHistoryItem))
                .GetField(nameof(PingHistoryItem.DeviceId));

            // create where clayse
            var whereActiveExpr = new WhereComparisonExpression(timeField, WhereOperator.Equal, deviceId);

            // create vaidator and return
            var deletionValidator =
                new DataItemCollectionValidator<PingHistoryItem>(true) as IDataItemCollectionValidator<PingHistoryItem>;
            deletionValidator.QueryExpression = whereActiveExpr;
            deletionValidator.IsForceLoader = true;
            return deletionValidator;
        }
    }
}