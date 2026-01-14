using System;
using System.Windows;
using Timer.Models;
using Timer.Services;
using Timer.ViewModels;

namespace Timer.Views
{
    /// <summary>
    /// 编辑芯片对话框
    /// </summary>
    public partial class EditChipDialog : Window
    {
        private readonly EditChipDialogViewModel _vm;
        private readonly IChipRepository _repository;
        private readonly ILoggingService? _loggingService;

        /// <summary>
        /// 初始化编辑芯片对话框
        /// </summary>
        /// <param name="chip">要编辑的芯片</param>
        /// <param name="repository">数据访问仓库</param>
        /// <param name="loggingService">日志服务（可选）</param>
        public EditChipDialog(Chip chip, IChipRepository repository, ILoggingService? loggingService = null)
        {
            InitializeComponent();

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _loggingService = loggingService;
            _vm = new EditChipDialogViewModel(chip, _repository, _loggingService);
            DataContext = _vm;
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证数据
                if (!await _vm.ValidateAsync())
                {
                    return; // 显示错误，不关闭对话框
                }

                // 更新数据库
                var updated = _vm.ToChip();
                await _repository.UpdateChipAsync(updated);
                _loggingService?.Info($"成功更新芯片: {updated.LabelNumber} (ID: {updated.Id})");

                // 设置对话框结果为成功
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"保存芯片信息失败: {ex.Message}", ex);
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

