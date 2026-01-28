using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RFID_API_ver1;

namespace Timer.Services
{
    /// <summary>
    /// RFID 读写器服务实现，封装 RFID_API_ver1.Reader 的全部交互
    /// </summary>
    public class RfidReaderService : IRfidReaderService
    {
        private readonly ILoggingService _logger;
        private Reader? _reader;
        private readonly Channels _channels = Channels.One;
        private bool _disposed;

        public bool IsConnected { get; private set; }
        public string? CurrentPort { get; private set; }
        public int CurrentBaudRate { get; private set; }

        public string? FirmwareVersion { get; private set; }
        public string? ChipType { get; private set; }
        public string? FrequencyRegion { get; private set; }
        public int? CurrentAntenna { get; private set; }

        public bool? Gpio1High { get; private set; }
        public bool? Gpio2High { get; private set; }

        public event EventHandler<RfidLogEventArgs>? TransportLog;
        public event EventHandler<FirmwareInfoEventArgs>? FirmwareInfoUpdated;
        public event EventHandler<WorkAntennaChangedEventArgs>? WorkAntennaChanged;
        public event EventHandler<GpioStatusEventArgs>? GpioStatusChanged;
        public event EventHandler<InventoryTagEventArgs>? TagRead;
        public event EventHandler<InventoryStatisticsEventArgs>? InventoryStatistics;

        public RfidReaderService(ILoggingService loggingService)
        {
            _logger = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public async Task ConnectAsync(string portName, int baudRate)
        {
            if (IsConnected)
            {
                await DisconnectAsync();
            }

            await Task.Run(() =>
            {
                var readerType = ReaderType.SERIAL;
                _reader = Reader.Create(readerType, _channels, portName, baudRate);

                _reader.MessageTransport += OnMessageTransport;
                _reader.ExceptionReceived += OnExceptionReceived;
                _reader.FirmwareVersionCallback += OnFirmwareVersion;
                _reader.FrequencyCallback += OnFrequency;
                _reader.SettingStatusCallback += OnSettingStatus;
                _reader.GpioPinsCallback += OnGpioPins;
                _reader.WorkAntennaCallback += OnWorkAntenna;

                _reader.Connect();
                IsConnected = true;
                CurrentPort = portName;
                CurrentBaudRate = baudRate;

                // 连接后读取频段信息
                _reader.GetFrequencyRegion();
            });

            _logger.Info($"RFID 读写器已连接: {portName} @ {baudRate}");
        }

        public async Task DisconnectAsync()
        {
            if (_reader == null)
                return;

            await Task.Run(() =>
            {
                try
                {
                    _reader.MessageTransport -= OnMessageTransport;
                    _reader.ExceptionReceived -= OnExceptionReceived;
                    _reader.FirmwareVersionCallback -= OnFirmwareVersion;
                    _reader.FrequencyCallback -= OnFrequency;
                    _reader.SettingStatusCallback -= OnSettingStatus;
                    _reader.GpioPinsCallback -= OnGpioPins;
                    _reader.WorkAntennaCallback -= OnWorkAntenna;

                    _reader.Disconnect();
                }
                catch (Exception ex)
                {
                    _logger.Error("断开 RFID 读写器失败", ex);
                }
                finally
                {
                    _reader = null;
                    IsConnected = false;
                    CurrentPort = null;
                }
            });

            _logger.Info("RFID 读写器已断开");
        }

        public Task RequestFirmwareVersionAsync()
        {
            if (_reader == null)
                throw new InvalidOperationException("读写器未连接");

            return Task.Run(() =>
            {
                _logger.Info("请求固件版本");
                _reader.GetFirmwareVersion();
            });
        }

        public Task RequestFrequencyRegionAsync()
        {
            if (_reader == null)
                throw new InvalidOperationException("读写器未连接");

            return Task.Run(() =>
            {
                _logger.Info("请求频段信息");
                _reader.GetFrequencyRegion();
            });
        }

        public Task SetWorkAntennaAsync(int antennaIndex)
        {
            if (_reader == null)
                throw new InvalidOperationException("读写器未连接");

            if (antennaIndex < 1 || antennaIndex > 4)
                throw new ArgumentOutOfRangeException(nameof(antennaIndex), "天线索引必须是 1-4");

            return Task.Run(() =>
            {
                var antenna = (Antenna)(antennaIndex - 1);
                _logger.Info($"设置工作天线: {antennaIndex}");
                _reader.SetWorkAntenna(antenna);
            });
        }

        public Task RequestWorkAntennaAsync()
        {
            if (_reader == null)
                throw new InvalidOperationException("读写器未连接");

            return Task.Run(() =>
            {
                _logger.Info("读取当前工作天线");
                _reader.GetWorkAntenna();
            });
        }

        public Task ReadGpioAsync()
        {
            if (_reader == null)
                throw new InvalidOperationException("读写器未连接");

            return Task.Run(() =>
            {
                _logger.Info("读取 GPIO 状态");
                _reader.ReadGpioValue();
            });
        }

        public Task WriteGpioAsync(int id, bool high)
        {
            if (_reader == null)
                throw new InvalidOperationException("读写器未连接");

            if (id != 3 && id != 4)
                throw new ArgumentOutOfRangeException(nameof(id), "仅支持 GPIO3 / GPIO4 输出");

            return Task.Run(() =>
            {
                var gpo = new GpioPin(id, high);
                _logger.Info($"写 GPIO{id}: {(high ? "高" : "低")}");
                _reader.WriteGpioValue(gpo);
            });
        }

        public Task StartInventoryAsync(InventoryMode mode, IEnumerable<int> antennas)
        {
            if (_reader == null)
                throw new InvalidOperationException("读写器未连接");

            return Task.Run(() =>
            {
                var antList = antennas?.Distinct().Where(i => i >= 1 && i <= 4).ToList() ?? new List<int> { 1 };
                if (antList.Count == 0)
                    antList.Add(1);

                _logger.Info($"开始盘存, 模式={mode}, 天线={string.Join(",", antList)}");

                if (mode == InventoryMode.FastSwitchAnt)
                {
                    var invAnts = new List<InventoryAntenna>();
                    foreach (var i in antList)
                    {
                        // 停留时间示例：100ms
                        invAnts.Add(new InventoryAntenna((Antenna)(i - 1), 100));
                    }

                    var cfg = new FastSwitchAntInventoryCfg(invAnts, 0, 1);
                    _reader.CommandStatistics += OnCommandStatistics;
                    _reader.TagRead += OnTagRead;

                    _reader.setInventoryCfg(cfg);
                    _reader.FastSwitchAntInventory(cfg);
                }
            });
        }

        public Task StopInventoryAsync()
        {
            // SDK 如果提供显式停止命令，可在此补充；目前先解除事件绑定
            return Task.Run(() =>
            {
                if (_reader == null)
                    return;

                _logger.Info("停止盘存（解除事件绑定，如需发送停止命令可根据 SDK 文档补充）");

                _reader.CommandStatistics -= OnCommandStatistics;
                _reader.TagRead -= OnTagRead;
            });
        }

        private void OnMessageTransport(object? sender, MessageTransportEventArgs e)
        {
            var txt = $"{(e.Tx ? "Send" : "Recv")}: {ByteUtils.ToHex(e.TransportData, "", " ")}";

            _logger.Debug($"[RFID] {txt}");

            TransportLog?.Invoke(this, new RfidLogEventArgs
            {
                Time = DateTime.Now,
                IsSend = e.Tx,
                Message = txt
            });
        }

        private void OnExceptionReceived(object? sender, ExceptionReceivedEventArgs e)
        {
            _logger.Error($"RFID 读写器异常: {e.Exception?.Message}", e.Exception);

            TransportLog?.Invoke(this, new RfidLogEventArgs
            {
                Time = DateTime.Now,
                IsSend = false,
                Message = $"Exception: {e.Exception?.Message}"
            });
        }

        private void OnFirmwareVersion(object? sender, FirmwareVersionEventArgs e)
        {
            var fw = $"{e.Info.Major:x2}.{e.Info.Minor:x2}";
            var chip = e.Info.Chip.ToString();

            FirmwareVersion = fw;
            ChipType = chip;

            _logger.Info($"固件版本: {fw}, 芯片: {chip}");

            FirmwareInfoUpdated?.Invoke(this, new FirmwareInfoEventArgs
            {
                FirmwareVersion = fw,
                ChipType = chip
            });
        }

        private void OnFrequency(object? sender, FreqEventArgs e)
        {
            // 这里只做简单标记和日志，如需详细信息可根据 SDK 文档细化
            FrequencyRegion = "频段信息已更新";
            _logger.Info("频段信息已更新");
        }

        private void OnSettingStatus(object? sender, SettingEventArgs e)
        {
            _logger.Debug("设置状态回调触发");
        }

        private void OnGpioPins(object? sender, GpioPinsEventArgs e)
        {
            bool? g1 = null;
            bool? g2 = null;

            foreach (var gpi in e.gpios)
            {
                if (gpi.ID == 1) g1 = gpi.High;
                if (gpi.ID == 2) g2 = gpi.High;
            }

            Gpio1High = g1;
            Gpio2High = g2;

            _logger.Info($"GPIO 状态: GPIO1={FormatBool(g1)}, GPIO2={FormatBool(g2)}");

            GpioStatusChanged?.Invoke(this, new GpioStatusEventArgs
            {
                Gpio1High = g1,
                Gpio2High = g2
            });
        }

        private void OnWorkAntenna(object? sender, WorkAntennaEventArgs e)
        {
            // 假设 e.ant 是 0-based Antenna 枚举
            var index = (int)e.ant + 1;
            CurrentAntenna = index;

            _logger.Info($"当前工作天线: {index}");

            WorkAntennaChanged?.Invoke(this, new WorkAntennaChangedEventArgs
            {
                AntennaIndex = index
            });
        }

        private void OnTagRead(object? sender, TagReadDataEventArgs e)
        {
            try
            {
                // 由于未使用 TagReadDataEventArgs 具体字段，这里先只记录一次回调触发
                var time = DateTime.Now;

                TagRead?.Invoke(this, new InventoryTagEventArgs
                {
                    Epc = string.Empty,
                    AntennaIndex = 0,
                    ReadTime = time,
                    Rssi = 0
                });
            }
            catch (Exception ex)
            {
                _logger.Error("处理 TagRead 回调失败", ex);
            }
        }

        private void OnCommandStatistics(object? sender, CommandstatisticsEventArgs e)
        {
            InventoryStatistics?.Invoke(this, new InventoryStatisticsEventArgs
            {
                RoundTotalRead = e.RoundTotalRead
            });
        }

        private static string FormatBool(bool? v)
        {
            if (v == null) return "未知";
            return v.Value ? "高" : "低";
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                DisconnectAsync().Wait();
            }
            catch
            {
                // ignore
            }
        }
    }
}

