using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Timer.ViewModels
{
    /// <summary>
    /// 参赛人员页面的ViewModel（占位）
    /// </summary>
    public class ParticipantViewModel : ObservableObject, IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// 初始化ParticipantViewModel实例
        /// </summary>
        public ParticipantViewModel()
        {
            Title = "参赛人员";
            Message = "功能待实现";
        }

        /// <summary>
        /// 页面标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 占位提示信息
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 清理托管资源
                _disposed = true;
            }
        }
    }
}

