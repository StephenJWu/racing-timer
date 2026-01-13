using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Timer.Data;
using Timer.Models;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 参赛人员管理页面的ViewModel
    /// </summary>
    public class ParticipantViewModel : ObservableObject, IDisposable
    {
        private readonly IParticipantRepository _repository;
        private readonly IExcelImportService _excelImportService;
        private readonly ILoggingService? _loggingService;
        private readonly DatabaseContext _dbContext;
        private bool _disposed;

        private ObservableCollection<Participant> _participants = new();
        private Participant? _selectedParticipant;
        private SearchFilter _searchFilter = new();
        private bool _isLoading;
        private double _importProgress;
        private ImportResult? _importResult;
        private int _totalCount;
        private int _currentPage = 1;
        private int _totalPages;

        // 日期范围
        private DateTime? _startDate;
        private DateTime? _endDate;

        // 级联下拉框数据源
        private ObservableCollection<string> _schools = new();
        private ObservableCollection<string> _grades = new();
        private ObservableCollection<string> _classes = new();
        private ObservableCollection<string> _groupNames = new();

        /// <summary>
        /// 初始化ParticipantViewModel实例
        /// </summary>
        public ParticipantViewModel(
            IParticipantRepository repository,
            IExcelImportService excelImportService,
            DatabaseContext dbContext,
            ILoggingService? loggingService = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _excelImportService = excelImportService ?? throw new ArgumentNullException(nameof(excelImportService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _loggingService = loggingService;

            Title = "参赛人员管理";
            ImportCommand = new AsyncRelayCommand(ImportExcelAsync);
            SearchCommand = new RelayCommand(Search);
            ClearSearchCommand = new RelayCommand(ClearSearch);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => CurrentPage > 1);
            NextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
            RefreshCommand = new AsyncRelayCommand(LoadParticipantsAsync);
            LoadSchoolsCommand = new AsyncRelayCommand(LoadSchoolsAsync);
            SchoolChangedCommand = new AsyncRelayCommand<string>(OnSchoolChangedAsync);
            GradeChangedCommand = new AsyncRelayCommand<string>(OnGradeChangedAsync);
            ClassChangedCommand = new AsyncRelayCommand<string>(OnClassChangedAsync);

            // 初始化时加载数据
            _ = LoadSchoolsAsync();
            _ = LoadParticipantsAsync();
        }

        /// <summary>
        /// 页面标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 当前页的人员列表
        /// </summary>
        public ObservableCollection<Participant> Participants
        {
            get => _participants;
            set => SetProperty(ref _participants, value);
        }

        /// <summary>
        /// 当前选中的人员
        /// </summary>
        public Participant? SelectedParticipant
        {
            get => _selectedParticipant;
            set => SetProperty(ref _selectedParticipant, value);
        }

        /// <summary>
        /// 搜索筛选条件
        /// </summary>
        public SearchFilter SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    // 当SearchFilter改变时，同步日期范围
                    if (value != null)
                    {
                        _startDate = value.StartDate;
                        _endDate = value.EndDate;
                        OnPropertyChanged(nameof(StartDate));
                        OnPropertyChanged(nameof(EndDate));
                    }
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

        /// <summary>
        /// 导入进度（0-100）
        /// </summary>
        public double ImportProgress
        {
            get => _importProgress;
            set => SetProperty(ref _importProgress, value);
        }

        /// <summary>
        /// 导入结果
        /// </summary>
        public ImportResult? ImportResult
        {
            get => _importResult;
            set => SetProperty(ref _importResult, value);
        }

        /// <summary>
        /// 总记录数
        /// </summary>
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
                    _ = LoadParticipantsAsync();
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
        /// 导入Excel文件命令
        /// </summary>
        public IAsyncRelayCommand ImportCommand { get; }

        /// <summary>
        /// 搜索命令
        /// </summary>
        public IRelayCommand SearchCommand { get; }

        /// <summary>
        /// 清除搜索命令
        /// </summary>
        public IRelayCommand ClearSearchCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public IRelayCommand PreviousPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public IRelayCommand NextPageCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public IAsyncRelayCommand RefreshCommand { get; }

        /// <summary>
        /// 加载学校列表命令
        /// </summary>
        public IAsyncRelayCommand LoadSchoolsCommand { get; }

        /// <summary>
        /// 学校选择改变命令
        /// </summary>
        public IAsyncRelayCommand<string> SchoolChangedCommand { get; }

        /// <summary>
        /// 年级选择改变命令
        /// </summary>
        public IAsyncRelayCommand<string> GradeChangedCommand { get; }

        /// <summary>
        /// 班级选择改变命令
        /// </summary>
        public IAsyncRelayCommand<string> ClassChangedCommand { get; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    SearchFilter.StartDate = value;
                }
            }
        }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    SearchFilter.EndDate = value;
                }
            }
        }

        /// <summary>
        /// 选中的学校（用于级联下拉框）
        /// </summary>
        public string? SelectedSchool
        {
            get => SearchFilter.School;
            set
            {
                if (SearchFilter.School != value)
                {
                    _ = OnSchoolChangedAsync(value);
                }
            }
        }

        /// <summary>
        /// 选中的年级（用于级联下拉框）
        /// </summary>
        public string? SelectedGrade
        {
            get => SearchFilter.Grade;
            set
            {
                if (SearchFilter.Grade != value)
                {
                    _ = OnGradeChangedAsync(value);
                }
            }
        }

        /// <summary>
        /// 选中的班级（用于级联下拉框）
        /// </summary>
        public string? SelectedClass
        {
            get => SearchFilter.Class;
            set
            {
                if (SearchFilter.Class != value)
                {
                    _ = OnClassChangedAsync(value);
                }
            }
        }

        /// <summary>
        /// 学校列表
        /// </summary>
        public ObservableCollection<string> Schools
        {
            get => _schools;
            set => SetProperty(ref _schools, value);
        }

        /// <summary>
        /// 年级列表
        /// </summary>
        public ObservableCollection<string> Grades
        {
            get => _grades;
            set => SetProperty(ref _grades, value);
        }

        /// <summary>
        /// 班级列表
        /// </summary>
        public ObservableCollection<string> Classes
        {
            get => _classes;
            set => SetProperty(ref _classes, value);
        }

        /// <summary>
        /// 组别列表
        /// </summary>
        public ObservableCollection<string> GroupNames
        {
            get => _groupNames;
            set => SetProperty(ref _groupNames, value);
        }

        /// <summary>
        /// 导入Excel文件
        /// </summary>
        private async Task ImportExcelAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Excel文件 (*.xls;*.xlsx)|*.xls;*.xlsx|所有文件 (*.*)|*.*",
                    Title = "选择要导入的Excel文件"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    ImportProgress = 0;
                    ImportResult = null;

                    try
                    {
                        // 读取Excel文件
                        var participants = await _excelImportService.ReadFromFileAsync(dialog.FileName);

                        // 导入到数据库
                        var progress = new Progress<double>(value => ImportProgress = value);
                        ImportResult = await _excelImportService.ImportAsync(participants, progress);

                        if (ImportResult.IsSuccess())
                        {
                            MessageBox.Show(
                                $"成功导入{ImportResult.SuccessCount}条记录",
                                "导入成功",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            var errorMessage = $"导入完成：成功{ImportResult.SuccessCount}条，失败{ImportResult.FailureCount}条\n\n";
                            errorMessage += string.Join("\n", ImportResult.Errors.Take(10).Select(e => e.ToString()));
                            if (ImportResult.Errors.Count > 10)
                            {
                                errorMessage += $"\n... 还有{ImportResult.Errors.Count - 10}个错误";
                            }

                            MessageBox.Show(
                                errorMessage,
                                "导入完成（有错误）",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }

                        // 刷新列表
                        await LoadParticipantsAsync();
                        // 刷新筛选项数据源（学校/年级/班级/组别）
                        await RefreshFilterSourcesAsync();
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.Error($"导入Excel文件失败: {ex.Message}", ex);
                        MessageBox.Show(
                            $"导入失败：{ex.Message}",
                            "错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"导入过程中发生错误: {ex.Message}", ex);
                MessageBox.Show(
                    $"发生错误：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private void Search()
        {
            CurrentPage = 1;
            _ = LoadParticipantsAsync();
        }

        /// <summary>
        /// 清除搜索
        /// </summary>
        private void ClearSearch()
        {
            SearchFilter = new SearchFilter();
            StartDate = null;
            EndDate = null;
            Grades.Clear();
            Classes.Clear();
            GroupNames.Clear();
            CurrentPage = 1;
            _ = LoadParticipantsAsync();
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
        /// 更新总页数
        /// </summary>
        private void UpdateTotalPages()
        {
            TotalPages = (int)Math.Ceiling((double)TotalCount / SearchFilter.PageSize);
            if (TotalPages == 0)
            {
                TotalPages = 1;
            }
        }

        /// <summary>
        /// 异步加载人员列表
        /// </summary>
        public async Task LoadParticipantsAsync()
        {
            try
            {
                IsLoading = true;

                SearchFilter.PageNumber = CurrentPage;
                var participants = await _repository.GetAllAsync(SearchFilter);
                var totalCount = await _repository.GetTotalCountAsync(SearchFilter);

                Participants = new ObservableCollection<Participant>(participants);
                TotalCount = totalCount;
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载人员列表失败: {ex.Message}", ex);
                MessageBox.Show(
                    $"加载数据失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 刷新列表
        /// </summary>
        public void RefreshList()
        {
            _ = LoadParticipantsAsync();
        }

        /// <summary>
        /// 加载学校列表
        /// </summary>
        private async Task LoadSchoolsAsync()
        {
            try
            {
                var currentSchool = SearchFilter.School;
                var schools = (await _repository.GetDistinctSchoolsAsync()).ToList();
                Schools = new ObservableCollection<string>(schools);

                // 如果当前选择已不存在，则清空；否则保留，并触发级联刷新
                if (!string.IsNullOrWhiteSpace(currentSchool) && schools.Contains(currentSchool))
                {
                    await OnSchoolChangedAsync(currentSchool);
                }
                else if (!string.IsNullOrWhiteSpace(currentSchool) && !schools.Contains(currentSchool))
                {
                    await OnSchoolChangedAsync(null);
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载学校列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 刷新筛选项数据源（导入后/数据变化后调用）
        /// </summary>
        private async Task RefreshFilterSourcesAsync()
        {
            // 先刷新学校（内部会根据当前选择触发级联刷新）
            await LoadSchoolsAsync();

            // 如果没有选学校，则提供“全量”年级列表，方便用户直接选年级再选学校（可按需要调整）
            if (string.IsNullOrWhiteSpace(SearchFilter.School))
            {
                try
                {
                    var grades = await _repository.GetDistinctGradesAsync(null);
                    Grades = new ObservableCollection<string>(grades);
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"刷新年级列表失败: {ex.Message}", ex);
                }
            }
        }
        /// <summary>
        /// 学校选择改变时的处理
        /// </summary>
        private async Task OnSchoolChangedAsync(string? school)
        {
            SearchFilter.School = school;
            SearchFilter.Grade = null;
            SearchFilter.Class = null;
            SearchFilter.GroupName = null;
            OnPropertyChanged(nameof(SelectedSchool));
            OnPropertyChanged(nameof(SelectedGrade));
            OnPropertyChanged(nameof(SelectedClass));

            Grades.Clear();
            Classes.Clear();
            GroupNames.Clear();

            if (!string.IsNullOrWhiteSpace(school))
            {
                try
                {
                    var grades = await _repository.GetDistinctGradesAsync(school);
                    Grades = new ObservableCollection<string>(grades);
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"加载年级列表失败: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 年级选择改变时的处理
        /// </summary>
        private async Task OnGradeChangedAsync(string? grade)
        {
            SearchFilter.Grade = grade;
            SearchFilter.Class = null;
            SearchFilter.GroupName = null;
            OnPropertyChanged(nameof(SelectedGrade));
            OnPropertyChanged(nameof(SelectedClass));

            Classes.Clear();
            GroupNames.Clear();

            if (!string.IsNullOrWhiteSpace(grade) && !string.IsNullOrWhiteSpace(SearchFilter.School))
            {
                try
                {
                    var classes = await _repository.GetDistinctClassesAsync(SearchFilter.School, grade);
                    Classes = new ObservableCollection<string>(classes);
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"加载班级列表失败: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 班级选择改变时的处理
        /// </summary>
        private async Task OnClassChangedAsync(string? classValue)
        {
            SearchFilter.Class = classValue;
            SearchFilter.GroupName = null;
            OnPropertyChanged(nameof(SelectedClass));

            GroupNames.Clear();

            if (!string.IsNullOrWhiteSpace(classValue) && !string.IsNullOrWhiteSpace(SearchFilter.School) && !string.IsNullOrWhiteSpace(SearchFilter.Grade))
            {
                try
                {
                    var groupNames = await _repository.GetDistinctGroupNamesAsync(SearchFilter.School, SearchFilter.Grade, classValue);
                    GroupNames = new ObservableCollection<string>(groupNames);
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"加载组别列表失败: {ex.Message}", ex);
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
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _dbContext?.Dispose();
                _disposed = true;
            }
        }
    }
}
