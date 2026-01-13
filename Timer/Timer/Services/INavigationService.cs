namespace Timer.Services
{
    /// <summary>
    /// 导航服务接口，定义页面导航功能
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// 导航到指定ViewModel类型的页面
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel类型</typeparam>
        void NavigateTo<TViewModel>() where TViewModel : class;

        /// <summary>
        /// 导航到指定ViewModel实例的页面
        /// </summary>
        /// <param name="viewModel">ViewModel实例</param>
        void NavigateTo(object viewModel);

        /// <summary>
        /// 获取指定ViewModel对应的视图实例
        /// </summary>
        /// <param name="viewModel">ViewModel实例</param>
        /// <returns>视图实例，如果无法创建则返回null</returns>
        object? GetView(object viewModel);
    }
}

