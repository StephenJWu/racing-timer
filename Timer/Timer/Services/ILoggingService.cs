namespace Timer.Services
{
    /// <summary>
    /// 日志服务接口，定义日志记录功能
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// 记录跟踪级别的日志（最详细的日志信息）
        /// </summary>
        /// <param name="message">日志消息</param>
        void Trace(string message);

        /// <summary>
        /// 记录调试级别的日志（开发调试信息）
        /// </summary>
        /// <param name="message">日志消息</param>
        void Debug(string message);

        /// <summary>
        /// 记录信息级别的日志（一般信息）
        /// </summary>
        /// <param name="message">日志消息</param>
        void Info(string message);

        /// <summary>
        /// 记录警告级别的日志（潜在问题）
        /// </summary>
        /// <param name="message">日志消息</param>
        void Warn(string message);

        /// <summary>
        /// 记录错误级别的日志（错误和异常）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常对象（可选）</param>
        void Error(string message, Exception? exception = null);
    }
}

