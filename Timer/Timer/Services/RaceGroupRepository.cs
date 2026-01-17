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
        /// 根据条件查询比赛分组
        /// </summary>
        public async Task<IEnumerable<RaceGroup>> QueryRaceGroupsAsync(
            DateTime startDate,
            DateTime endDate,
            string school,
            string? grade = null,
            string? classValue = null,
            string? groupName = null)
        {
            if (string.IsNullOrWhiteSpace(school))
            {
                throw new ArgumentNullException(nameof(school), "学校不能为空");
            }

            var connection = await _dbContext.GetConnectionAsync();

            // 首先查询符合条件的参赛人员分组信息
            var participantCommand = connection.CreateCommand();
            var whereClauses = new List<string>
            {
                "substr(Date, 1, 10) >= @startDate",
                "substr(Date, 1, 10) <= @endDate",
                "School = @school",
                "GroupName IS NOT NULL",
                "GroupName != ''"
            };

            participantCommand.Parameters.Add(new SqliteParameter("@startDate", startDate.ToString("yyyy-MM-dd")));
            participantCommand.Parameters.Add(new SqliteParameter("@endDate", endDate.ToString("yyyy-MM-dd")));
            participantCommand.Parameters.Add(new SqliteParameter("@school", school));

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

            // 为每个分组获取或创建 RaceGroup 记录
            var raceGroups = new List<RaceGroup>();
            foreach (var info in groupInfos)
            {
                var raceGroup = await GetOrCreateRaceGroupAsync(info.School, info.Grade, info.Class, info.GroupName);
                raceGroup.ParticipantCount = info.Count;
                raceGroups.Add(raceGroup);
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
                    rg.School,
                    rg.Grade,
                    rg.Class,
                    rg.GroupName,
                    rg.ChipGroupId,
                    rg.RaceLaps,
                    rg.CreatedAt,
                    rg.UpdatedAt,
                    cg.GroupName as ChipGroupName,
                    cg.Color as ChipGroupColor
                FROM RaceGroups rg
                LEFT JOIN ChipGroups cg ON rg.ChipGroupId = cg.Id
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
        /// 获取或创建分组记录
        /// </summary>
        public async Task<RaceGroup> GetOrCreateRaceGroupAsync(
            string school,
            string? grade,
            string? classValue,
            string groupName)
        {
            if (string.IsNullOrWhiteSpace(school))
            {
                throw new ArgumentNullException(nameof(school));
            }

            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var connection = await _dbContext.GetConnectionAsync();

            // 尝试查找现有记录
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
                SELECT 
                    rg.Id,
                    rg.School,
                    rg.Grade,
                    rg.Class,
                    rg.GroupName,
                    rg.ChipGroupId,
                    rg.RaceLaps,
                    rg.CreatedAt,
                    rg.UpdatedAt,
                    cg.GroupName as ChipGroupName,
                    cg.Color as ChipGroupColor
                FROM RaceGroups rg
                LEFT JOIN ChipGroups cg ON rg.ChipGroupId = cg.Id
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

            // 如果不存在，创建新记录
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO RaceGroups (School, Grade, Class, GroupName, RaceLaps, CreatedAt, UpdatedAt)
                VALUES (@school, @grade, @class, @groupName, 1, @createdAt, @updatedAt);
                SELECT last_insert_rowid();
            ";

            insertCommand.Parameters.Add(new SqliteParameter("@school", school));
            insertCommand.Parameters.Add(new SqliteParameter("@grade", grade ?? (object)DBNull.Value));
            insertCommand.Parameters.Add(new SqliteParameter("@class", classValue ?? (object)DBNull.Value));
            insertCommand.Parameters.Add(new SqliteParameter("@groupName", groupName));
            insertCommand.Parameters.Add(new SqliteParameter("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            insertCommand.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            var newId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());

            _loggingService?.Info($"Created new RaceGroup: {school}-{grade}-{classValue}-{groupName}");

            return new RaceGroup
            {
                Id = newId,
                School = school,
                Grade = grade,
                Class = classValue,
                GroupName = groupName,
                RaceLaps = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 更新分组的芯片组和比赛圈数
        /// </summary>
        public async Task<bool> UpdateChipGroupAndLapsAsync(int id, int chipGroupId, int raceLaps)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(id));
            }

            if (chipGroupId <= 0)
            {
                throw new ArgumentException("ChipGroupId must be greater than 0", nameof(chipGroupId));
            }

            if (raceLaps < 1 || raceLaps > 20)
            {
                throw new ArgumentException("RaceLaps must be between 1 and 20", nameof(raceLaps));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE RaceGroups
                SET ChipGroupId = @chipGroupId, RaceLaps = @raceLaps, UpdatedAt = @updatedAt
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", id));
            command.Parameters.Add(new SqliteParameter("@chipGroupId", chipGroupId));
            command.Parameters.Add(new SqliteParameter("@raceLaps", raceLaps));
            command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
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
                SELECT LabelNumber
                FROM Chips
                WHERE ChipGroupId = @chipGroupId
                ORDER BY CAST(LabelNumber AS INTEGER)
            ";
            chipsCommand.Parameters.Add(new SqliteParameter("@chipGroupId", chipGroupId));

            var chips = new List<string>();
            using (var reader = await chipsCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    chips.Add(reader.GetString(0));
                }
            }

            // 获取分组内的所有参赛人员（按序号排序）
            var participants = (await GetParticipantsByGroupAsync(
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

            // 为每个参赛人员分配芯片
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE Participants
                SET ChipNumber = @chipNumber, UpdatedAt = @updatedAt
                WHERE Id = @id
            ";

            var chipNumberParam = new SqliteParameter("@chipNumber", "");
            var updatedAtParam = new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var idParam = new SqliteParameter("@id", 0);

            updateCommand.Parameters.Add(chipNumberParam);
            updateCommand.Parameters.Add(updatedAtParam);
            updateCommand.Parameters.Add(idParam);

            int assignedCount = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                chipNumberParam.Value = chips[i];
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
                SELECT p.Id, p.SequenceNumber, p.Date, p.School, p.Grade, p.Class, p.Name, p.Gender, 
                       p.ExamNumber, p.GroupName, p.BibNumber, p.ChipNumber, p.CreatedAt, p.UpdatedAt,
                       c.InternalNumber as ChipInternalNumber
                FROM Participants p
                LEFT JOIN Chips c ON p.ChipNumber = c.LabelNumber
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
            return new RaceGroup
            {
                Id = reader.GetInt32(0),
                School = reader.GetString(1),
                Grade = reader.IsDBNull(2) ? null : reader.GetString(2),
                Class = reader.IsDBNull(3) ? null : reader.GetString(3),
                GroupName = reader.GetString(4),
                ChipGroupId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                RaceLaps = reader.GetInt32(6),
                CreatedAt = DateTime.Parse(reader.GetString(7)),
                UpdatedAt = DateTime.Parse(reader.GetString(8)),
                ChipGroupName = reader.IsDBNull(9) ? null : reader.GetString(9),
                ChipGroupColor = reader.IsDBNull(10) ? null : reader.GetString(10)
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
                BibNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                ChipNumber = reader.IsDBNull(11) ? null : reader.GetString(11),
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
                SequenceNumber = reader.GetInt32(1),
                Date = DateTime.Parse(reader.GetString(2)),
                School = reader.IsDBNull(3) ? null : reader.GetString(3),
                Grade = reader.IsDBNull(4) ? null : reader.GetString(4),
                Class = reader.IsDBNull(5) ? null : reader.GetString(5),
                Name = reader.GetString(6),
                Gender = reader.GetString(7),
                ExamNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                GroupName = reader.IsDBNull(9) ? null : reader.GetString(9),
                BibNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                ChipNumber = reader.IsDBNull(11) ? null : reader.GetString(11),
                CreatedAt = DateTime.Parse(reader.GetString(12)),
                UpdatedAt = DateTime.Parse(reader.GetString(13)),
                ChipInternalNumber = reader.IsDBNull(14) ? null : reader.GetString(14)
            };
        }
    }
}

