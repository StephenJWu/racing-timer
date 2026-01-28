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
                    ParticipantGroupId INTEGER,
                    SequenceNumber INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    School TEXT,
                    Grade TEXT,
                    Class TEXT,
                    Name TEXT NOT NULL,
                    Gender TEXT NOT NULL CHECK(Gender IN ('男', '女')),
                    ExamNumber TEXT,
                    GroupName TEXT,
                    LabelNumber TEXT,
                    InternalNumber TEXT,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id),
                    FOREIGN KEY(ParticipantGroupId) REFERENCES ParticipantGroups(Id),
                    UNIQUE(ProjectId, SequenceNumber),
                    UNIQUE(ProjectId, ExamNumber)
                );

                CREATE INDEX IF NOT EXISTS idx_participants_name ON Participants(Name);
                CREATE INDEX IF NOT EXISTS idx_participants_exam_number ON Participants(ExamNumber);
                CREATE INDEX IF NOT EXISTS idx_participants_group_name ON Participants(GroupName);
                CREATE INDEX IF NOT EXISTS idx_participants_school ON Participants(School);
                CREATE INDEX IF NOT EXISTS idx_participants_project_id ON Participants(ProjectId);
                CREATE INDEX IF NOT EXISTS idx_participants_participantgroup_id ON Participants(ParticipantGroupId);

                CREATE TABLE IF NOT EXISTS ChipGroups (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ChipGroupName TEXT NOT NULL UNIQUE,
                    Color TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS ParticipantGroups (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER,
                    School TEXT NOT NULL,
                    Grade TEXT,
                    Class TEXT,
                    GroupName TEXT NOT NULL,
                    RaceLaps INTEGER DEFAULT 1,
                    ChipGroupId INTEGER,
                    ChipGroupName TEXT,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id),
                    FOREIGN KEY(ChipGroupId) REFERENCES ChipGroups(Id),
                    UNIQUE(ProjectId, School, Grade, Class, GroupName)
                );

                CREATE INDEX IF NOT EXISTS idx_participantgroups_projectid ON ParticipantGroups(ProjectId);
                CREATE INDEX IF NOT EXISTS idx_participantgroups_school ON ParticipantGroups(School);
                CREATE INDEX IF NOT EXISTS idx_participantgroups_chipgroupid ON ParticipantGroups(ChipGroupId);

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
                    ProjectId INTEGER NOT NULL,
                    School TEXT NOT NULL,
                    Grade TEXT,
                    Class TEXT,
                    GroupName TEXT NOT NULL,
                    ParticipantCount INTEGER NOT NULL DEFAULT 0,
                    RaceLaps INTEGER DEFAULT 1,
                    ChipGroupId INTEGER,
                    ChipGroupName TEXT,
                    Status TEXT NOT NULL DEFAULT 'Pending' CHECK(Status IN ('Pending', 'Running', 'Paused', 'Completed', 'Stopped')),
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id),
                    FOREIGN KEY(ChipGroupId) REFERENCES ChipGroups(Id)
                );

                CREATE INDEX IF NOT EXISTS idx_racegroups_projectid ON RaceGroups(ProjectId);
                CREATE INDEX IF NOT EXISTS idx_racegroups_school ON RaceGroups(School);
                CREATE INDEX IF NOT EXISTS idx_racegroups_chipgroupid ON RaceGroups(ChipGroupId);
                CREATE INDEX IF NOT EXISTS idx_racegroups_status ON RaceGroups(Status);

                CREATE TABLE IF NOT EXISTS RaceRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RaceGroupId INTEGER NOT NULL,
                    ProjectId INTEGER NOT NULL,
                    SequenceNumber INTEGER NOT NULL,
                    School TEXT NOT NULL,
                    Grade TEXT,
                    Class TEXT,
                    GroupName TEXT NOT NULL,
                    LabelNumber TEXT,
                    Name TEXT NOT NULL,
                    Gender TEXT NOT NULL,
                    Lap1Time TEXT DEFAULT '00:00:00.000',
                    Lap2Time TEXT DEFAULT '00:00:00.000',
                    TotalTime TEXT DEFAULT '00:00:00.000',
                    Status TEXT NOT NULL DEFAULT 'Pending' CHECK(Status IN ('Pending', 'Running', 'Paused', 'Completed', 'Stopped')),
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(RaceGroupId) REFERENCES RaceGroups(Id) ON DELETE CASCADE,
                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id)
                );

                CREATE INDEX IF NOT EXISTS idx_racerecords_racegroupid ON RaceRecords(RaceGroupId);
                CREATE INDEX IF NOT EXISTS idx_racerecords_projectid ON RaceRecords(ProjectId);
                CREATE INDEX IF NOT EXISTS idx_racerecords_status ON RaceRecords(Status);

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
            await MigrateParticipantsTableAsync(connection);

            // 迁移：删除 LapRecords 表，更新 RaceGroups 和 RaceRecords 表结构
            await MigrateRaceTablesAsync(connection);

            // 迁移：回填 Participants.ParticipantGroupId（便于关联查询）
            await BackfillParticipantGroupIdAsync(connection);
        }

        /// <summary>
        /// 回填 Participants.ParticipantGroupId（通过 ProjectId + School/Grade/Class/GroupName 匹配 ParticipantGroups）
        /// </summary>
        private async Task BackfillParticipantGroupIdAsync(SqliteConnection connection)
        {
            try
            {
                // 只回填为空的记录；Grade/Class 允许 NULL/空字符串等价匹配
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Participants
                    SET ParticipantGroupId = (
                        SELECT pg.Id
                        FROM ParticipantGroups pg
                        WHERE pg.ProjectId = Participants.ProjectId
                          AND pg.School = Participants.School
                          AND IFNULL(pg.Grade, '') = IFNULL(NULLIF(TRIM(Participants.Grade), ''), '')
                          AND IFNULL(pg.Class, '') = IFNULL(NULLIF(TRIM(Participants.Class), ''), '')
                          AND pg.GroupName = Participants.GroupName
                        LIMIT 1
                    )
                    WHERE ParticipantGroupId IS NULL
                      AND ProjectId IS NOT NULL
                      AND School IS NOT NULL AND TRIM(School) != ''
                      AND GroupName IS NOT NULL AND TRIM(GroupName) != ''
                ";
                var rows = await cmd.ExecuteNonQueryAsync();
                _loggingService?.Info($"[数据库迁移] 回填 Participants.ParticipantGroupId 完成，影响行数: {rows}");
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"[数据库迁移] 回填 Participants.ParticipantGroupId 失败: {ex.Message}", ex);
                // 不抛出异常，允许应用继续运行
            }
        }

        /// <summary>
        /// 迁移 Participants 表：
        /// 1) BibNumber -> LabelNumber
        /// 2) ChipNumber -> InternalNumber
        /// 3) 确保 UNIQUE(ProjectId, SequenceNumber) 和 UNIQUE(ProjectId, ExamNumber)
        /// </summary>
        private async Task MigrateParticipantsTableAsync(SqliteConnection connection)
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

                // 读取列信息
                bool hasLabelNumber = false;
                bool hasInternalNumber = false;
                bool hasBibNumber = false;
                bool hasChipNumber = false;

                var pragmaInfo = connection.CreateCommand();
                pragmaInfo.CommandText = "PRAGMA table_info(Participants)";
                using (var reader = await pragmaInfo.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var col = reader.GetString(1);
                        if (col.Equals("LabelNumber", StringComparison.OrdinalIgnoreCase)) hasLabelNumber = true;
                        if (col.Equals("InternalNumber", StringComparison.OrdinalIgnoreCase)) hasInternalNumber = true;
                        if (col.Equals("BibNumber", StringComparison.OrdinalIgnoreCase)) hasBibNumber = true;
                        if (col.Equals("ChipNumber", StringComparison.OrdinalIgnoreCase)) hasChipNumber = true;
                    }
                }

                // 读取建表 SQL 以判断 UNIQUE 约束是否正确
                var pragmaCommand = connection.CreateCommand();
                pragmaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='Participants'";
                var createTableSql = await pragmaCommand.ExecuteScalarAsync() as string ?? "";

                bool hasCompositeUnique = createTableSql.Contains("UNIQUE(ProjectId, SequenceNumber)") &&
                                         createTableSql.Contains("UNIQUE(ProjectId, ExamNumber)");

                // 如果字段和约束都正确，则无需迁移
                if (hasLabelNumber && hasInternalNumber && hasCompositeUnique)
                {
                    _loggingService?.Debug("[数据库迁移] Participants 表结构已正确（LabelNumber/InternalNumber + 复合 UNIQUE），无需迁移");
                    return;
                }

                _loggingService?.Info("[数据库迁移] 开始迁移 Participants 表：字段改名 + 复合 UNIQUE 约束");

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
                            ParticipantGroupId INTEGER,
                            SequenceNumber INTEGER NOT NULL,
                            Date TEXT NOT NULL,
                            School TEXT,
                            Grade TEXT,
                            Class TEXT,
                            Name TEXT NOT NULL,
                            Gender TEXT NOT NULL CHECK(Gender IN ('男', '女')),
                            ExamNumber TEXT,
                            GroupName TEXT,
                            LabelNumber TEXT,
                            InternalNumber TEXT,
                            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY(ProjectId) REFERENCES Projects(Id),
                            FOREIGN KEY(ParticipantGroupId) REFERENCES ParticipantGroups(Id),
                            UNIQUE(ProjectId, SequenceNumber),
                            UNIQUE(ProjectId, ExamNumber)
                        )
                    ";
                    await createNewTableCommand.ExecuteNonQueryAsync();

                    // 复制数据（兼容旧字段名 BibNumber/ChipNumber）
                    var labelExpr = hasLabelNumber ? "LabelNumber" : (hasBibNumber ? "BibNumber" : "NULL");
                    var internalExpr = hasInternalNumber ? "InternalNumber" : (hasChipNumber ? "ChipNumber" : "NULL");
                    var participantGroupExpr = createTableSql.Contains("ParticipantGroupId", StringComparison.OrdinalIgnoreCase)
                        ? "ParticipantGroupId"
                        : "NULL";

                    var copyDataCommand = connection.CreateCommand();
                    copyDataCommand.CommandText = $@"
                        INSERT INTO Participants_new 
                        (Id, ProjectId, ParticipantGroupId, SequenceNumber, Date, School, Grade, Class, Name, Gender, ExamNumber, GroupName, LabelNumber, InternalNumber, CreatedAt, UpdatedAt)
                        SELECT Id, ProjectId, {participantGroupExpr}, SequenceNumber, Date, School, Grade, Class, Name, Gender, ExamNumber, GroupName, {labelExpr}, {internalExpr}, CreatedAt, UpdatedAt
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
                        CREATE INDEX IF NOT EXISTS idx_participants_participantgroup_id ON Participants(ParticipantGroupId);
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
        /// 迁移 ChipGroups 表：将 GroupName 重命名为 ChipGroupName
        /// </summary>
        private async Task MigrateChipGroupsTableAsync(SqliteConnection connection)
        {
            try
            {
                // 检查表是否存在
                var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='ChipGroups'
                ";
                var tableExists = await checkTableCommand.ExecuteScalarAsync() != null;

                if (!tableExists)
                {
                    return; // 表不存在，无需迁移
                }

                // 检查表结构
                var pragmaCommand = connection.CreateCommand();
                pragmaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='ChipGroups'";
                var createTableSql = await pragmaCommand.ExecuteScalarAsync() as string;

                if (string.IsNullOrEmpty(createTableSql))
                {
                    return;
                }

                // 检查是否需要迁移（如果已经有ChipGroupName字段，则不需要迁移）
                bool hasChipGroupName = createTableSql.Contains("ChipGroupName");
                bool hasGroupName = createTableSql.Contains("GroupName");

                if (hasChipGroupName || !hasGroupName)
                {
                    _loggingService?.Debug("[数据库迁移] ChipGroups 表结构已正确，无需迁移");
                    return;
                }

                _loggingService?.Info("[数据库迁移] 开始迁移 ChipGroups 表：将 GroupName 重命名为 ChipGroupName");

                // 开始事务
                var beginTransactionCommand = connection.CreateCommand();
                beginTransactionCommand.CommandText = "BEGIN TRANSACTION";
                await beginTransactionCommand.ExecuteNonQueryAsync();

                try
                {
                    // 创建新表（使用ChipGroupName）
                    var createNewTableCommand = connection.CreateCommand();
                    createNewTableCommand.CommandText = @"
                        CREATE TABLE ChipGroups_new (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ChipGroupName TEXT NOT NULL UNIQUE,
                            Color TEXT NOT NULL,
                            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                        )
                    ";
                    await createNewTableCommand.ExecuteNonQueryAsync();

                    // 复制数据
                    var copyDataCommand = connection.CreateCommand();
                    copyDataCommand.CommandText = @"
                        INSERT INTO ChipGroups_new (Id, ChipGroupName, Color, CreatedAt, UpdatedAt)
                        SELECT Id, GroupName, Color, CreatedAt, UpdatedAt
                        FROM ChipGroups
                    ";
                    await copyDataCommand.ExecuteNonQueryAsync();

                    // 删除旧表
                    var dropOldTableCommand = connection.CreateCommand();
                    dropOldTableCommand.CommandText = "DROP TABLE ChipGroups";
                    await dropOldTableCommand.ExecuteNonQueryAsync();

                    // 重命名新表
                    var renameTableCommand = connection.CreateCommand();
                    renameTableCommand.CommandText = "ALTER TABLE ChipGroups_new RENAME TO ChipGroups";
                    await renameTableCommand.ExecuteNonQueryAsync();

                    // 提交事务
                    var commitCommand = connection.CreateCommand();
                    commitCommand.CommandText = "COMMIT";
                    await commitCommand.ExecuteNonQueryAsync();

                    _loggingService?.Info("[数据库迁移] 成功迁移 ChipGroups 表结构");
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    var rollbackCommand = connection.CreateCommand();
                    rollbackCommand.CommandText = "ROLLBACK";
                    await rollbackCommand.ExecuteNonQueryAsync();
                    _loggingService?.Error($"[数据库迁移] 迁移 ChipGroups 表失败: {ex.Message}", ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"[数据库迁移] 迁移 ChipGroups 表失败: {ex.Message}", ex);
                // 不抛出异常，允许应用继续运行
            }
        }

        /// <summary>
        /// 迁移比赛相关表：删除 LapRecords，更新 RaceGroups 和 RaceRecords 表结构
        /// </summary>
        private async Task MigrateRaceTablesAsync(SqliteConnection connection)
        {
            try
            {
                // 1. 删除 LapRecords 表（如果存在）
                var checkLapRecordsTable = connection.CreateCommand();
                checkLapRecordsTable.CommandText = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='LapRecords'
                ";
                var lapRecordsTableExists = await checkLapRecordsTable.ExecuteScalarAsync() != null;

                if (lapRecordsTableExists)
                {
                    _loggingService?.Info("[数据库迁移] 开始删除 LapRecords 表");
                    var dropLapRecordsCommand = connection.CreateCommand();
                    dropLapRecordsCommand.CommandText = "DROP TABLE IF EXISTS LapRecords";
                    await dropLapRecordsCommand.ExecuteNonQueryAsync();
                    _loggingService?.Info("[数据库迁移] 已删除 LapRecords 表");
                }

                // 2. 迁移 ChipGroups 表：将 GroupName 重命名为 ChipGroupName
                await MigrateChipGroupsTableAsync(connection);

                // 3. 创建ParticipantGroups表（如果不存在）
                // 注意：表创建在CreateTablesAsync中已经完成，这里只需要确保字段存在

                // 4. 为 RaceGroups 表添加新字段
                await AddColumnIfNotExistsAsync(connection, "RaceGroups", "ParticipantCount", "INTEGER DEFAULT 0");
                await AddColumnIfNotExistsAsync(connection, "RaceGroups", "Status", "TEXT DEFAULT 'Pending'");
                await AddColumnIfNotExistsAsync(connection, "RaceGroups", "ChipGroupName", "TEXT");
                
                // 5. 移除RaceGroups表的ChipId字段（如果存在）
                await RemoveChipIdFromRaceGroupsAsync(connection);

                // 3. 检查 RaceRecords 表结构，如果需要则重建
                var checkRaceRecordsTable = connection.CreateCommand();
                checkRaceRecordsTable.CommandText = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='RaceRecords'
                ";
                var raceRecordsTableExists = await checkRaceRecordsTable.ExecuteScalarAsync() != null;

                if (raceRecordsTableExists)
                {
                    // 检查表结构，看是否需要迁移
                    var pragmaCommand = connection.CreateCommand();
                    pragmaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='RaceRecords'";
                    var createTableSql = await pragmaCommand.ExecuteScalarAsync() as string;

                    // 如果表结构是旧版本（有 RaceGroupId, StartTime, EndTime 等），需要重建
                    bool needsMigration = createTableSql != null && 
                                         (createTableSql.Contains("StartTime") || 
                                          createTableSql.Contains("EndTime") ||
                                          !createTableSql.Contains("ProjectId") ||
                                          !createTableSql.Contains("SequenceNumber"));

                    if (needsMigration)
                    {
                        _loggingService?.Info("[数据库迁移] 开始迁移 RaceRecords 表结构");

                        // 开始事务
                        var beginTransactionCommand = connection.CreateCommand();
                        beginTransactionCommand.CommandText = "BEGIN TRANSACTION";
                        await beginTransactionCommand.ExecuteNonQueryAsync();

                        try
                        {
                            // 删除旧表
                            var dropOldTableCommand = connection.CreateCommand();
                            dropOldTableCommand.CommandText = "DROP TABLE RaceRecords";
                            await dropOldTableCommand.ExecuteNonQueryAsync();

                            // 创建新表
                            var createNewTableCommand = connection.CreateCommand();
                            createNewTableCommand.CommandText = @"
                                CREATE TABLE RaceRecords (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    RaceGroupId INTEGER NOT NULL,
                                    ProjectId INTEGER NOT NULL,
                                    SequenceNumber INTEGER NOT NULL,
                                    School TEXT NOT NULL,
                                    Grade TEXT,
                                    Class TEXT,
                                    GroupName TEXT NOT NULL,
                                    LabelNumber TEXT,
                                    Name TEXT NOT NULL,
                                    Gender TEXT NOT NULL,
                                    Lap1Time TEXT DEFAULT '00:00:00.000',
                                    Lap2Time TEXT DEFAULT '00:00:00.000',
                                    TotalTime TEXT DEFAULT '00:00:00.000',
                                    Status TEXT NOT NULL DEFAULT 'Pending' CHECK(Status IN ('Pending', 'Running', 'Paused', 'Completed', 'Stopped')),
                                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    FOREIGN KEY(RaceGroupId) REFERENCES RaceGroups(Id) ON DELETE CASCADE,
                                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id)
                                )
                            ";
                            await createNewTableCommand.ExecuteNonQueryAsync();

                            // 创建索引
                            var createIndexesCommand = connection.CreateCommand();
                            createIndexesCommand.CommandText = @"
                                CREATE INDEX IF NOT EXISTS idx_racerecords_racegroupid ON RaceRecords(RaceGroupId);
                                CREATE INDEX IF NOT EXISTS idx_racerecords_projectid ON RaceRecords(ProjectId);
                                CREATE INDEX IF NOT EXISTS idx_racerecords_status ON RaceRecords(Status);
                            ";
                            await createIndexesCommand.ExecuteNonQueryAsync();

                            // 提交事务
                            var commitCommand = connection.CreateCommand();
                            commitCommand.CommandText = "COMMIT";
                            await commitCommand.ExecuteNonQueryAsync();

                            _loggingService?.Info("[数据库迁移] 成功迁移 RaceRecords 表结构");
                        }
                        catch (Exception ex)
                        {
                            // 回滚事务
                            var rollbackCommand = connection.CreateCommand();
                            rollbackCommand.CommandText = "ROLLBACK";
                            await rollbackCommand.ExecuteNonQueryAsync();
                            _loggingService?.Error($"[数据库迁移] 迁移 RaceRecords 表失败: {ex.Message}", ex);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"[数据库迁移] 迁移比赛相关表失败: {ex.Message}", ex);
                // 不抛出异常，允许应用继续运行
            }
        }

        /// <summary>
        /// 移除RaceGroups表的ChipId字段（如果存在）
        /// </summary>
        private async Task RemoveChipIdFromRaceGroupsAsync(SqliteConnection connection)
        {
            try
            {
                // 检查表是否存在
                var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='RaceGroups'
                ";
                var tableExists = await checkTableCommand.ExecuteScalarAsync() != null;

                if (!tableExists)
                {
                    return; // 表不存在，无需迁移
                }

                // 检查表结构
                var pragmaCommand = connection.CreateCommand();
                pragmaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='RaceGroups'";
                var createTableSql = await pragmaCommand.ExecuteScalarAsync() as string;

                if (string.IsNullOrEmpty(createTableSql))
                {
                    return;
                }

                // 检查是否有ChipId字段
                bool hasChipId = createTableSql.Contains("ChipId INTEGER");
                bool hasChipIdForeignKey = createTableSql.Contains("FOREIGN KEY(ChipId) REFERENCES Chips(Id)");

                if (!hasChipId)
                {
                    _loggingService?.Debug("[数据库迁移] RaceGroups 表没有ChipId字段，无需迁移");
                    return;
                }

                _loggingService?.Info("[数据库迁移] 开始移除 RaceGroups 表的 ChipId 字段");

                // 开始事务
                var beginTransactionCommand = connection.CreateCommand();
                beginTransactionCommand.CommandText = "BEGIN TRANSACTION";
                await beginTransactionCommand.ExecuteNonQueryAsync();

                try
                {
                    // 创建新表（不包含ChipId）
                    var createNewTableCommand = connection.CreateCommand();
                    createNewTableCommand.CommandText = @"
                        CREATE TABLE RaceGroups_new (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ProjectId INTEGER NOT NULL,
                            School TEXT NOT NULL,
                            Grade TEXT,
                            Class TEXT,
                            GroupName TEXT NOT NULL,
                            ParticipantCount INTEGER NOT NULL DEFAULT 0,
                            RaceLaps INTEGER DEFAULT 1,
                            ChipGroupId INTEGER,
                            ChipGroupName TEXT,
                            Status TEXT NOT NULL DEFAULT 'Pending' CHECK(Status IN ('Pending', 'Running', 'Paused', 'Completed', 'Stopped')),
                            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY(ProjectId) REFERENCES Projects(Id),
                            FOREIGN KEY(ChipGroupId) REFERENCES ChipGroups(Id)
                        )
                    ";
                    await createNewTableCommand.ExecuteNonQueryAsync();

                    // 复制数据（排除ChipId字段）
                    var copyDataCommand = connection.CreateCommand();
                    copyDataCommand.CommandText = @"
                        INSERT INTO RaceGroups_new 
                        (Id, ProjectId, School, Grade, Class, GroupName, ParticipantCount, RaceLaps, ChipGroupId, ChipGroupName, Status, CreatedAt, UpdatedAt)
                        SELECT Id, ProjectId, School, Grade, Class, GroupName, ParticipantCount, RaceLaps, ChipGroupId, ChipGroupName, Status, CreatedAt, UpdatedAt
                        FROM RaceGroups
                    ";
                    await copyDataCommand.ExecuteNonQueryAsync();

                    // 删除旧表
                    var dropOldTableCommand = connection.CreateCommand();
                    dropOldTableCommand.CommandText = "DROP TABLE RaceGroups";
                    await dropOldTableCommand.ExecuteNonQueryAsync();

                    // 重命名新表
                    var renameTableCommand = connection.CreateCommand();
                    renameTableCommand.CommandText = "ALTER TABLE RaceGroups_new RENAME TO RaceGroups";
                    await renameTableCommand.ExecuteNonQueryAsync();

                    // 重新创建索引
                    var createIndexesCommand = connection.CreateCommand();
                    createIndexesCommand.CommandText = @"
                        CREATE INDEX IF NOT EXISTS idx_racegroups_projectid ON RaceGroups(ProjectId);
                        CREATE INDEX IF NOT EXISTS idx_racegroups_school ON RaceGroups(School);
                        CREATE INDEX IF NOT EXISTS idx_racegroups_chipgroupid ON RaceGroups(ChipGroupId);
                        CREATE INDEX IF NOT EXISTS idx_racegroups_status ON RaceGroups(Status);
                    ";
                    await createIndexesCommand.ExecuteNonQueryAsync();

                    // 提交事务
                    var commitCommand = connection.CreateCommand();
                    commitCommand.CommandText = "COMMIT";
                    await commitCommand.ExecuteNonQueryAsync();

                    _loggingService?.Info("[数据库迁移] 成功移除 RaceGroups 表的 ChipId 字段");
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    var rollbackCommand = connection.CreateCommand();
                    rollbackCommand.CommandText = "ROLLBACK";
                    await rollbackCommand.ExecuteNonQueryAsync();
                    _loggingService?.Error($"[数据库迁移] 移除 RaceGroups 表的 ChipId 字段失败: {ex.Message}", ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"[数据库迁移] 移除 RaceGroups 表的 ChipId 字段失败: {ex.Message}", ex);
                // 不抛出异常，允许应用继续运行
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

