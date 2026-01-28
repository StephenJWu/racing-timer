using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Timer.Data;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 比赛记录数据访问实现
    /// </summary>
    public class RaceRecordRepository : IRaceRecordRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILoggingService? _loggingService;

        public RaceRecordRepository(DatabaseContext dbContext, ILoggingService? loggingService = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _loggingService = loggingService;
        }

        /// <summary>
        /// 记录SQL执行日志
        /// </summary>
        private void LogSql(string operation, string sql, object? parameters = null)
        {
            var paramStr = parameters != null ? $", Params: {parameters}" : "";
            _loggingService?.Debug($"[SQL] {operation}: {sql.Trim().Replace("\n", " ").Replace("  ", " ")}{paramStr}");
        }

        /// <summary>
        /// 记录数据库异常
        /// </summary>
        private void LogDbError(string operation, Exception ex, string? sql = null)
        {
            var sqlInfo = sql != null ? $"\nSQL: {sql.Trim().Replace("\n", " ")}" : "";
            _loggingService?.Error($"[数据库异常] {operation} 失败: {ex.Message}{sqlInfo}", ex);
        }

        public async Task<RaceRecord?> GetByIdAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceGroupId, ProjectId, SequenceNumber, School, Grade, Class, GroupName, 
                       LabelNumber, Name, Gender, Lap1Time, Lap2Time, TotalTime, Status, CreatedAt, UpdatedAt
                FROM RaceRecords
                WHERE Id = @Id
            ";
            command.Parameters.AddWithValue("@Id", id);

            LogSql("GetByIdAsync", command.CommandText, $"Id={id}");

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapRaceRecord(reader);
            }

            return null;
        }

        public async Task<List<RaceRecord>> GetAllAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceGroupId, ProjectId, SequenceNumber, School, Grade, Class, GroupName, 
                       LabelNumber, Name, Gender, Lap1Time, Lap2Time, TotalTime, Status, CreatedAt, UpdatedAt
                FROM RaceRecords
                ORDER BY RaceGroupId, SequenceNumber
            ";

            LogSql("GetAllAsync", command.CommandText);

            var records = new List<RaceRecord>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(MapRaceRecord(reader));
            }

            return records;
        }

        public async Task<List<RaceRecord>> GetByRaceGroupIdAsync(int raceGroupId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceGroupId, ProjectId, SequenceNumber, School, Grade, Class, GroupName, 
                       LabelNumber, Name, Gender, Lap1Time, Lap2Time, TotalTime, Status, CreatedAt, UpdatedAt
                FROM RaceRecords
                WHERE RaceGroupId = @RaceGroupId
                ORDER BY SequenceNumber
            ";
            command.Parameters.AddWithValue("@RaceGroupId", raceGroupId);

            LogSql("GetByRaceGroupIdAsync", command.CommandText, $"RaceGroupId={raceGroupId}");

            var records = new List<RaceRecord>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(MapRaceRecord(reader));
            }

            return records;
        }

        public async Task<RaceRecord?> GetActiveRaceAsync()
        {
            // 此方法在新架构中不再适用，返回 null
            return null;
        }

        public async Task<List<RaceRecord>> GetActiveRacesAsync()
        {
            // 此方法在新架构中不再适用，返回空列表
            return new List<RaceRecord>();
        }

        public async Task<int> CreateAsync(RaceRecord raceRecord)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO RaceRecords (RaceGroupId, ProjectId, SequenceNumber, School, Grade, Class, GroupName, 
                                       LabelNumber, Name, Gender, Lap1Time, Lap2Time, TotalTime, Status, CreatedAt, UpdatedAt)
                VALUES (@RaceGroupId, @ProjectId, @SequenceNumber, @School, @Grade, @Class, @GroupName, 
                       @LabelNumber, @Name, @Gender, @Lap1Time, @Lap2Time, @TotalTime, @Status, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@RaceGroupId", raceRecord.RaceGroupId);
            command.Parameters.AddWithValue("@ProjectId", raceRecord.ProjectId);
            command.Parameters.AddWithValue("@SequenceNumber", raceRecord.SequenceNumber);
            command.Parameters.AddWithValue("@School", raceRecord.School);
            command.Parameters.AddWithValue("@Grade", raceRecord.Grade ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Class", raceRecord.Class ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@GroupName", raceRecord.GroupName);
            command.Parameters.AddWithValue("@LabelNumber", raceRecord.LabelNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Name", raceRecord.Name);
            command.Parameters.AddWithValue("@Gender", raceRecord.Gender);
            command.Parameters.AddWithValue("@Lap1Time", raceRecord.Lap1Time);
            command.Parameters.AddWithValue("@Lap2Time", raceRecord.Lap2Time);
            command.Parameters.AddWithValue("@TotalTime", raceRecord.TotalTime);
            command.Parameters.AddWithValue("@Status", raceRecord.Status.ToString());
            command.Parameters.AddWithValue("@CreatedAt", raceRecord.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@UpdatedAt", raceRecord.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            LogSql("CreateAsync", command.CommandText, $"RaceGroupId={raceRecord.RaceGroupId}, Name={raceRecord.Name}");

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// 批量创建比赛记录
        /// </summary>
        public async Task<int> CreateBatchAsync(IEnumerable<RaceRecord> raceRecords)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var count = 0;

            foreach (var raceRecord in raceRecords)
            {
                await CreateAsync(raceRecord);
                count++;
            }

            _loggingService?.Info($"批量创建比赛记录: {count} 条");
            return count;
        }

        public async Task<bool> UpdateAsync(RaceRecord raceRecord)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE RaceRecords
                SET RaceGroupId = @RaceGroupId,
                    ProjectId = @ProjectId,
                    SequenceNumber = @SequenceNumber,
                    School = @School,
                    Grade = @Grade,
                    Class = @Class,
                    GroupName = @GroupName,
                    LabelNumber = @LabelNumber,
                    Name = @Name,
                    Gender = @Gender,
                    Lap1Time = @Lap1Time,
                    Lap2Time = @Lap2Time,
                    TotalTime = @TotalTime,
                    Status = @Status,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Id", raceRecord.Id);
            command.Parameters.AddWithValue("@RaceGroupId", raceRecord.RaceGroupId);
            command.Parameters.AddWithValue("@ProjectId", raceRecord.ProjectId);
            command.Parameters.AddWithValue("@SequenceNumber", raceRecord.SequenceNumber);
            command.Parameters.AddWithValue("@School", raceRecord.School);
            command.Parameters.AddWithValue("@Grade", raceRecord.Grade ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Class", raceRecord.Class ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@GroupName", raceRecord.GroupName);
            command.Parameters.AddWithValue("@LabelNumber", raceRecord.LabelNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Name", raceRecord.Name);
            command.Parameters.AddWithValue("@Gender", raceRecord.Gender);
            command.Parameters.AddWithValue("@Lap1Time", raceRecord.Lap1Time);
            command.Parameters.AddWithValue("@Lap2Time", raceRecord.Lap2Time);
            command.Parameters.AddWithValue("@TotalTime", raceRecord.TotalTime);
            command.Parameters.AddWithValue("@Status", raceRecord.Status.ToString());
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            LogSql("UpdateAsync", command.CommandText, $"Id={raceRecord.Id}");

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM RaceRecords WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            LogSql("DeleteAsync", command.CommandText, $"Id={id}");

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteByRaceGroupIdAsync(int raceGroupId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM RaceRecords WHERE RaceGroupId = @RaceGroupId";
            command.Parameters.AddWithValue("@RaceGroupId", raceGroupId);

            LogSql("DeleteByRaceGroupIdAsync", command.CommandText, $"RaceGroupId={raceGroupId}");

            var rowsAffected = await command.ExecuteNonQueryAsync();
            _loggingService?.Info($"删除比赛记录: RaceGroupId={raceGroupId}, 影响行数={rowsAffected}");
            return rowsAffected > 0;
        }

        private RaceRecord MapRaceRecord(SqliteDataReader reader)
        {
            return new RaceRecord
            {
                Id = reader.GetInt32(0),
                RaceGroupId = reader.GetInt32(1),
                ProjectId = reader.GetInt32(2),
                SequenceNumber = reader.GetInt32(3),
                School = reader.GetString(4),
                Grade = reader.IsDBNull(5) ? null : reader.GetString(5),
                Class = reader.IsDBNull(6) ? null : reader.GetString(6),
                GroupName = reader.GetString(7),
                LabelNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                Name = reader.GetString(9),
                Gender = reader.GetString(10),
                Lap1Time = reader.IsDBNull(11) ? "00:00:00.000" : reader.GetString(11),
                Lap2Time = reader.IsDBNull(12) ? "00:00:00.000" : reader.GetString(12),
                TotalTime = reader.IsDBNull(13) ? "00:00:00.000" : reader.GetString(13),
                Status = reader.IsDBNull(14) ? RaceStatus.Pending : Enum.Parse<RaceStatus>(reader.GetString(14)),
                CreatedAt = DateTime.Parse(reader.GetString(15)),
                UpdatedAt = DateTime.Parse(reader.GetString(16))
            };
        }

        /// <summary>
        /// 查询成绩结果（从 RaceRecords 表查询）
        /// </summary>
        public async Task<List<ScoreResult>> SearchScoresAsync(ScoreSearchFilter filter)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var conditions = new List<string> { "rr.Status = 'Completed'" };

            if (filter.ProjectId.HasValue && filter.ProjectId.Value > 0)
            {
                conditions.Add("rr.ProjectId = @ProjectId");
                command.Parameters.AddWithValue("@ProjectId", filter.ProjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.School))
            {
                conditions.Add("rr.School = @School");
                command.Parameters.AddWithValue("@School", filter.School);
            }

            if (!string.IsNullOrWhiteSpace(filter.Grade))
            {
                conditions.Add("rr.Grade = @Grade");
                command.Parameters.AddWithValue("@Grade", filter.Grade);
            }

            if (!string.IsNullOrWhiteSpace(filter.Class))
            {
                conditions.Add("rr.Class = @Class");
                command.Parameters.AddWithValue("@Class", filter.Class);
            }

            if (!string.IsNullOrWhiteSpace(filter.GroupName))
            {
                conditions.Add("rr.GroupName = @GroupName");
                command.Parameters.AddWithValue("@GroupName", filter.GroupName);
            }

            var whereClause = string.Join(" AND ", conditions);
            var offset = (filter.PageNumber - 1) * filter.PageSize;

            command.CommandText = $@"
                SELECT 
                    rr.Id as RaceRecordId,
                    rr.SequenceNumber as ParticipantId,
                    DATE(rr.CreatedAt) as RaceDate,
                    rr.School,
                    rr.Grade,
                    rr.Class,
                    rr.GroupName,
                    rr.Name,
                    rr.Gender,
                    p.ExamNumber,
                    rr.LabelNumber,
                    rr.Lap1Time,
                    rr.Lap2Time,
                    rr.TotalTime
                FROM RaceRecords rr
                LEFT JOIN Participants p ON p.ProjectId = rr.ProjectId 
                    AND p.SequenceNumber = rr.SequenceNumber
                WHERE {whereClause}
                ORDER BY rr.CreatedAt DESC, rr.SequenceNumber
                LIMIT @PageSize OFFSET @Offset
            ";

            command.Parameters.AddWithValue("@PageSize", filter.PageSize);
            command.Parameters.AddWithValue("@Offset", offset);

            LogSql("SearchScoresAsync", command.CommandText, $"PageNumber={filter.PageNumber}, PageSize={filter.PageSize}");

            var results = new List<ScoreResult>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var lap1Time = reader.IsDBNull(11) ? "00:00:00.000" : reader.GetString(11);
                    var lap2Time = reader.IsDBNull(12) ? "00:00:00.000" : reader.GetString(12);
                    var totalTime = reader.IsDBNull(13) ? "00:00:00.000" : reader.GetString(13);

                    // 计算圈数
                    int totalLaps = 0;
                    if (!string.IsNullOrWhiteSpace(lap2Time) && lap2Time != "00:00:00.000")
                    {
                        totalLaps = 2;
                    }
                    else if (!string.IsNullOrWhiteSpace(lap1Time) && lap1Time != "00:00:00.000")
                    {
                        totalLaps = 1;
                    }

                    // 解析总用时为毫秒
                    long totalTimeMs = ParseTimeStringToMilliseconds(totalTime);

                    results.Add(new ScoreResult
                    {
                        RaceRecordId = reader.GetInt32(0),
                        ParticipantId = reader.GetInt32(1),
                        RaceDate = DateTime.Parse(reader.GetString(2)),
                        School = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Grade = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Class = reader.IsDBNull(5) ? null : reader.GetString(5),
                        GroupName = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Name = reader.IsDBNull(7) ? null : reader.GetString(7),
                        Gender = reader.IsDBNull(8) ? null : reader.GetString(8),
                        ExamNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
                        LabelNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                        TotalLaps = totalLaps,
                        TotalTimeMs = totalTimeMs
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// 获取成绩查询的总记录数
        /// </summary>
        public async Task<int> GetScoresTotalCountAsync(ScoreSearchFilter filter)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var conditions = new List<string> { "Status = 'Completed'" };

            if (filter.ProjectId.HasValue && filter.ProjectId.Value > 0)
            {
                conditions.Add("ProjectId = @ProjectId");
                command.Parameters.AddWithValue("@ProjectId", filter.ProjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.School))
            {
                conditions.Add("School = @School");
                command.Parameters.AddWithValue("@School", filter.School);
            }

            if (!string.IsNullOrWhiteSpace(filter.Grade))
            {
                conditions.Add("Grade = @Grade");
                command.Parameters.AddWithValue("@Grade", filter.Grade);
            }

            if (!string.IsNullOrWhiteSpace(filter.Class))
            {
                conditions.Add("Class = @Class");
                command.Parameters.AddWithValue("@Class", filter.Class);
            }

            if (!string.IsNullOrWhiteSpace(filter.GroupName))
            {
                conditions.Add("GroupName = @GroupName");
                command.Parameters.AddWithValue("@GroupName", filter.GroupName);
            }

            var whereClause = string.Join(" AND ", conditions);

            command.CommandText = $@"
                SELECT COUNT(*)
                FROM RaceRecords
                WHERE {whereClause}
            ";

            LogSql("GetScoresTotalCountAsync", command.CommandText);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// 获取成绩查询中所有学校列表
        /// </summary>
        public async Task<List<string>> GetScoreSchoolsAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT DISTINCT School
                FROM RaceRecords
                WHERE Status = 'Completed' AND School IS NOT NULL AND School != ''
                ORDER BY School
            ";

            LogSql("GetScoreSchoolsAsync", command.CommandText);

            var schools = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    schools.Add(reader.GetString(0));
                }
            }

            return schools;
        }

        /// <summary>
        /// 根据项目ID获取成绩查询中的学校列表
        /// </summary>
        public async Task<List<string>> GetScoreSchoolsByProjectAsync(int projectId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT DISTINCT School
                FROM RaceRecords
                WHERE ProjectId = @ProjectId 
                    AND Status = 'Completed' 
                    AND School IS NOT NULL 
                    AND School != ''
                ORDER BY School
            ";

            command.Parameters.AddWithValue("@ProjectId", projectId);
            LogSql("GetScoreSchoolsByProjectAsync", command.CommandText, $"ProjectId={projectId}");

            var schools = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    schools.Add(reader.GetString(0));
                }
            }

            return schools;
        }

        /// <summary>
        /// 获取指定学校下的年级列表
        /// </summary>
        public async Task<List<string>> GetScoreGradesAsync(string school)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT DISTINCT Grade
                FROM RaceRecords
                WHERE School = @School 
                    AND Status = 'Completed' 
                    AND Grade IS NOT NULL 
                    AND Grade != ''
                ORDER BY Grade
            ";

            command.Parameters.AddWithValue("@School", school);
            LogSql("GetScoreGradesAsync", command.CommandText, $"School={school}");

            var grades = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    grades.Add(reader.GetString(0));
                }
            }

            return grades;
        }

        /// <summary>
        /// 获取指定学校和年级下的班级列表
        /// </summary>
        public async Task<List<string>> GetScoreClassesAsync(string school, string grade)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT DISTINCT Class
                FROM RaceRecords
                WHERE School = @School 
                    AND Grade = @Grade 
                    AND Status = 'Completed' 
                    AND Class IS NOT NULL 
                    AND Class != ''
                ORDER BY Class
            ";

            command.Parameters.AddWithValue("@School", school);
            command.Parameters.AddWithValue("@Grade", grade);
            LogSql("GetScoreClassesAsync", command.CommandText, $"School={school}, Grade={grade}");

            var classes = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    classes.Add(reader.GetString(0));
                }
            }

            return classes;
        }

        /// <summary>
        /// 获取指定学校、年级、班级下的组别列表
        /// </summary>
        public async Task<List<string>> GetScoreGroupNamesAsync(string school, string grade, string className)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT DISTINCT GroupName
                FROM RaceRecords
                WHERE School = @School 
                    AND Grade = @Grade 
                    AND Class = @Class 
                    AND Status = 'Completed' 
                    AND GroupName IS NOT NULL 
                    AND GroupName != ''
                ORDER BY GroupName
            ";

            command.Parameters.AddWithValue("@School", school);
            command.Parameters.AddWithValue("@Grade", grade);
            command.Parameters.AddWithValue("@Class", className);
            LogSql("GetScoreGroupNamesAsync", command.CommandText, $"School={school}, Grade={grade}, Class={className}");

            var groupNames = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    groupNames.Add(reader.GetString(0));
                }
            }

            return groupNames;
        }

        /// <summary>
        /// 解析时间字符串为毫秒数
        /// </summary>
        private long ParseTimeStringToMilliseconds(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString) || timeString == "00:00:00.000")
            {
                return 0;
            }

            try
            {
                var parts = timeString.Split(':');
                if (parts.Length == 3)
                {
                    var hours = int.Parse(parts[0]);
                    var minutes = int.Parse(parts[1]);
                    var secondsAndMs = parts[2].Split('.');
                    var seconds = int.Parse(secondsAndMs[0]);
                    var milliseconds = secondsAndMs.Length > 1 ? int.Parse(secondsAndMs[1]) : 0;
                    return (long)(new TimeSpan(0, hours, minutes, seconds, milliseconds).TotalMilliseconds);
                }
            }
            catch
            {
                // 解析失败，返回零
            }

            return 0;
        }
    }
}

