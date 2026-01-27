using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Timer.Services;

namespace Timer.Data
{
    /// <summary>
    /// 数据库上下文，负责SQLite连接和表创建
    /// </summary>
    public class DatabaseContext : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private readonly ILoggingService? _loggingService;
        private SqliteConnection? _connection;
        private bool _disposed = false;

        /// <summary>
        /// 初始化数据库上下文
        /// </summary>
        /// <param name="databasePath">数据库文件路径</param>
        /// <param name="loggingService">日志服务（可选）</param>
        public DatabaseContext(string databasePath, ILoggingService? loggingService = null)
        {
            _databasePath = databasePath;
            _loggingService = loggingService;
            
            try
            {
                // 确保数据库目录存在
                var directory = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _loggingService?.Debug($"[数据库] 创建数据库目录: {directory}");
                }

                _connectionString = $"Data Source={databasePath}";
                _loggingService?.Debug($"[数据库] 初始化数据库上下文: {databasePath}");
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"[数据库异常] 初始化数据库上下文失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 获取数据库连接
        /// </summary>
        public async Task<SqliteConnection> GetConnectionAsync()
        {
            try
            {
                if (_connection == null)
                {
                    _loggingService?.Debug($"[数据库] 建立新连接: {_databasePath}");
                    _connection = new SqliteConnection(_connectionString);
                    await _connection.OpenAsync();
                    _loggingService?.Debug("[数据库] 连接已建立");
                }
                return _connection;
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"[数据库异常] 获取数据库连接失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 创建数据库表
        /// </summary>
        public async Task CreateTablesAsync()
        {
            var connection = await GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Participants (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER,
                    SequenceNumber INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    School TEXT,
                    Grade TEXT,
                    Class TEXT,
                    Name TEXT NOT NULL,
                    Gender TEXT NOT NULL CHECK(Gender IN ('男', '女')),
                    ExamNumber TEXT,
                    GroupName TEXT,
                    BibNumber TEXT,
                    ChipNumber TEXT,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id),
                    UNIQUE(ProjectId, SequenceNumber),
                    UNIQUE(ProjectId, ExamNumber)
                );

                CREATE INDEX IF NOT EXISTS idx_participants_name ON Participants(Name);
                CREATE INDEX IF NOT EXISTS idx_participants_exam_number ON Participants(ExamNumber);
                CREATE INDEX IF NOT EXISTS idx_participants_group_name ON Participants(GroupName);
                CREATE INDEX IF NOT EXISTS idx_participants_school ON Participants(School);
                CREATE INDEX IF NOT EXISTS idx_participants_project_id ON Participants(ProjectId);

                CREATE TABLE IF NOT EXISTS ChipGroups (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GroupName TEXT NOT NULL UNIQUE,
                    Color TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Chips (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ChipGroupId INTEGER NOT NULL,
                    LabelNumber TEXT NOT NULL UNIQUE,
                    InternalNumber TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(ChipGroupId) REFERENCES ChipGroups(Id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_chips_chipgroupid ON Chips(ChipGroupId);
                CREATE INDEX IF NOT EXISTS idx_chips_labelnumber ON Chips(LabelNumber);

                CREATE TABLE IF NOT EXISTS RaceGroups (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER,
                    School TEXT NOT NULL,
                    Grade TEXT,
                    Class TEXT,
                    GroupName TEXT NOT NULL,
                    ChipGroupId INTEGER,
                    RaceLaps INTEGER DEFAULT 1,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id),
                    FOREIGN KEY(ChipGroupId) REFERENCES ChipGroups(Id),
                    UNIQUE(School, Grade, Class, GroupName)
                );

                CREATE INDEX IF NOT EXISTS idx_racegroups_projectid ON RaceGroups(ProjectId);
                CREATE INDEX IF NOT EXISTS idx_racegroups_school ON RaceGroups(School);
                CREATE INDEX IF NOT EXISTS idx_racegroups_chipgroupid ON RaceGroups(ChipGroupId);

                CREATE TABLE IF NOT EXISTS RaceRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RaceGroupId INTEGER NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    Status TEXT NOT NULL CHECK(Status IN ('Running', 'Paused', 'Completed', 'Stopped')),
                    TotalLaps INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(RaceGroupId) REFERENCES RaceGroups(Id)
                );

                CREATE INDEX IF NOT EXISTS idx_racerecords_racegroupid ON RaceRecords(RaceGroupId);
                CREATE INDEX IF NOT EXISTS idx_racerecords_status ON RaceRecords(Status);

                CREATE TABLE IF NOT EXISTS LapRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RaceRecordId INTEGER NOT NULL,
                    ParticipantId INTEGER NOT NULL,
                    ChipNumber TEXT,
                    LapNumber INTEGER NOT NULL,
                    PassTime TEXT NOT NULL,
                    LapTime INTEGER NOT NULL,
                    TotalTime INTEGER NOT NULL,
                    Rank INTEGER,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(RaceRecordId) REFERENCES RaceRecords(Id) ON DELETE CASCADE,
                    FOREIGN KEY(ParticipantId) REFERENCES Participants(Id)
                );

                CREATE INDEX IF NOT EXISTS idx_laprecords_racerecordid ON LapRecords(RaceRecordId);
                CREATE INDEX IF NOT EXISTS idx_laprecords_participantid ON LapRecords(ParticipantId);
                CREATE INDEX IF NOT EXISTS idx_laprecords_chipnumber ON LapRecords(ChipNumber);

                CREATE TABLE IF NOT EXISTS Projects (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectDate TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'Normal',
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS idx_projects_date ON Projects(ProjectDate);
                CREATE INDEX IF NOT EXISTS idx_projects_name ON Projects(Name);
                CREATE INDEX IF NOT EXISTS idx_projects_status ON Projects(Status);
            ";

            await command.ExecuteNonQueryAsync();

            // 升级现有表结构：为 Projects 表添加 Status 列（如果不存在）
            // 注意：ALTER TABLE ADD COLUMN 不支持 NOT NULL（除非有默认值），这里使用 TEXT DEFAULT 'Normal'
            await AddColumnIfNotExistsAsync(connection, "Projects", "Status", "TEXT DEFAULT 'Normal'");

            // 升级现有表结构：为 Participants 表添加 ProjectId 列（如果不存在）
            await AddColumnIfNotExistsAsync(connection, "Participants", "ProjectId", "INTEGER");

            // 升级现有表结构：为 RaceGroups 表添加 ProjectId 列（如果不存在）
            await AddColumnIfNotExistsAsync(connection, "RaceGroups", "ProjectId", "INTEGER");

            // 迁移：删除 Participants 表的 SequenceNumber 和 ExamNumber 的 UNIQUE 约束
            await RemoveUniqueConstraintsFromParticipantsAsync(connection);
        }

        /// <summary>
        /// 迁移 Participants 表：删除单字段 UNIQUE 约束，添加复合 UNIQUE 约束
        /// </summary>
        private async Task RemoveUniqueConstraintsFromParticipantsAsync(SqliteConnection connection)
        {
            try
            {
                // 检查表是否存在
                var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='Participants'
                ";
                var tableExists = await checkTableCommand.ExecuteScalarAsync() != null;

                if (!tableExists)
                {
                    return; // 表不存在，无需迁移
                }

                // 检查表结构
                var pragmaCommand = connection.CreateCommand();
                pragmaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='Participants'";
                var createTableSql = await pragmaCommand.ExecuteScalarAsync() as string;

                if (string.IsNullOrEmpty(createTableSql))
                {
                    return;
                }

                // 检查是否需要迁移
                // 1. 检查是否有单字段 UNIQUE 约束（需要删除）
                bool hasSingleFieldUnique = createTableSql.Contains("SequenceNumber INTEGER NOT NULL UNIQUE") ||
                                          createTableSql.Contains("SequenceNumber INTEGER UNIQUE") ||
                                          createTableSql.Contains("ExamNumber TEXT UNIQUE");
                
                // 2. 检查是否有复合 UNIQUE 约束（需要添加）
                bool hasCompositeUnique = createTableSql.Contains("UNIQUE(ProjectId, SequenceNumber)") &&
                                         createTableSql.Contains("UNIQUE(ProjectId, ExamNumber)");

                // 如果表结构已经正确（有复合约束且没有单字段约束），则无需迁移
                if (!hasSingleFieldUnique && hasCompositeUnique)
                {
                    _loggingService?.Debug("[数据库迁移] Participants 表结构已正确，无需迁移");
                    return;
                }

                _loggingService?.Info("[数据库迁移] 开始迁移 Participants 表：添加复合 UNIQUE 约束");

                // 开始事务
                var beginTransactionCommand = connection.CreateCommand();
                beginTransactionCommand.CommandText = "BEGIN TRANSACTION";
                await beginTransactionCommand.ExecuteNonQueryAsync();

                try
                {
                    // 创建新表（带复合 UNIQUE 约束）
                    var createNewTableCommand = connection.CreateCommand();
                    createNewTableCommand.CommandText = @"
                        CREATE TABLE Participants_new (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ProjectId INTEGER,
                            SequenceNumber INTEGER NOT NULL,
                            Date TEXT NOT NULL,
                            School TEXT,
                            Grade TEXT,
                            Class TEXT,
                            Name TEXT NOT NULL,
                            Gender TEXT NOT NULL CHECK(Gender IN ('男', '女')),
                            ExamNumber TEXT,
                            GroupName TEXT,
                            BibNumber TEXT,
                            ChipNumber TEXT,
                            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY(ProjectId) REFERENCES Projects(Id),
                            UNIQUE(ProjectId, SequenceNumber),
                            UNIQUE(ProjectId, ExamNumber)
                        )
                    ";
                    await createNewTableCommand.ExecuteNonQueryAsync();

                    // 复制数据（如果存在重复数据，会失败，这是预期的）
                    var copyDataCommand = connection.CreateCommand();
                    copyDataCommand.CommandText = @"
                        INSERT INTO Participants_new 
                        (Id, ProjectId, SequenceNumber, Date, School, Grade, Class, Name, Gender, ExamNumber, GroupName, BibNumber, ChipNumber, CreatedAt, UpdatedAt)
                        SELECT Id, ProjectId, SequenceNumber, Date, School, Grade, Class, Name, Gender, ExamNumber, GroupName, BibNumber, ChipNumber, CreatedAt, UpdatedAt
                        FROM Participants
                    ";
                    await copyDataCommand.ExecuteNonQueryAsync();

                    // 删除旧表
                    var dropOldTableCommand = connection.CreateCommand();
                    dropOldTableCommand.CommandText = "DROP TABLE Participants";
                    await dropOldTableCommand.ExecuteNonQueryAsync();

                    // 重命名新表
                    var renameTableCommand = connection.CreateCommand();
                    renameTableCommand.CommandText = "ALTER TABLE Participants_new RENAME TO Participants";
                    await renameTableCommand.ExecuteNonQueryAsync();

                    // 重新创建索引
                    var recreateIndexesCommand = connection.CreateCommand();
                    recreateIndexesCommand.CommandText = @"
                        CREATE INDEX IF NOT EXISTS idx_participants_name ON Participants(Name);
                        CREATE INDEX IF NOT EXISTS idx_participants_exam_number ON Participants(ExamNumber);
                        CREATE INDEX IF NOT EXISTS idx_participants_group_name ON Participants(GroupName);
                        CREATE INDEX IF NOT EXISTS idx_participants_school ON Participants(School);
                        CREATE INDEX IF NOT EXISTS idx_participants_project_id ON Participants(ProjectId);
                    ";
                    await recreateIndexesCommand.ExecuteNonQueryAsync();

                    // 提交事务
                    var commitCommand = connection.CreateCommand();
                    commitCommand.CommandText = "COMMIT";
                    await commitCommand.ExecuteNonQueryAsync();

                    _loggingService?.Info("[数据库迁移] 成功迁移 Participants 表：已添加复合 UNIQUE 约束");
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    var rollbackCommand = connection.CreateCommand();
                    rollbackCommand.CommandText = "ROLLBACK";
                    await rollbackCommand.ExecuteNonQueryAsync();
                    _loggingService?.Error($"[数据库迁移] 迁移 Participants 表失败: {ex.Message}", ex);
                    // 如果是唯一约束冲突，说明数据有问题，需要用户先清理数据
                    if (ex.Message.Contains("UNIQUE constraint") || ex.Message.Contains("unique"))
                    {
                        _loggingService?.Error("[数据库迁移] 检测到重复数据，请先清理同一项目下的重复 SequenceNumber 或 ExamNumber");
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"[数据库迁移] 检查或迁移 Participants 表失败: {ex.Message}", ex);
                // 不抛出异常，允许应用继续运行（如果迁移失败，应用层验证仍然有效）
            }
        }

        /// <summary>
        /// 如果列不存在则添加列
        /// </summary>
        private async Task AddColumnIfNotExistsAsync(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
        {
            try
            {
                // 检查列是否存在
                using var pragmaCommand = connection.CreateCommand();
                pragmaCommand.CommandText = $"PRAGMA table_info({tableName})";
                
                bool columnExists = false;
                using (var reader = await pragmaCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var name = reader.GetString(1); // 列名在第二个位置
                        if (name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        {
                            columnExists = true;
                            break;
                        }
                    }
                }

                // 如果列不存在，添加列
                if (!columnExists)
                {
                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
                    await alterCommand.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // 忽略错误（列可能已存在或其他问题）
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
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}

