namespace Timer.Models
{
    /// <summary>
    /// 表示从Excel文件读取的芯片导入数据
    /// </summary>
    public class ChipImportData
    {
        /// <summary>
        /// 序号（Excel中的序号列）
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 芯片标签号码
        /// </summary>
        public string LabelNumber { get; set; } = string.Empty;

        /// <summary>
        /// 芯片内部编号
        /// </summary>
        public string InternalNumber { get; set; } = string.Empty;

        /// <summary>
        /// 组号（芯片组名称）
        /// </summary>
        public string GroupName { get; set; } = string.Empty;
    }
}

