using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Timer.Services
{
    /// <summary>
    /// 盘存模式（当前只用快速切换天线模式，后续可扩展）
    /// </summary>
    public enum InventoryMode
    {
        FastSwitchAnt
    }

    public class RfidLogEventArgs : EventArgs
    {
        public DateTime Time { get; init; }
        public bool IsSend { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public class FirmwareInfoEventArgs : EventArgs
    {
        public string FirmwareVersion { get; init; } = string.Empty;
        public string ChipType { get; init; } = string.Empty;
    }

    public class WorkAntennaChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 工作天线索引（1-4）
        /// </summary>
        public int AntennaIndex { get; init; }
    }

    public class GpioStatusEventArgs : EventArgs
    {
        public bool? Gpio1High { get; init; }
        public bool? Gpio2High { get; init; }
    }

    public class InventoryTagEventArgs : EventArgs
    {
        /// <summary>
        /// 标签 EPC（十六进制字符串）
        /// </summary>
        public string Epc { get; init; } = string.Empty;

        /// <summary>
        /// 读取天线（1-4）
        /// </summary>
        public int AntennaIndex { get; init; }

        /// <summary>
        /// 读取时间
        /// </summary>
        public DateTime ReadTime { get; init; }

        /// <summary>
        /// 信号强度
        /// </summary>
        public int Rssi { get; init; }
    }

    public class InventoryStatisticsEventArgs : EventArgs
    {
        /// <summary>
        /// 本轮盘存读取到的标签总数
        /// </summary>
        public int RoundTotalRead { get; init; }
    }

    /// <summary>
    /// RFID 读写器服务接口，封装底层 SDK，供 ViewModel / 计时业务调用
    /// </summary>
    public interface IRfidReaderService : IDisposable
    {
        bool IsConnected { get; }
        string? CurrentPort { get; }
        int CurrentBaudRate { get; }

        string? FirmwareVersion { get; }
        string? ChipType { get; }
        string? FrequencyRegion { get; }
        int? CurrentAntenna { get; }

        bool? Gpio1High { get; }
        bool? Gpio2High { get; }

        event EventHandler<RfidLogEventArgs>? TransportLog;
        event EventHandler<FirmwareInfoEventArgs>? FirmwareInfoUpdated;
        event EventHandler<WorkAntennaChangedEventArgs>? WorkAntennaChanged;
        event EventHandler<GpioStatusEventArgs>? GpioStatusChanged;
        event EventHandler<InventoryTagEventArgs>? TagRead;
        event EventHandler<InventoryStatisticsEventArgs>? InventoryStatistics;

        Task ConnectAsync(string portName, int baudRate);
        Task DisconnectAsync();

        Task RequestFirmwareVersionAsync();
        Task RequestFrequencyRegionAsync();

        Task SetWorkAntennaAsync(int antennaIndex);
        Task RequestWorkAntennaAsync();

        Task ReadGpioAsync();
        Task WriteGpioAsync(int id, bool high);

        Task StartInventoryAsync(InventoryMode mode, IEnumerable<int> antennas);
        Task StopInventoryAsync();
    }
}

