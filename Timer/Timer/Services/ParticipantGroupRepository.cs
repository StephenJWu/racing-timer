using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Timer.Data;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 参赛人员分组配置数据访问实现
    /// </summary>
    public class ParticipantGroupRepository : IParticipantGroupRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILoggingService? _loggingService;

        public ParticipantGroupRepository(DatabaseContext dbContext, ILoggingService? loggingService = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _loggingService = loggingService;
        }

        /// <summary>
        /// 根据条件查询参赛人员分组配置
        /// </summary>
        public async Task<IEnumerable<ParticipantGroup>> QueryAsync(
            int? projectId,
            string? school,
            string? grade = null,
            string? classValue = null,
            string? groupName = null)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var whereClauses = new List<string>();
            
            if (projectId.HasValue && projectId.Value > 0)
            {
                whereClauses.Add("ProjectId = @projectId");
                command.Parameters.Add(new SqliteParameter("@projectId", projectId.Value));
            }

            if (!string.IsNullOrWhiteSpace(school))
            {
                whereClauses.Add("School = @school");
                command.Parameters.Add(new SqliteParameter("@school", school));
            }

            if (!string.IsNullOrWhiteSpace(grade))
            {
                whereClauses.Add("Grade = @grade");
                command.Parameters.Add(new SqliteParameter("@grade", grade));
            }

            if (!string.IsNullOrWhiteSpace(classValue))
            {
                whereClauses.Add("Class = @class");
                command.Parameters.Add(new SqliteParameter("@class", classValue));
            }

            if (!string.IsNullOrWhiteSpace(groupName))
            {
                whereClauses.Add("GroupName = @groupName");
                command.Parameters.Add(new SqliteParameter("@groupName", groupName));
            }

            var whereClause = whereClauses.Count > 0 
                ? "WHERE " + string.Join(" AND ", whereClauses) 
                : "";

            command.CommandText = $@"
                SELECT 
                    Id,
                    ProjectId,
                    School,
                    Grade,
                    Class,
                    GroupName,
                    RaceLaps,
                    ChipGroupId,
                    ChipGroupName,
                    CreatedAt,
                    UpdatedAt
                FROM ParticipantGroups
                {whereClause}
                ORDER BY School, Grade, Class, GroupName
            ";

            var groups = new List<ParticipantGroup>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                groups.Add(MapToParticipantGroup(reader));
            }

            return groups;
        }

        /// <summary>
        /// 根据ID获取单个分组配置
        /// </summary>
        public async Task<ParticipantGroup?> GetByIdAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT 
                    Id,
                    ProjectId,
                    School,
                    Grade,
                    Class,
                    GroupName,
                    RaceLaps,
                    ChipGroupId,
                    ChipGroupName,
                    CreatedAt,
                    UpdatedAt
                FROM ParticipantGroups
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", id));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToParticipantGroup(reader);
            }

            return null;
        }

        /// <summary>
        /// 根据分组信息获取配置（如果不存在则返回null）
        /// </summary>
        public async Task<ParticipantGroup?> GetByGroupInfoAsync(
            int? projectId,
            string school,
            string? grade,
            string? classValue,
            string groupName)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT 
                    Id,
                    ProjectId,
                    School,
                    Grade,
                    Class,
                    GroupName,
                    RaceLaps,
                    ChipGroupId,
                    ChipGroupName,
                    CreatedAt,
                    UpdatedAt
                FROM ParticipantGroups
                WHERE (ProjectId = @projectId OR (ProjectId IS NULL AND @projectId IS NULL))
                  AND School = @school
                  AND (Grade = @grade OR (Grade IS NULL AND @grade IS NULL))
                  AND (Class = @class OR (Class IS NULL AND @class IS NULL))
                  AND GroupName = @groupName
            ";

            command.Parameters.Add(new SqliteParameter("@projectId", projectId ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@school", school));
            command.Parameters.Add(new SqliteParameter("@grade", grade ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@class", classValue ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@groupName", groupName));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToParticipantGroup(reader);
            }

            return null;
        }

        /// <summary>
        /// 创建分组配置
        /// </summary>
        public async Task<int> CreateAsync(ParticipantGroup participantGroup)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO ParticipantGroups (ProjectId, School, Grade, Class, GroupName, RaceLaps, ChipGroupId, ChipGroupName, CreatedAt, UpdatedAt)
                VALUES (@projectId, @school, @grade, @class, @groupName, @raceLaps, @chipGroupId, @chipGroupName, @createdAt, @updatedAt);
                SELECT last_insert_rowid();
            ";

            command.Parameters.Add(new SqliteParameter("@projectId", participantGroup.ProjectId ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@school", participantGroup.School));
            command.Parameters.Add(new SqliteParameter("@grade", participantGroup.Grade ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@class", participantGroup.Class ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@groupName", participantGroup.GroupName));
            command.Parameters.Add(new SqliteParameter("@raceLaps", participantGroup.RaceLaps));
            command.Parameters.Add(new SqliteParameter("@chipGroupId", participantGroup.ChipGroupId ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@chipGroupName", participantGroup.ChipGroupName ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@createdAt", participantGroup.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(new SqliteParameter("@updatedAt", participantGroup.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")));

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            _loggingService?.Info($"创建参赛人员分组配置: {participantGroup.DisplayName} (ID: {id})");
            return id;
        }

        /// <summary>
        /// 更新分组配置
        /// </summary>
        public async Task<bool> UpdateAsync(ParticipantGroup participantGroup)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE ParticipantGroups 
                SET ProjectId = @projectId, 
                    School = @school, 
                    Grade = @grade, 
                    Class = @class, 
                    GroupName = @groupName,
                    RaceLaps = @raceLaps,
                    ChipGroupId = @chipGroupId,
                    ChipGroupName = @chipGroupName,
                    UpdatedAt = @updatedAt
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", participantGroup.Id));
            command.Parameters.Add(new SqliteParameter("@projectId", participantGroup.ProjectId ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@school", participantGroup.School));
            command.Parameters.Add(new SqliteParameter("@grade", participantGroup.Grade ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@class", participantGroup.Class ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@groupName", participantGroup.GroupName));
            command.Parameters.Add(new SqliteParameter("@raceLaps", participantGroup.RaceLaps));
            command.Parameters.Add(new SqliteParameter("@chipGroupId", participantGroup.ChipGroupId ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@chipGroupName", participantGroup.ChipGroupName ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// 删除分组配置
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM ParticipantGroups WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            _loggingService?.Info($"删除参赛人员分组配置: ID={id}, 影响行数={rowsAffected}");
            return rowsAffected > 0;
        }

        /// <summary>
        /// 获取或创建分组配置（如果不存在则创建）
        /// </summary>
        public async Task<ParticipantGroup> GetOrCreateAsync(
            int? projectId,
            string school,
            string? grade,
            string? classValue,
            string groupName)
        {
            var existing = await GetByGroupInfoAsync(projectId, school, grade, classValue, groupName);
            if (existing != null)
            {
                return existing;
            }

            var newGroup = new ParticipantGroup
            {
                ProjectId = projectId,
                School = school,
                Grade = grade,
                Class = classValue,
                GroupName = groupName,
                RaceLaps = 1,
                ChipGroupId = null,
                ChipGroupName = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            newGroup.Id = await CreateAsync(newGroup);
            return newGroup;
        }

        /// <summary>
        /// 从数据读取器映射到ParticipantGroup对象
        /// </summary>
        private ParticipantGroup MapToParticipantGroup(SqliteDataReader reader)
        {
            return new ParticipantGroup
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                School = reader.GetString(2),
                Grade = reader.IsDBNull(3) ? null : reader.GetString(3),
                Class = reader.IsDBNull(4) ? null : reader.GetString(4),
                GroupName = reader.GetString(5),
                RaceLaps = reader.IsDBNull(6) ? 1 : reader.GetInt32(6),
                ChipGroupId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                ChipGroupName = reader.IsDBNull(8) ? null : reader.GetString(8),
                CreatedAt = DateTime.Parse(reader.GetString(9)),
                UpdatedAt = DateTime.Parse(reader.GetString(10))
            };
        }

        /// <summary>
        /// 根据项目ID获取所有不同的学校列表
        /// </summary>
        public async Task<List<string>> GetDistinctSchoolsByProjectAsync(int? projectId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var whereClause = projectId.HasValue && projectId.Value > 0
                ? "WHERE ProjectId = @projectId"
                : "";

            if (projectId.HasValue && projectId.Value > 0)
            {
                command.Parameters.Add(new SqliteParameter("@projectId", projectId.Value));
            }

            command.CommandText = $@"
                SELECT DISTINCT School
                FROM ParticipantGroups
                {whereClause}
                ORDER BY School
            ";

            var schools = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var school = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(school))
                {
                    schools.Add(school);
                }
            }

            return schools;
        }

        /// <summary>
        /// 获取指定学校下的所有不同年级列表
        /// </summary>
        public async Task<List<string>> GetDistinctGradesAsync(string? school)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var whereClauses = new List<string> { "Grade IS NOT NULL AND Grade != ''" };
            if (!string.IsNullOrWhiteSpace(school))
            {
                whereClauses.Insert(0, "School = @school");
                command.Parameters.Add(new SqliteParameter("@school", school));
            }

            var whereClause = "WHERE " + string.Join(" AND ", whereClauses);

            command.CommandText = $@"
                SELECT DISTINCT Grade
                FROM ParticipantGroups
                {whereClause}
                ORDER BY Grade
            ";

            var grades = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var grade = reader.IsDBNull(0) ? null : reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(grade))
                {
                    grades.Add(grade);
                }
            }

            return grades;
        }

        /// <summary>
        /// 获取指定学校和年级下的所有不同班级列表
        /// </summary>
        public async Task<List<string>> GetDistinctClassesAsync(string? school, string? grade)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var whereClauses = new List<string>();
            if (!string.IsNullOrWhiteSpace(school))
            {
                whereClauses.Add("School = @school");
                command.Parameters.Add(new SqliteParameter("@school", school));
            }
            if (!string.IsNullOrWhiteSpace(grade))
            {
                whereClauses.Add("Grade = @grade");
                command.Parameters.Add(new SqliteParameter("@grade", grade));
            }

            var whereClause = whereClauses.Count > 0
                ? "WHERE " + string.Join(" AND ", whereClauses)
                : "";

            command.CommandText = $@"
                SELECT DISTINCT Class
                FROM ParticipantGroups
                {whereClause}
                AND Class IS NOT NULL AND Class != ''
                ORDER BY Class
            ";

            var classes = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var cls = reader.IsDBNull(0) ? null : reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(cls))
                {
                    classes.Add(cls);
                }
            }

            return classes;
        }

        /// <summary>
        /// 获取指定学校、年级、班级下的所有不同组别列表
        /// </summary>
        public async Task<List<string>> GetDistinctGroupNamesAsync(string? school, string? grade, string? classValue)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var whereClauses = new List<string>();
            if (!string.IsNullOrWhiteSpace(school))
            {
                whereClauses.Add("School = @school");
                command.Parameters.Add(new SqliteParameter("@school", school));
            }
            if (!string.IsNullOrWhiteSpace(grade))
            {
                whereClauses.Add("Grade = @grade");
                command.Parameters.Add(new SqliteParameter("@grade", grade));
            }
            if (!string.IsNullOrWhiteSpace(classValue))
            {
                whereClauses.Add("Class = @class");
                command.Parameters.Add(new SqliteParameter("@class", classValue));
            }

            whereClauses.Add("GroupName IS NOT NULL AND GroupName != ''");
            var whereClause = whereClauses.Count > 0
                ? "WHERE " + string.Join(" AND ", whereClauses)
                : "";

            command.CommandText = $@"
                SELECT DISTINCT GroupName
                FROM ParticipantGroups
                {whereClause}
                ORDER BY GroupName
            ";

            var groups = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var groupName = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    groups.Add(groupName);
                }
            }

            return groups;
        }
    }
}
