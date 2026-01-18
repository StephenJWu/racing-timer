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
        private SqliteConnection? _connection;
        private bool _disposed = false;

        /// <summary>
        /// 初始化数据库上下文
        /// </summary>
        /// <param name="databasePath">数据库文件路径</param>
        public DatabaseContext(string databasePath)
        {
            // 确保数据库目录存在
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={databasePath}";
        }

        /// <summary>
        /// 获取数据库连接
        /// </summary>
        public async Task<SqliteConnection> GetConnectionAsync()
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection(_connectionString);
                await _connection.OpenAsync();
            }
            return _connection;
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
                    SequenceNumber INTEGER NOT NULL UNIQUE,
                    Date TEXT NOT NULL,
                    School TEXT,
                    Grade TEXT,
                    Class TEXT,
                    Name TEXT NOT NULL,
                    Gender TEXT NOT NULL CHECK(Gender IN ('男', '女')),
                    ExamNumber TEXT UNIQUE,
                    GroupName TEXT,
                    BibNumber TEXT,
                    ChipNumber TEXT,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS idx_participants_name ON Participants(Name);
                CREATE INDEX IF NOT EXISTS idx_participants_exam_number ON Participants(ExamNumber);
                CREATE INDEX IF NOT EXISTS idx_participants_group_name ON Participants(GroupName);
                CREATE INDEX IF NOT EXISTS idx_participants_school ON Participants(School);

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
                    School TEXT NOT NULL,
                    Grade TEXT,
                    Class TEXT,
                    GroupName TEXT NOT NULL,
                    ChipGroupId INTEGER,
                    RaceLaps INTEGER DEFAULT 1,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(ChipGroupId) REFERENCES ChipGroups(Id),
                    UNIQUE(School, Grade, Class, GroupName)
                );

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
            ";

            await command.ExecuteNonQueryAsync();
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

