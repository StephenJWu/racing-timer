using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Timer.Data;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 圈次记录数据访问实现
    /// </summary>
    public class LapRecordRepository : ILapRecordRepository
    {
        private readonly DatabaseContext _dbContext;

        public LapRecordRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<LapRecord?> GetByIdAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                       PassTime, LapTime, TotalTime, Rank, CreatedAt
                FROM LapRecords
                WHERE Id = @Id
            ";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapLapRecord(reader);
            }

            return null;
        }

        public async Task<List<LapRecord>> GetByRaceRecordIdAsync(int raceRecordId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                       PassTime, LapTime, TotalTime, Rank, CreatedAt
                FROM LapRecords
                WHERE RaceRecordId = @RaceRecordId
                ORDER BY PassTime ASC
            ";
            command.Parameters.AddWithValue("@RaceRecordId", raceRecordId);

            var records = new List<LapRecord>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(MapLapRecord(reader));
            }

            return records;
        }

        public async Task<List<LapRecord>> GetParticipantLapsAsync(int raceRecordId, int participantId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                       PassTime, LapTime, TotalTime, Rank, CreatedAt
                FROM LapRecords
                WHERE RaceRecordId = @RaceRecordId AND ParticipantId = @ParticipantId
                ORDER BY LapNumber ASC
            ";
            command.Parameters.AddWithValue("@RaceRecordId", raceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", participantId);

            var records = new List<LapRecord>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(MapLapRecord(reader));
            }

            return records;
        }

        public async Task<LapRecord?> GetLatestLapAsync(int raceRecordId, int participantId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                       PassTime, LapTime, TotalTime, Rank, CreatedAt
                FROM LapRecords
                WHERE RaceRecordId = @RaceRecordId AND ParticipantId = @ParticipantId
                ORDER BY LapNumber DESC
                LIMIT 1
            ";
            command.Parameters.AddWithValue("@RaceRecordId", raceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", participantId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapLapRecord(reader);
            }

            return null;
        }

        public async Task<long?> GetParticipantBestTimeAsync(int raceRecordId, int participantId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT MIN(LapTime)
                FROM LapRecords
                WHERE RaceRecordId = @RaceRecordId AND ParticipantId = @ParticipantId
            ";
            command.Parameters.AddWithValue("@RaceRecordId", raceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", participantId);

            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? null : Convert.ToInt64(result);
        }

        public async Task<int> CreateAsync(LapRecord lapRecord)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO LapRecords (RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                                       PassTime, LapTime, TotalTime, Rank, CreatedAt)
                VALUES (@RaceRecordId, @ParticipantId, @ChipNumber, @LapNumber, 
                        @PassTime, @LapTime, @TotalTime, @Rank, @CreatedAt);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@RaceRecordId", lapRecord.RaceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", lapRecord.ParticipantId);
            command.Parameters.AddWithValue("@ChipNumber", lapRecord.ChipNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LapNumber", lapRecord.LapNumber);
            command.Parameters.AddWithValue("@PassTime", lapRecord.PassTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@LapTime", lapRecord.LapTime);
            command.Parameters.AddWithValue("@TotalTime", lapRecord.TotalTime);
            command.Parameters.AddWithValue("@Rank", lapRecord.Rank ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> CreateBatchAsync(List<LapRecord> lapRecords)
        {
            if (lapRecords == null || lapRecords.Count == 0)
                return 0;

            var connection = await _dbContext.GetConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                int count = 0;
                foreach (var lapRecord in lapRecords)
                {
                    var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT INTO LapRecords (RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                                               PassTime, LapTime, TotalTime, Rank, CreatedAt)
                        VALUES (@RaceRecordId, @ParticipantId, @ChipNumber, @LapNumber, 
                                @PassTime, @LapTime, @TotalTime, @Rank, @CreatedAt)
                    ";

                    command.Parameters.AddWithValue("@RaceRecordId", lapRecord.RaceRecordId);
                    command.Parameters.AddWithValue("@ParticipantId", lapRecord.ParticipantId);
                    command.Parameters.AddWithValue("@ChipNumber", lapRecord.ChipNumber ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@LapNumber", lapRecord.LapNumber);
                    command.Parameters.AddWithValue("@PassTime", lapRecord.PassTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    command.Parameters.AddWithValue("@LapTime", lapRecord.LapTime);
                    command.Parameters.AddWithValue("@TotalTime", lapRecord.TotalTime);
                    command.Parameters.AddWithValue("@Rank", lapRecord.Rank ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                    count += await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return count;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(LapRecord lapRecord)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE LapRecords
                SET RaceRecordId = @RaceRecordId,
                    ParticipantId = @ParticipantId,
                    ChipNumber = @ChipNumber,
                    LapNumber = @LapNumber,
                    PassTime = @PassTime,
                    LapTime = @LapTime,
                    TotalTime = @TotalTime,
                    Rank = @Rank
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Id", lapRecord.Id);
            command.Parameters.AddWithValue("@RaceRecordId", lapRecord.RaceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", lapRecord.ParticipantId);
            command.Parameters.AddWithValue("@ChipNumber", lapRecord.ChipNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LapNumber", lapRecord.LapNumber);
            command.Parameters.AddWithValue("@PassTime", lapRecord.PassTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@LapTime", lapRecord.LapTime);
            command.Parameters.AddWithValue("@TotalTime", lapRecord.TotalTime);
            command.Parameters.AddWithValue("@Rank", lapRecord.Rank ?? (object)DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM LapRecords WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        private LapRecord MapLapRecord(SqliteDataReader reader)
        {
            return new LapRecord
            {
                Id = reader.GetInt32(0),
                RaceRecordId = reader.GetInt32(1),
                ParticipantId = reader.GetInt32(2),
                ChipNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                LapNumber = reader.GetInt32(4),
                PassTime = DateTime.Parse(reader.GetString(5)),
                LapTime = reader.GetInt64(6),
                TotalTime = reader.GetInt64(7),
                Rank = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                CreatedAt = DateTime.Parse(reader.GetString(9))
            };
        }
    }
}

