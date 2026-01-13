using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Timer.Data;

namespace Timer
{
    public partial class App : Application
    {
        private static DatabaseContext? _databaseContext;

        /// <summary>
        /// 获取数据库上下文实例
        /// </summary>
        public static DatabaseContext GetDatabaseContext()
        {
            if (_databaseContext == null)
            {
                // 使用项目根目录下的data文件夹
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
                var databasePath = Path.Combine(projectRoot, "data", "timer.db");
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

