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
    /// 比赛分组数据访问实现
    /// </summary>
    public class RaceGroupRepository : IRaceGroupRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILoggingService? _loggingService;

        /// <summary>
        /// 初始化数据访问实现
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="loggingService">日志服务（可选）</param>
        public RaceGroupRepository(DatabaseContext dbContext, ILoggingService? loggingService = null)
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

        /// <summary>
        /// 根据条件查询比赛分组
        /// </summary>
        public async Task<IEnumerable<RaceGroup>> QueryRaceGroupsAsync(
            int? projectId,
            string? school,
            string? grade = null,
            string? classValue = null,
            string? groupName = null)
        {
            var connection = await _dbContext.GetConnectionAsync();

            // 首先查询符合条件的参赛人员分组信息
            var participantCommand = connection.CreateCommand();
            var whereClauses = new List<string>
            {
                "GroupName IS NOT NULL",
                "GroupName != ''"
            };

            if (projectId.HasValue && projectId.Value > 0)
            {
                whereClauses.Add("ProjectId = @projectId");
                participantCommand.Parameters.Add(new SqliteParameter("@projectId", projectId.Value));
            }

            if (!string.IsNullOrWhiteSpace(school))
            {
                whereClauses.Add("School = @school");
                participantCommand.Parameters.Add(new SqliteParameter("@school", school));
            }

            if (!string.IsNullOrWhiteSpace(grade))
            {
                whereClauses.Add("Grade = @grade");
                participantCommand.Parameters.Add(new SqliteParameter("@grade", grade));
            }

            if (!string.IsNullOrWhiteSpace(classValue))
            {
                whereClauses.Add("Class = @class");
                participantCommand.Parameters.Add(new SqliteParameter("@class", classValue));
            }

            if (!string.IsNullOrWhiteSpace(groupName))
            {
                whereClauses.Add("GroupName = @groupName");
                participantCommand.Parameters.Add(new SqliteParameter("@groupName", groupName));
            }

            var whereClause = string.Join(" AND ", whereClauses);

            participantCommand.CommandText = $@"
                SELECT 
                    School,
                    Grade,
                    Class,
                    GroupName,
                    COUNT(*) as ParticipantCount
                FROM Participants
                WHERE {whereClause}
                GROUP BY School, Grade, Class, GroupName
                ORDER BY Grade, Class, GroupName
            ";

            _loggingService?.Debug($"[SQL] QueryRaceGroupsAsync: {participantCommand.CommandText}");

            var groupInfos = new List<(string School, string? Grade, string? Class, string GroupName, int Count)>();

            using (var reader = await participantCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    groupInfos.Add((
                        reader.GetString(0),
                        reader.IsDBNull(1) ? null : reader.GetString(1),
                        reader.IsDBNull(2) ? null : reader.GetString(2),
                        reader.GetString(3),
                        reader.GetInt32(4)
                    ));
                }
            }

            // 根据查询条件从 RaceGroups 表查询已存在的分组
            // 注意：新架构中，RaceGroups 只在比赛计时菜单中创建，不再自动创建
            var raceGroupCommand = connection.CreateCommand();
            var raceGroupWhereClauses = new List<string>();

            if (projectId.HasValue && projectId.Value > 0)
            {
                raceGroupWhereClauses.Add("rg.ProjectId = @projectId");
                raceGroupCommand.Parameters.Add(new SqliteParameter("@projectId", projectId.Value));
            }

            if (!string.IsNullOrWhiteSpace(school))
            {
                raceGroupWhereClauses.Add("rg.School = @school");
                raceGroupCommand.Parameters.Add(new SqliteParameter("@school", school));
            }

            if (!string.IsNullOrWhiteSpace(grade))
            {
                raceGroupWhereClauses.Add("rg.Grade = @grade");
                raceGroupCommand.Parameters.Add(new SqliteParameter("@grade", grade));
            }

            if (!string.IsNullOrWhiteSpace(classValue))
            {
                raceGroupWhereClauses.Add("rg.Class = @class");
                raceGroupCommand.Parameters.Add(new SqliteParameter("@class", classValue));
            }

            if (!string.IsNullOrWhiteSpace(groupName))
            {
                raceGroupWhereClauses.Add("rg.GroupName = @groupName");
                raceGroupCommand.Parameters.Add(new SqliteParameter("@groupName", groupName));
            }

            var raceGroupWhereClause = raceGroupWhereClauses.Count > 0 
                ? "WHERE " + string.Join(" AND ", raceGroupWhereClauses) 
                : "";

            raceGroupCommand.CommandText = $@"
                SELECT 
                    rg.Id,
                    rg.ProjectId,
                    rg.School,
                    rg.Grade,
                    rg.Class,
                    rg.GroupName,
                    rg.ParticipantCount,
                    rg.RaceLaps,
                    rg.ChipGroupId,
                    rg.ChipGroupName,
                    rg.Status,
                    rg.CreatedAt,
                    rg.UpdatedAt,
                    cg.ChipGroupName as ChipGroupNameFromJoin,
                    cg.Color as ChipGroupColor,
                    p.Name as ProjectName
                FROM RaceGroups rg
                LEFT JOIN ChipGroups cg ON rg.ChipGroupId = cg.Id
                LEFT JOIN Projects p ON rg.ProjectId = p.Id
                {raceGroupWhereClause}
                ORDER BY rg.School, rg.Grade, rg.Class, rg.GroupName
            ";

            LogSql("QueryRaceGroupsAsync", raceGroupCommand.CommandText);

            var raceGroups = new List<RaceGroup>();
            using (var reader = await raceGroupCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    raceGroups.Add(MapToRaceGroup(reader));
                }
            }

            return raceGroups;
        }

        /// <summary>
        /// 获取所有比赛分组
        /// </summary>
        public async Task<List<RaceGroup>> GetAllAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT 
                    rg.Id,
                    rg.ProjectId,
                    rg.School,
                    rg.Grade,
                    rg.Class,
                    rg.GroupName,
                    rg.ParticipantCount,
                    rg.RaceLaps,
                    rg.ChipGroupId,
                    rg.ChipGroupName,
                    rg.Status,
                    rg.CreatedAt,
                    rg.UpdatedAt,
                    cg.ChipGroupName as ChipGroupNameFromJoin,
                    cg.Color as ChipGroupColor,
                    p.Name as ProjectName
                FROM RaceGroups rg
                LEFT JOIN ChipGroups cg ON rg.ChipGroupId = cg.Id
                LEFT JOIN Projects p ON rg.ProjectId = p.Id
                ORDER BY rg.School, rg.Grade, rg.Class, rg.GroupName
            ";

            var raceGroups = new List<RaceGroup>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                raceGroups.Add(MapToRaceGroup(reader));
            }

            return raceGroups;
        }

        /// <summary>
        /// 根据ID获取单个分组
        /// </summary>
        public async Task<RaceGroup?> GetByIdAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT 
                    rg.Id,
                    rg.ProjectId,
                    rg.School,
                    rg.Grade,
                    rg.Class,
                    rg.GroupName,
                    rg.ParticipantCount,
                    rg.RaceLaps,
                    rg.ChipGroupId,
                    rg.ChipGroupName,
                    rg.Status,
                    rg.CreatedAt,
                    rg.UpdatedAt,
                    cg.ChipGroupName as ChipGroupNameFromJoin,
                    cg.Color as ChipGroupColor,
                    p.Name as ProjectName
                FROM RaceGroups rg
                LEFT JOIN ChipGroups cg ON rg.ChipGroupId = cg.Id
                LEFT JOIN Projects p ON rg.ProjectId = p.Id
                WHERE rg.Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", id));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToRaceGroup(reader);
            }

            return null;
        }

        /// <summary>
        /// 根据项目ID获取比赛分组选项（从参赛人员数据中获取学校-年级-班级-组别组合）
        /// 注意：这些是用于显示在下拉框中的选项，不会创建 RaceGroups
        /// RaceGroups 只在比赛计时菜单中通过"添加到比赛"按钮创建
        /// </summary>
        public async Task<List<RaceGroup>> GetByProjectIdAsync(int projectId)
        {
            var connection = await _dbContext.GetConnectionAsync();

            // 从 Participants 表查询该项目下的所有分组组合
            var participantCommand = connection.CreateCommand();
            participantCommand.CommandText = @"
                SELECT 
                    School,
                    Grade,
                    Class,
                    GroupName,
                    COUNT(*) as ParticipantCount
                FROM Participants
                WHERE ProjectId = @projectId
                    AND GroupName IS NOT NULL
                    AND GroupName != ''
                GROUP BY School, Grade, Class, GroupName
                ORDER BY School, Grade, Class, GroupName
            ";
            participantCommand.Parameters.Add(new SqliteParameter("@projectId", projectId));

            var raceGroups = new List<RaceGroup>();
            using (var reader = await participantCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var school = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    if (string.IsNullOrWhiteSpace(school)) continue;

                    // 创建临时的 RaceGroup 对象用于显示（不保存到数据库）
                    var raceGroup = new RaceGroup
                    {
                        Id = 0, // 临时对象，ID为0表示未保存
                        ProjectId = projectId,
                        School = school,
                        Grade = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Class = reader.IsDBNull(2) ? null : reader.GetString(2),
                        GroupName = reader.GetString(3),
                        ParticipantCount = reader.GetInt32(4),
                        RaceLaps = 1,
                        Status = RaceStatus.Pending,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    raceGroups.Add(raceGroup);
                }
            }

            return raceGroups;
        }

        /// <summary>
        /// 获取或创建分组记录（已废弃：新架构中 RaceGroups 只在比赛计时菜单中创建）
        /// </summary>
        [Obsolete("新架构中 RaceGroups 只在比赛计时菜单中创建，请使用 CreateAsync 方法")]
        public async Task<RaceGroup> GetOrCreateRaceGroupAsync(
            string school,
            string? grade,
            string? classValue,
            string groupName)
        {
            // 新架构中不再自动创建 RaceGroups，只查询已存在的
            var connection = await _dbContext.GetConnectionAsync();

            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
                SELECT 
                    rg.Id,
                    rg.ProjectId,
                    rg.School,
                    rg.Grade,
                    rg.Class,
                    rg.GroupName,
                    rg.ParticipantCount,
                    rg.RaceLaps,
                    rg.ChipGroupId,
                    rg.ChipGroupName,
                    rg.Status,
                    rg.CreatedAt,
                    rg.UpdatedAt,
                    cg.ChipGroupName as ChipGroupNameFromJoin,
                    cg.Color as ChipGroupColor,
                    p.Name as ProjectName
                FROM RaceGroups rg
                LEFT JOIN ChipGroups cg ON rg.ChipGroupId = cg.Id
                LEFT JOIN Projects p ON rg.ProjectId = p.Id
                WHERE rg.School = @school
                  AND (rg.Grade = @grade OR (rg.Grade IS NULL AND @grade IS NULL))
                  AND (rg.Class = @class OR (rg.Class IS NULL AND @class IS NULL))
                  AND rg.GroupName = @groupName
            ";

            selectCommand.Parameters.Add(new SqliteParameter("@school", school));
            selectCommand.Parameters.Add(new SqliteParameter("@grade", grade ?? (object)DBNull.Value));
            selectCommand.Parameters.Add(new SqliteParameter("@class", classValue ?? (object)DBNull.Value));
            selectCommand.Parameters.Add(new SqliteParameter("@groupName", groupName));

            using (var reader = await selectCommand.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return MapToRaceGroup(reader);
                }
            }

            // 如果不存在，抛出异常（不再自动创建）
            throw new InvalidOperationException($"找不到比赛分组: {school}-{grade}-{classValue}-{groupName}。请先在比赛计时菜单中创建该分组。");
        }

        /// <summary>
        /// 更新分组的配置（芯片组 + 圈数）
        /// </summary>
        public async Task<bool> UpdateRaceGroupSettingsAsync(int id, int chipGroupId, int raceLaps)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(id));
            }

            if (chipGroupId <= 0)
            {
                throw new ArgumentException("ChipGroupId must be greater than 0", nameof(chipGroupId));
            }

            if (raceLaps <= 0)
            {
                raceLaps = 1;
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            // 获取芯片组名称
            string? chipGroupName = null;
            if (chipGroupId > 0)
            {
                var chipRepo = new ChipRepository(_dbContext, _loggingService);
                var chipGroup = await chipRepo.GetChipGroupByIdAsync(chipGroupId);
                if (chipGroup != null)
                {
                    chipGroupName = chipGroup.ChipGroupName;
                }
            }
            
            command.CommandText = @"
                UPDATE RaceGroups
                SET ChipGroupId = @chipGroupId, ChipGroupName = @chipGroupName, RaceLaps = @raceLaps, UpdatedAt = @updatedAt
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", id));
            command.Parameters.Add(new SqliteParameter("@chipGroupId", chipGroupId));
            command.Parameters.Add(new SqliteParameter("@chipGroupName", chipGroupName ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@raceLaps", raceLaps));
            command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// 兼容旧接口：仅更新芯片组（圈数保持不变）
        /// </summary>
        public async Task<bool> UpdateChipGroupAsync(int id, int chipGroupId)
        {
            var existing = await GetByIdAsync(id);
            var laps = existing?.RaceLaps ?? 1;
            return await UpdateRaceGroupSettingsAsync(id, chipGroupId, laps);
        }

        /// <summary>
        /// 为分组内的所有参赛人员分配芯片
        /// </summary>
        public async Task<int> AssignChipsToParticipantsAsync(int raceGroupId, int chipGroupId)
        {
            var raceGroup = await GetByIdAsync(raceGroupId);
            if (raceGroup == null)
            {
                throw new InvalidOperationException($"RaceGroup with Id {raceGroupId} not found");
            }

            var connection = await _dbContext.GetConnectionAsync();

            // 获取芯片组的所有芯片（按标签号码排序）
            var chipsCommand = connection.CreateCommand();
            chipsCommand.CommandText = @"
                SELECT LabelNumber, InternalNumber
                FROM Chips
                WHERE ChipGroupId = @chipGroupId
                ORDER BY CAST(LabelNumber AS INTEGER)
            ";
            chipsCommand.Parameters.Add(new SqliteParameter("@chipGroupId", chipGroupId));

            var chips = new List<(string LabelNumber, string InternalNumber)>();
            using (var reader = await chipsCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    chips.Add((reader.GetString(0), reader.GetString(1)));
                }
            }

            // 获取分组内的所有参赛人员（按序号排序）
            var participants = (await GetParticipantsByGroupAsync(
                raceGroup.ProjectId,
                raceGroup.School,
                raceGroup.Grade,
                raceGroup.Class,
                raceGroup.GroupName)).ToList();

            // 验证芯片数量是否足够
            if (chips.Count < participants.Count)
            {
                throw new InvalidOperationException(
                    $"芯片数量不足：需要 {participants.Count} 个芯片，但芯片组中只有 {chips.Count} 个芯片");
            }

            // 为每个参赛人员分配芯片（外部号码/内部号码）
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE Participants
                SET LabelNumber = @labelNumber, InternalNumber = @internalNumber, UpdatedAt = @updatedAt
                WHERE Id = @id
            ";

            var labelNumberParam = new SqliteParameter("@labelNumber", "");
            var internalNumberParam = new SqliteParameter("@internalNumber", "");
            var updatedAtParam = new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var idParam = new SqliteParameter("@id", 0);

            updateCommand.Parameters.Add(labelNumberParam);
            updateCommand.Parameters.Add(internalNumberParam);
            updateCommand.Parameters.Add(updatedAtParam);
            updateCommand.Parameters.Add(idParam);

            int assignedCount = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                var chip = chips[i];
                labelNumberParam.Value = chip.LabelNumber;
                internalNumberParam.Value = chip.InternalNumber;
                idParam.Value = participants[i].Id;

                await updateCommand.ExecuteNonQueryAsync();
                assignedCount++;
            }

            _loggingService?.Info($"Assigned {assignedCount} chips to participants in group {raceGroup.DisplayName}");

            return assignedCount;
        }

        /// <summary>
        /// 获取分组内的所有参赛人员
        /// </summary>
        public async Task<IEnumerable<Participant>> GetParticipantsByGroupAsync(
            int? projectId,
            string school,
            string? grade,
            string? classValue,
            string groupName)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var whereClauses = new List<string>
            {
                "p.School = @school",
                "p.GroupName = @groupName"
            };

            command.Parameters.Add(new SqliteParameter("@school", school));
            command.Parameters.Add(new SqliteParameter("@groupName", groupName));

            // 如果提供了 ProjectId，则添加项目过滤条件
            if (projectId.HasValue && projectId.Value > 0)
            {
                whereClauses.Add("p.ProjectId = @projectId");
                command.Parameters.Add(new SqliteParameter("@projectId", projectId.Value));
            }

            if (!string.IsNullOrWhiteSpace(grade))
            {
                whereClauses.Add("p.Grade = @grade");
                command.Parameters.Add(new SqliteParameter("@grade", grade));
            }
            else
            {
                whereClauses.Add("p.Grade IS NULL");
            }

            if (!string.IsNullOrWhiteSpace(classValue))
            {
                whereClauses.Add("p.Class = @class");
                command.Parameters.Add(new SqliteParameter("@class", classValue));
            }
            else
            {
                whereClauses.Add("p.Class IS NULL");
            }

            var whereClause = string.Join(" AND ", whereClauses);

            // LEFT JOIN Chips 表获取芯片内部号码
            command.CommandText = $@"
                SELECT p.Id, p.ProjectId, p.ParticipantGroupId,
                       p.SequenceNumber, p.Date, p.School, p.Grade, p.Class, p.Name, p.Gender, 
                       p.ExamNumber, p.GroupName, p.LabelNumber, p.InternalNumber, p.CreatedAt, p.UpdatedAt,
                       c.InternalNumber as ChipInternalNumber
                FROM Participants p
                LEFT JOIN Chips c ON p.LabelNumber = c.LabelNumber
                WHERE {whereClause}
                ORDER BY p.SequenceNumber
            ";

            var participants = new List<Participant>();
            using var reader = await command.ExecuteReaderAsync();
            int groupSeqNum = 1;
            while (await reader.ReadAsync())
            {
                var participant = MapToParticipantWithChipInfo(reader);
                participant.GroupSequenceNumber = groupSeqNum++;
                participants.Add(participant);
            }

            return participants;
        }

        /// <summary>
        /// 从数据读取器映射到RaceGroup对象
        /// </summary>
        private RaceGroup MapToRaceGroup(SqliteDataReader reader)
        {
            // 优先使用数据库中的ChipGroupName，如果没有则使用JOIN的ChipGroupNameFromJoin
            var chipGroupNameFromDb = reader.IsDBNull(9) ? null : reader.GetString(9);
            var chipGroupNameFromJoin = reader.IsDBNull(13) ? null : reader.GetString(13);
            var chipGroupName = chipGroupNameFromDb ?? chipGroupNameFromJoin;
            
            return new RaceGroup
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                School = reader.GetString(2),
                Grade = reader.IsDBNull(3) ? null : reader.GetString(3),
                Class = reader.IsDBNull(4) ? null : reader.GetString(4),
                GroupName = reader.GetString(5),
                ParticipantCount = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                RaceLaps = reader.GetInt32(7),
                ChipGroupId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                ChipGroupName = chipGroupName,
                Status = reader.IsDBNull(10) ? RaceStatus.Pending : Enum.Parse<RaceStatus>(reader.GetString(10)),
                CreatedAt = DateTime.Parse(reader.GetString(11)),
                UpdatedAt = DateTime.Parse(reader.GetString(12)),
                ChipGroupColor = reader.IsDBNull(14) ? null : reader.GetString(14),
                ProjectName = reader.IsDBNull(15) ? null : reader.GetString(15)
            };
        }

        /// <summary>
        /// 从数据读取器映射到Participant对象
        /// </summary>
        private Participant MapToParticipant(SqliteDataReader reader)
        {
            return new Participant
            {
                Id = reader.GetInt32(0),
                SequenceNumber = reader.GetInt32(1),
                Date = DateTime.Parse(reader.GetString(2)),
                School = reader.IsDBNull(3) ? null : reader.GetString(3),
                Grade = reader.IsDBNull(4) ? null : reader.GetString(4),
                Class = reader.IsDBNull(5) ? null : reader.GetString(5),
                Name = reader.GetString(6),
                Gender = reader.GetString(7),
                ExamNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                GroupName = reader.IsDBNull(9) ? null : reader.GetString(9),
                LabelNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                InternalNumber = reader.IsDBNull(11) ? null : reader.GetString(11),
                CreatedAt = DateTime.Parse(reader.GetString(12)),
                UpdatedAt = DateTime.Parse(reader.GetString(13))
            };
        }

        /// <summary>
        /// 从数据读取器映射到Participant对象（包含芯片内部号码）
        /// </summary>
        private Participant MapToParticipantWithChipInfo(SqliteDataReader reader)
        {
            return new Participant
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                ParticipantGroupId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                SequenceNumber = reader.GetInt32(3),
                Date = DateTime.Parse(reader.GetString(4)),
                School = reader.IsDBNull(5) ? null : reader.GetString(5),
                Grade = reader.IsDBNull(6) ? null : reader.GetString(6),
                Class = reader.IsDBNull(7) ? null : reader.GetString(7),
                Name = reader.GetString(8),
                Gender = reader.GetString(9),
                ExamNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                GroupName = reader.IsDBNull(11) ? null : reader.GetString(11),
                LabelNumber = reader.IsDBNull(12) ? null : reader.GetString(12),
                InternalNumber = reader.IsDBNull(13) ? null : reader.GetString(13),
                CreatedAt = DateTime.Parse(reader.GetString(14)),
                UpdatedAt = DateTime.Parse(reader.GetString(15)),
                ChipInternalNumber = reader.IsDBNull(16) ? null : reader.GetString(16)
            };
        }

        /// <summary>
        /// 创建比赛分组
        /// </summary>
        public async Task<int> CreateAsync(RaceGroup raceGroup)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO RaceGroups (ProjectId, School, Grade, Class, GroupName, ParticipantCount, RaceLaps, ChipGroupId, ChipGroupName, Status, CreatedAt, UpdatedAt)
                VALUES (@projectId, @school, @grade, @class, @groupName, @participantCount, @raceLaps, @chipGroupId, @chipGroupName, @status, @createdAt, @updatedAt);
                SELECT last_insert_rowid();
            ";

            command.Parameters.Add(new SqliteParameter("@projectId", raceGroup.ProjectId ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@school", raceGroup.School));
            command.Parameters.Add(new SqliteParameter("@grade", raceGroup.Grade ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@class", raceGroup.Class ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@groupName", raceGroup.GroupName));
            command.Parameters.Add(new SqliteParameter("@participantCount", raceGroup.ParticipantCount));
            command.Parameters.Add(new SqliteParameter("@raceLaps", raceGroup.RaceLaps));
            command.Parameters.Add(new SqliteParameter("@chipGroupId", raceGroup.ChipGroupId ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@chipGroupName", raceGroup.ChipGroupName ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@status", raceGroup.Status.ToString()));
            command.Parameters.Add(new SqliteParameter("@createdAt", raceGroup.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(new SqliteParameter("@updatedAt", raceGroup.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")));

            LogSql("CreateAsync", command.CommandText, $"ProjectId={raceGroup.ProjectId}, School={raceGroup.School}");

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            _loggingService?.Info($"创建比赛分组: {raceGroup.DisplayName} (ID: {id})");
            return id;
        }

        /// <summary>
        /// 更新比赛分组
        /// </summary>
        public async Task<bool> UpdateAsync(RaceGroup raceGroup)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE RaceGroups 
                SET ProjectId = @projectId, 
                    School = @school, 
                    Grade = @grade, 
                    Class = @class, 
                    GroupName = @groupName,
                    ParticipantCount = @participantCount,
                    RaceLaps = @raceLaps,
                    ChipGroupId = @chipGroupId,
                    ChipGroupName = @chipGroupName,
                    Status = @status,
                    UpdatedAt = @updatedAt
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", raceGroup.Id));
            command.Parameters.Add(new SqliteParameter("@projectId", raceGroup.ProjectId ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@school", raceGroup.School));
            command.Parameters.Add(new SqliteParameter("@grade", raceGroup.Grade ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@class", raceGroup.Class ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@groupName", raceGroup.GroupName));
            command.Parameters.Add(new SqliteParameter("@participantCount", raceGroup.ParticipantCount));
            command.Parameters.Add(new SqliteParameter("@raceLaps", raceGroup.RaceLaps));
            command.Parameters.Add(new SqliteParameter("@chipGroupId", raceGroup.ChipGroupId ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@chipGroupName", raceGroup.ChipGroupName ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@status", raceGroup.Status.ToString()));
            command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            LogSql("UpdateAsync", command.CommandText, $"Id={raceGroup.Id}");

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// 删除比赛分组
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM RaceGroups WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            LogSql("DeleteAsync", command.CommandText, $"Id={id}");

            var rowsAffected = await command.ExecuteNonQueryAsync();
            _loggingService?.Info($"删除比赛分组: ID={id}, 影响行数={rowsAffected}");
            return rowsAffected > 0;
        }
    }
}

