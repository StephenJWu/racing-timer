using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timer.Models;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 项目管理页面的ViewModel
    /// </summary>
    public class ProjectViewModel : ObservableObject, IDisposable
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ILoggingService? _loggingService;
        private bool _disposed;

        // 查询条件
        private DateTime? _startDate;
        private DateTime? _endDate;
        private string? _selectedProject;

        // 数据
        private Project? _selectedProjectItem;
        private int _totalCount;
        private bool _isLoading;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 10;

        // 全量数据（用于分页）
        private System.Collections.Generic.List<Project> _allProjects = new();

        private const string AllOption = "全部";

        public ProjectViewModel(IProjectRepository projectRepository, ILoggingService? loggingService = null)
        {
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _loggingService = loggingService;

            Title = "项目管理";

            // 初始化集合
            Projects = new ObservableCollection<Project>();
            ProjectNames = new ObservableCollection<string>();

            // 初始化命令
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            AddCommand = new AsyncRelayCommand(AddProjectAsync);
            EditCommand = new AsyncRelayCommand<Project>(EditProjectAsync);
            DeleteCommand = new AsyncRelayCommand<Project>(DeleteProjectAsync);
            BatchDeleteCommand = new AsyncRelayCommand(BatchDeleteAsync);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => CurrentPage > 1);
            NextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);

            // 加载初始数据
            _ = LoadInitialDataAsync();
        }

        /// <summary>
        /// 页面标题
        /// </summary>
        public string Title { get; }

        // 查询条件属性
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public ObservableCollection<string> ProjectNames { get; }

        public string? SelectedProject
        {
            get => _selectedProject;
            set => SetProperty(ref _selectedProject, value);
        }

        // 数据属性
        public ObservableCollection<Project> Projects { get; }

        public Project? SelectedProjectItem
        {
            get => _selectedProjectItem;
            set => SetProperty(ref _selectedProjectItem, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    UpdateTotalPages();
                }
            }
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                    LoadCurrentPageData();
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (SetProperty(ref _totalPages, value))
                {
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // 命令
        public IAsyncRelayCommand SearchCommand { get; }
        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand<Project> EditCommand { get; }
        public IAsyncRelayCommand<Project> DeleteCommand { get; }
        public IAsyncRelayCommand BatchDeleteCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand NextPageCommand { get; }

        /// <summary>
        /// 加载初始数据
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            try
            {
                await LoadProjectNamesAsync();
                await SearchAsync();
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载初始数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载项目名称列表
        /// </summary>
        private async Task LoadProjectNamesAsync()
        {
            try
            {
                var names = await _projectRepository.GetDistinctProjectNamesAsync();
                ProjectNames.Clear();
                ProjectNames.Add(AllOption);
                foreach (var name in names)
                {
                    ProjectNames.Add(name);
                }
                _selectedProject = AllOption;
                OnPropertyChanged(nameof(SelectedProject));
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载项目名称失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 查询项目
        /// </summary>
        private async Task SearchAsync()
        {
            _loggingService?.Info("[按钮点击] 项目管理 - 查询按钮");
            _loggingService?.Debug($"[查询条件] ProjectName={SelectedProject}, StartDate={StartDate}, EndDate={EndDate}");
            try
            {
                IsLoading = true;
                // 让UI有机会刷新显示遮罩层
                await Task.Delay(50);
                
                var projectName = (SelectedProject == AllOption) ? null : SelectedProject;
                var projects = await _projectRepository.QueryAsync(StartDate, EndDate, projectName);

                _allProjects = projects.ToList();
                TotalCount = _allProjects.Count;
                _currentPage = 1;
                OnPropertyChanged(nameof(CurrentPage));
                
                LoadCurrentPageData();

                _loggingService?.Info($"[查询结果] 查询到 {TotalCount} 个项目");
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"查询项目失败: {ex.Message}", ex);
                MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载当前页数据
        /// </summary>
        private void LoadCurrentPageData()
        {
            Projects.Clear();
            var pageData = _allProjects
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);
            foreach (var project in pageData)
            {
                Projects.Add(project);
            }
        }

        /// <summary>
        /// 更新总页数
        /// </summary>
        private void UpdateTotalPages()
        {
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            if (TotalPages == 0)
            {
                TotalPages = 1;
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        /// <summary>
        /// 新增项目
        /// </summary>
        private async Task AddProjectAsync()
        {
            try
            {
                var dialogViewModel = new EditProjectDialogViewModel();
                var dialog = new Timer.Views.EditProjectDialog
                {
                    DataContext = dialogViewModel,
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    var project = dialogViewModel.GetProject();
                    await _projectRepository.AddAsync(project);

                    MessageBox.Show("新增成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    _loggingService?.Info($"新增项目: {project.Name}");

                    // 刷新数据
                    await LoadProjectNamesAsync();
                    await SearchAsync();
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"新增项目失败: {ex.Message}", ex);
                MessageBox.Show($"新增失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 编辑项目
        /// </summary>
        private async Task EditProjectAsync(Project? project)
        {
            if (project == null) return;

            try
            {
                var dialogViewModel = new EditProjectDialogViewModel(project);
                var dialog = new Timer.Views.EditProjectDialog
                {
                    DataContext = dialogViewModel,
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    var updatedProject = dialogViewModel.GetProject();
                    await _projectRepository.UpdateAsync(updatedProject);

                    MessageBox.Show("编辑成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    _loggingService?.Info($"编辑项目: {updatedProject.Name}");

                    // 刷新数据
                    await LoadProjectNamesAsync();
                    await SearchAsync();
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"编辑项目失败: {ex.Message}", ex);
                MessageBox.Show($"编辑失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        private async Task DeleteProjectAsync(Project? project)
        {
            if (project == null) return;

            var result = MessageBox.Show(
                $"确定要删除项目 \"{project.Name}\" 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _projectRepository.DeleteAsync(project.Id);
                    Projects.Remove(project);
                    TotalCount = Projects.Count;

                    MessageBox.Show("删除成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    _loggingService?.Info($"删除项目: {project.Name}");

                    // 刷新项目名称列表
                    await LoadProjectNamesAsync();
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"删除项目失败: {ex.Message}", ex);
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 批量删除项目
        /// </summary>
        private async Task BatchDeleteAsync()
        {
            var selectedProjects = Projects.Where(p => p.IsSelected).ToList();
            if (!selectedProjects.Any())
            {
                MessageBox.Show("请先选择要删除的项目", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除选中的 {selectedProjects.Count} 个项目吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var ids = selectedProjects.Select(p => p.Id);
                    await _projectRepository.BatchDeleteAsync(ids);

                    foreach (var project in selectedProjects)
                    {
                        Projects.Remove(project);
                    }
                    TotalCount = Projects.Count;

                    MessageBox.Show($"成功删除 {selectedProjects.Count} 个项目！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    _loggingService?.Info($"批量删除 {selectedProjects.Count} 个项目");

                    // 刷新项目名称列表
                    await LoadProjectNamesAsync();
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"批量删除项目失败: {ex.Message}", ex);
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

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
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
            }
        }
    }
}
