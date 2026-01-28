using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 扫描设备页面的 ViewModel：负责 RFID 读写器的连接、配置与调试
    /// </summary>
    public class DeviceViewModel : ObservableObject, IDisposable
    {
        private readonly IRfidReaderService _rfidService;
        private bool _disposed;

        private string? _selectedPort;
        private int _selectedBaudRate;
        private bool _isBusy;

        private string _firmwareVersion = "-";
        private string _chipType = "-";
        private string _frequencyRegion = "-";

        private int _selectedAntenna = 1;
        private int? _currentAntenna;

        private bool? _gpio1High;
        private bool? _gpio2High;
        private bool _gpio3HighToWrite;
        private bool _gpio4HighToWrite;

        private int _lastRoundTagCount;
        private int _totalTagCount;

        private readonly StringBuilder _logBuilder = new();
        private string _logText = string.Empty;

        public DeviceViewModel()
            : this(new RfidReaderService(App.GetLoggingService()))
        {
        }

        public DeviceViewModel(IRfidReaderService rfidReaderService)
        {
            _rfidService = rfidReaderService ?? throw new ArgumentNullException(nameof(rfidReaderService));

            Title = "扫描设备";

            AvailablePorts = new ObservableCollection<string>();
            AvailableBaudRates = new ObservableCollection<int> { 115200 };
            _selectedBaudRate = AvailableBaudRates.First();

            RefreshPorts();

            ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsBusy);
            DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => !IsBusy);
            RefreshPortsCommand = new RelayCommand(RefreshPorts, () => !IsBusy);

            ReadFirmwareCommand = new AsyncRelayCommand(ReadFirmwareAsync, () => !IsBusy && IsConnected);
            ReadFrequencyCommand = new AsyncRelayCommand(ReadFrequencyAsync, () => !IsBusy && IsConnected);

            SetWorkAntennaCommand = new AsyncRelayCommand(SetWorkAntennaAsync, () => !IsBusy && IsConnected);
            GetWorkAntennaCommand = new AsyncRelayCommand(GetWorkAntennaAsync, () => !IsBusy && IsConnected);

            ReadGpioCommand = new AsyncRelayCommand(ReadGpioAsync, () => !IsBusy && IsConnected);
            WriteGpio3Command = new AsyncRelayCommand(WriteGpio3Async, () => !IsBusy && IsConnected);
            WriteGpio4Command = new AsyncRelayCommand(WriteGpio4Async, () => !IsBusy && IsConnected);

            StartInventoryCommand = new AsyncRelayCommand(StartInventoryAsync, () => !IsBusy && IsConnected);
            StopInventoryCommand = new AsyncRelayCommand(StopInventoryAsync, () => !IsBusy && IsConnected);

            _rfidService.TransportLog += OnTransportLog;
            _rfidService.FirmwareInfoUpdated += OnFirmwareInfoUpdated;
            _rfidService.WorkAntennaChanged += OnWorkAntennaChanged;
            _rfidService.GpioStatusChanged += OnGpioStatusChanged;
            _rfidService.InventoryStatistics += OnInventoryStatistics;
            _rfidService.TagRead += OnTagRead;
        }

        public string Title { get; }

        public ObservableCollection<string> AvailablePorts { get; }

        public ObservableCollection<int> AvailableBaudRates { get; }

        public string? SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        public bool IsConnected => _rfidService.IsConnected;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCommandCanExecuteChanged();
                }
            }
        }

        public string FirmwareVersion
        {
            get => _firmwareVersion;
            private set => SetProperty(ref _firmwareVersion, value);
        }

        public string ChipType
        {
            get => _chipType;
            private set => SetProperty(ref _chipType, value);
        }

        public string FrequencyRegion
        {
            get => _frequencyRegion;
            private set => SetProperty(ref _frequencyRegion, value);
        }

        public int SelectedAntenna
        {
            get => _selectedAntenna;
            set => SetProperty(ref _selectedAntenna, value);
        }

        public int? CurrentAntenna
        {
            get => _currentAntenna;
            private set => SetProperty(ref _currentAntenna, value);
        }

        public bool? Gpio1High
        {
            get => _gpio1High;
            private set => SetProperty(ref _gpio1High, value);
        }

        public bool? Gpio2High
        {
            get => _gpio2High;
            private set => SetProperty(ref _gpio2High, value);
        }

        public bool Gpio3HighToWrite
        {
            get => _gpio3HighToWrite;
            set => SetProperty(ref _gpio3HighToWrite, value);
        }

        public bool Gpio4HighToWrite
        {
            get => _gpio4HighToWrite;
            set => SetProperty(ref _gpio4HighToWrite, value);
        }

        public int LastRoundTagCount
        {
            get => _lastRoundTagCount;
            private set => SetProperty(ref _lastRoundTagCount, value);
        }

        public int TotalTagCount
        {
            get => _totalTagCount;
            private set => SetProperty(ref _totalTagCount, value);
        }

        public string LogText
        {
            get => _logText;
            private set => SetProperty(ref _logText, value);
        }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand RefreshPortsCommand { get; }

        public ICommand ReadFirmwareCommand { get; }
        public ICommand ReadFrequencyCommand { get; }

        public ICommand SetWorkAntennaCommand { get; }
        public ICommand GetWorkAntennaCommand { get; }

        public ICommand ReadGpioCommand { get; }
        public ICommand WriteGpio3Command { get; }
        public ICommand WriteGpio4Command { get; }

        public ICommand StartInventoryCommand { get; }
        public ICommand StopInventoryCommand { get; }

        private void RefreshPorts()
        {
            AvailablePorts.Clear();
            var ports = System.IO.Ports.SerialPort.GetPortNames().OrderBy(p => p).ToList();
            foreach (var port in ports)
            {
                AvailablePorts.Add(port);
            }

            SelectedPort = AvailablePorts.LastOrDefault();
        }

        private async Task ConnectAsync()
        {
            if (SelectedPort == null)
                return;

            IsBusy = true;
            AppendLog($"[INFO] 连接读写器: {SelectedPort} @ {SelectedBaudRate}");

            try
            {
                await _rfidService.ConnectAsync(SelectedPort, SelectedBaudRate);
                OnPropertyChanged(nameof(IsConnected));
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 连接失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DisconnectAsync()
        {
            IsBusy = true;
            AppendLog("[INFO] 断开读写器");

            try
            {
                await _rfidService.DisconnectAsync();
                OnPropertyChanged(nameof(IsConnected));
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 断开失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ReadFirmwareAsync()
        {
            IsBusy = true;
            AppendLog("[INFO] 读取固件版本");

            try
            {
                await _rfidService.RequestFirmwareVersionAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 请求固件版本失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ReadFrequencyAsync()
        {
            IsBusy = true;
            AppendLog("[INFO] 读取频段信息");

            try
            {
                await _rfidService.RequestFrequencyRegionAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 请求频段失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SetWorkAntennaAsync()
        {
            IsBusy = true;
            AppendLog($"[INFO] 设置工作天线: {SelectedAntenna}");

            try
            {
                await _rfidService.SetWorkAntennaAsync(SelectedAntenna);
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 设置工作天线失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GetWorkAntennaAsync()
        {
            IsBusy = true;
            AppendLog("[INFO] 读取当前工作天线");

            try
            {
                await _rfidService.RequestWorkAntennaAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 读取工作天线失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ReadGpioAsync()
        {
            IsBusy = true;
            AppendLog("[INFO] 读取 GPIO 状态");

            try
            {
                await _rfidService.ReadGpioAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 读取 GPIO 失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task WriteGpio3Async()
        {
            IsBusy = true;
            AppendLog($"[INFO] 写 GPIO3: {(Gpio3HighToWrite ? "高" : "低")}");

            try
            {
                await _rfidService.WriteGpioAsync(3, Gpio3HighToWrite);
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 写 GPIO3 失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task WriteGpio4Async()
        {
            IsBusy = true;
            AppendLog($"[INFO] 写 GPIO4: {(Gpio4HighToWrite ? "高" : "低")}");

            try
            {
                await _rfidService.WriteGpioAsync(4, Gpio4HighToWrite);
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 写 GPIO4 失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task StartInventoryAsync()
        {
            IsBusy = true;
            LastRoundTagCount = 0;
            TotalTagCount = 0;
            AppendLog("[INFO] 开始盘存");

            try
            {
                var ants = new[] { SelectedAntenna };
                await _rfidService.StartInventoryAsync(InventoryMode.FastSwitchAnt, ants);
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 开始盘存失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task StopInventoryAsync()
        {
            IsBusy = true;
            AppendLog("[INFO] 停止盘存");

            try
            {
                await _rfidService.StopInventoryAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] 停止盘存失败: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnTransportLog(object? sender, RfidLogEventArgs e)
        {
            AppendLog($"{(e.IsSend ? "[TX]" : "[RX]")} {e.Message}");
        }

        private void OnFirmwareInfoUpdated(object? sender, FirmwareInfoEventArgs e)
        {
            FirmwareVersion = e.FirmwareVersion;
            ChipType = e.ChipType;
        }

        private void OnWorkAntennaChanged(object? sender, WorkAntennaChangedEventArgs e)
        {
            CurrentAntenna = e.AntennaIndex;
        }

        private void OnGpioStatusChanged(object? sender, GpioStatusEventArgs e)
        {
            Gpio1High = e.Gpio1High;
            Gpio2High = e.Gpio2High;
        }

        private void OnInventoryStatistics(object? sender, InventoryStatisticsEventArgs e)
        {
            LastRoundTagCount = e.RoundTotalRead;
            TotalTagCount += e.RoundTotalRead;
        }

        private void OnTagRead(object? sender, InventoryTagEventArgs e)
        {
            AppendLog($"[TAG] EPC={e.Epc}, ANT={e.AntennaIndex}, RSSI={e.Rssi}");
        }

        private void AppendLog(string line)
        {
            _logBuilder.AppendLine($"{DateTime.Now:HH:mm:ss.fff} {line}");
            LogText = _logBuilder.ToString();
        }

        private void RaiseCommandCanExecuteChanged()
        {
            (ConnectCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (DisconnectCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (RefreshPortsCommand as RelayCommand)?.NotifyCanExecuteChanged();

            (ReadFirmwareCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (ReadFrequencyCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

            (SetWorkAntennaCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (GetWorkAntennaCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

            (ReadGpioCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (WriteGpio3Command as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (WriteGpio4Command as AsyncRelayCommand)?.NotifyCanExecuteChanged();

            (StartInventoryCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (StopInventoryCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _rfidService.TransportLog -= OnTransportLog;
                _rfidService.FirmwareInfoUpdated -= OnFirmwareInfoUpdated;
                _rfidService.WorkAntennaChanged -= OnWorkAntennaChanged;
                _rfidService.GpioStatusChanged -= OnGpioStatusChanged;
                _rfidService.InventoryStatistics -= OnInventoryStatistics;
                _rfidService.TagRead -= OnTagRead;

                _rfidService.Dispose();

                _disposed = true;
            }
        }
    }
}

