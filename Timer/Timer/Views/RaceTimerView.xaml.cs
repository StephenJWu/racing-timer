using System.Windows.Controls;
using Timer.ViewModels;

namespace Timer.Views
{
    public partial class RaceTimerView : UserControl
    {
        public RaceTimerView()
        {
            InitializeComponent();
        }

        public RaceTimerView(RaceTimerViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
