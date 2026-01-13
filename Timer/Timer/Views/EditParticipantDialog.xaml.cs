using System;
using System.Threading.Tasks;
using System.Windows;
using Timer.Models;
using Timer.Services;
using Timer.ViewModels;

namespace Timer.Views
{
    /// <summary>
    /// 编辑参赛人员信息对话框
    /// </summary>
    public partial class EditParticipantDialog : Window
    {
        private readonly EditParticipantDialogViewModel _vm;
        private readonly IParticipantRepository _repository;
        private readonly ILoggingService? _loggingService;

        /// <summary>
        /// 初始化编辑对话框
        /// </summary>
        /// <param name="participant">要编辑的参赛人员（在打开前已从数据库加载最新值）</param>
        /// <param name="repository">数据访问仓库</param>
        /// <param name="loggingService">日志服务（可选）</param>
        public EditParticipantDialog(Participant participant, IParticipantRepository repository, ILoggingService? loggingService = null)
        {
            InitializeComponent();

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _loggingService = loggingService;
            _vm = new EditParticipantDialogViewModel(participant, _repository, _loggingService);
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
                var updated = _vm.ToParticipant();
                await _repository.UpdateAsync(updated);
                _loggingService?.Info($"成功更新参赛人员: {updated.Name} (ID: {updated.Id})");

                // 设置对话框结果为成功
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"保存参赛人员信息失败: {ex.Message}", ex);
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

