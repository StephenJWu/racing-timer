using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Timer.Data;

namespace Timer
{
    public partial class App : Application
    {
        private static DatabaseContext? _databaseContext;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 统一 DatePicker 等控件的短日期显示格式为 yyyy-MM-dd
            var culture = (CultureInfo)CultureInfo.GetCultureInfo("zh-CN").Clone();
            culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            culture.DateTimeFormat.DateSeparator = "-";
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // 同步设置当前 UI 线程文化（避免已创建线程仍使用旧文化）
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // WPF 绑定/格式化还会受 FrameworkElement.Language 影响，这里统一覆盖为 zh-CN
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            base.OnStartup(e);
        }

        /// <summary>
        /// 获取数据库上下文实例
        /// </summary>
        public static DatabaseContext GetDatabaseContext()
        {
            if (_databaseContext == null)
            {
                // 使用应用程序所在目录的data文件夹（适用于安装目录）
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var databasePath = Path.Combine(baseDirectory, "data", "timer.db");
                
                _databaseContext = new DatabaseContext(databasePath);
                // 同步初始化数据库表（确保在UI线程之前完成）
                _databaseContext.CreateTablesAsync().Wait();
            }
            return _databaseContext;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _databaseContext?.Dispose();
            base.OnExit(e);
        }
    }
}

