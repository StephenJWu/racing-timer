using System.Windows;
using System.Windows.Controls;
using Timer.ViewModels;

namespace Timer.Views
{
    public partial class RaceTimerView : UserControl
    {
        public RaceTimerView()
        {
            InitializeComponent();
            Loaded += RaceTimerView_Loaded;
        }

        public RaceTimerView(RaceTimerViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private async void RaceTimerView_Loaded(object sender, RoutedEventArgs e)
        {
            // 每次页面加载时刷新分组数据
            if (DataContext is RaceTimerViewModel viewModel)
            {
                await viewModel.RefreshAsync();
            }
        }
    }
}
