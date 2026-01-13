using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timer.Models;
using Timer.Services;
using Timer.ViewModels;

namespace Timer.ViewModels
{
    /// <summary>
    /// 主窗口的ViewModel，管理导航菜单和页面切换
    /// </summary>
    public class MainViewModel : ObservableObject, IDisposable
    {
        private readonly INavigationService _navigationService;
        private NavigationItem? _selectedMenuItem;
        private object? _currentView;
        private bool _disposed;

        /// <summary>
        /// 初始化MainViewModel实例
        /// </summary>
        /// <param name="navigationService">导航服务实例</param>
        /// <exception cref="ArgumentNullException">当navigationService为null时抛出</exception>
        public MainViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            MenuItems = new ObservableCollection<NavigationItem>();
            NavigateCommand = new RelayCommand<NavigationItem>(NavigateTo);
            ToggleExpandCommand = new RelayCommand<NavigationItem>(ToggleExpand);
            InitializeMenuItems();
        }

        public ObservableCollection<NavigationItem> MenuItems { get; }

        public NavigationItem? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set => SetProperty(ref _selectedMenuItem, value);
        }

        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public IRelayCommand<NavigationItem> NavigateCommand { get; }
        public IRelayCommand<NavigationItem> ToggleExpandCommand { get; }

        private void InitializeMenuItems()
        {
            // 比赛计时
            var raceTimerItem = new NavigationItem
            {
                Title = "比赛计时",
                ViewModel = new RaceTimerViewModel()
            };
            raceTimerItem.Command = NavigateCommand;

            // 成绩管理
            var scoreItem = new NavigationItem
            {
                Title = "成绩管理",
                ViewModel = new ScoreViewModel()
            };
            scoreItem.Command = NavigateCommand;

            // 人员管理
            var participantManagementItem = new NavigationItem
            {
                Title = "人员管理",
                ViewModel = null
            };

            var participantItem = new NavigationItem
            {
                Title = "参赛人员",
                ViewModel = new ParticipantViewModel()
            };
            participantItem.Command = NavigateCommand;

            var groupItem = new NavigationItem
            {
                Title = "人员分组",
                ViewModel = new GroupViewModel()
            };
            groupItem.Command = NavigateCommand;

            participantManagementItem.Children.Add(participantItem);
            participantManagementItem.Children.Add(groupItem);
            participantManagementItem.Command = ToggleExpandCommand;

            // 设备管理
            var deviceManagementItem = new NavigationItem
            {
                Title = "设备管理",
                ViewModel = null
            };

            var deviceItem = new NavigationItem
            {
                Title = "扫描设备",
                ViewModel = new DeviceViewModel()
            };
            deviceItem.Command = NavigateCommand;

            var chipItem = new NavigationItem
            {
                Title = "芯片设备",
                ViewModel = new ChipViewModel()
            };
            chipItem.Command = NavigateCommand;

            deviceManagementItem.Children.Add(deviceItem);
            deviceManagementItem.Children.Add(chipItem);
            deviceManagementItem.Command = ToggleExpandCommand;

            MenuItems.Add(raceTimerItem);
            MenuItems.Add(scoreItem);
            MenuItems.Add(participantManagementItem);
            MenuItems.Add(deviceManagementItem);
        }

        /// <summary>
        /// 导航到指定菜单项对应的页面
        /// </summary>
        /// <param name="item">导航菜单项</param>
        private void NavigateTo(NavigationItem? item)
        {
            if (item == null || item.ViewModel == null)
                return;

            try
            {
                // Clear previous selection
                if (SelectedMenuItem != null)
                {
                    SelectedMenuItem.IsSelected = false;
                }

                // Set new selection
                item.IsSelected = true;
                SelectedMenuItem = item;

                // Navigate to view
                var view = _navigationService.GetView(item.ViewModel);
                if (view == null)
                {
                    // Navigation failed - revert selection
                    item.IsSelected = false;
                    if (SelectedMenuItem != null)
                    {
                        SelectedMenuItem.IsSelected = true;
                    }
                    return;
                }

                CurrentView = view;
            }
            catch (Exception ex)
            {
                // Log error and revert selection
                // In production, this should use ILoggingService
                System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
                
                // Revert selection on error
                if (SelectedMenuItem != null)
                {
                    SelectedMenuItem.IsSelected = true;
                }
                item.IsSelected = false;
            }
        }

        /// <summary>
        /// 切换菜单项的展开/折叠状态
        /// </summary>
        /// <param name="item">导航菜单项</param>

        private void ToggleExpand(NavigationItem? item)
        {
            if (item == null || !item.HasChildren)
                return;

            item.IsExpanded = !item.IsExpanded;
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
            if (!_disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                    // RelayCommand会自动处理，这里可以添加其他清理逻辑
                    MenuItems.Clear();
                    _currentView = null;
                    _selectedMenuItem = null;
                }

                _disposed = true;
            }
        }
    }
}

