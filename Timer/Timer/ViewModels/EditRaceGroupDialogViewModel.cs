using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timer.Models;
using System.Collections.ObjectModel;

namespace Timer.ViewModels
{
    /// <summary>
    /// 编辑比赛分组对话框的ViewModel
    /// </summary>
    public class EditRaceGroupDialogViewModel : ObservableObject
    {
        private RaceGroup _raceGroup;
        private int? _selectedChipGroupId;
        private int _selectedRaceLaps;

        public EditRaceGroupDialogViewModel(RaceGroup raceGroup, ObservableCollection<ChipGroup> chipGroups)
        {
            _raceGroup = raceGroup ?? throw new ArgumentNullException(nameof(raceGroup));
            ChipGroups = chipGroups ?? throw new ArgumentNullException(nameof(chipGroups));

            // 初始化选中值
            _selectedChipGroupId = raceGroup.ChipGroupId;
            _selectedRaceLaps = raceGroup.RaceLaps;

            // 初始化圈数选项（1-20）
            LapOptions = new ObservableCollection<int>();
            for (int i = 1; i <= 20; i++)
            {
                LapOptions.Add(i);
            }

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        public ObservableCollection<ChipGroup> ChipGroups { get; }
        public ObservableCollection<int> LapOptions { get; }

        public int? SelectedChipGroupId
        {
            get => _selectedChipGroupId;
            set => SetProperty(ref _selectedChipGroupId, value);
        }

        public int SelectedRaceLaps
        {
            get => _selectedRaceLaps;
            set => SetProperty(ref _selectedRaceLaps, value);
        }

        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public bool DialogResult { get; private set; }

        public event EventHandler? RequestClose;

        private void Save()
        {
            if (!SelectedChipGroupId.HasValue)
            {
                System.Windows.MessageBox.Show("请选择芯片组", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 更新 RaceGroup
            _raceGroup.ChipGroupId = SelectedChipGroupId;
            _raceGroup.RaceLaps = SelectedRaceLaps;

            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}


