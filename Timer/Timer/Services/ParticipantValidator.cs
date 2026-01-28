using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 参赛人员数据校验服务
    /// </summary>
    public static class ParticipantValidator
    {
        /// <summary>
        /// 验证结果
        /// </summary>
        public class ValidationResult
        {
            /// <summary>
            /// 是否验证通过
            /// </summary>
            public bool IsValid { get; set; }

            /// <summary>
            /// 错误信息列表
            /// </summary>
            public List<string> Errors { get; set; } = new List<string>();

            /// <summary>
            /// 添加错误信息
            /// </summary>
            public void AddError(string error)
            {
                Errors.Add(error);
                IsValid = false;
            }
        }

        /// <summary>
        /// 验证参赛人员数据
        /// </summary>
        /// <param name="participant">要验证的人员对象</param>
        /// <param name="repository">数据访问接口（用于唯一性检查）</param>
        /// <returns>验证结果</returns>
        public static async Task<ValidationResult> ValidateAsync(Participant participant, IParticipantRepository? repository = null)
        {
            var result = new ValidationResult { IsValid = true };

            // 验证必填字段
            ValidateRequiredFields(participant, result);

            // 验证日期格式
            ValidateDateFormat(participant, result);

            // 验证序号
            ValidateSequenceNumber(participant, result);

            // 验证唯一性（如果提供了repository）
            if (repository != null)
            {
                await ValidateUniquenessAsync(participant, repository, result);
            }

            return result;
        }

        /// <summary>
        /// 验证必填字段
        /// </summary>
        public static void ValidateRequiredFields(Participant participant, ValidationResult result)
        {
            if (participant == null)
            {
                result.AddError("参赛人员对象不能为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(participant.Name))
            {
                result.AddError("姓名不能为空");
            }
            else if (participant.Name.Length > 50)
            {
                result.AddError("姓名长度不能超过50个字符");
            }

            if (string.IsNullOrWhiteSpace(participant.Gender))
            {
                result.AddError("性别不能为空");
            }
            else if (participant.Gender != "男" && participant.Gender != "女")
            {
                result.AddError("性别必须是'男'或'女'");
            }

            if (participant.Date == default(DateTime))
            {
                result.AddError("日期不能为空");
            }
        }

        /// <summary>
        /// 验证日期格式
        /// </summary>
        public static void ValidateDateFormat(Participant participant, ValidationResult result)
        {
            if (participant.Date == default(DateTime))
            {
                return; // 已在ValidateRequiredFields中处理
            }

            // 日期格式验证在DateConverter中处理，这里只检查是否有效
            // 如果DateConverter解析失败，Date会是default值，已在ValidateRequiredFields中检查
        }

        /// <summary>
        /// 验证序号
        /// </summary>
        public static void ValidateSequenceNumber(Participant participant, ValidationResult result)
        {
            if (participant.SequenceNumber < 1)
            {
                result.AddError("序号必须大于等于1");
            }
        }

        /// <summary>
        /// 验证序号连续性（批量验证）
        /// </summary>
        /// <param name="participants">人员列表</param>
        /// <param name="result">验证结果</param>
        public static void ValidateSequenceNumberContinuity(IEnumerable<Participant> participants, ValidationResult result)
        {
            var sorted = participants.OrderBy(p => p.SequenceNumber).ToList();
            if (sorted.Count == 0)
            {
                return;
            }

            // 检查是否从1开始
            if (sorted[0].SequenceNumber != 1)
            {
                result.AddError($"序号必须从1开始，但第一个序号是{sorted[0].SequenceNumber}");
                return;
            }

            // 检查是否连续
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].SequenceNumber != sorted[i - 1].SequenceNumber + 1)
                {
                    result.AddError($"序号不连续：在{sorted[i - 1].SequenceNumber}之后应该是{sorted[i - 1].SequenceNumber + 1}，但实际是{sorted[i].SequenceNumber}");
                }
            }
        }

        /// <summary>
        /// 验证唯一性
        /// </summary>
        public static async Task ValidateUniquenessAsync(Participant participant, IParticipantRepository repository, ValidationResult result)
        {
            if (participant == null || repository == null)
            {
                return;
            }

            // 检查准考证号唯一性（如果提供了准考证号，在同一项目内）
            if (!string.IsNullOrWhiteSpace(participant.ExamNumber))
            {
                var exists = await repository.ExistsByExamNumberAsync(participant.ExamNumber, participant.ProjectId);
                if (exists)
                {
                    // 如果是更新操作，需要检查是否是同一条记录
                    var existing = await repository.GetByIdAsync(participant.Id);
                    if (existing == null || existing.ExamNumber != participant.ExamNumber)
                    {
                        result.AddError($"准考证号'{participant.ExamNumber}'已存在");
                    }
                }
            }

            // 检查芯片外部号码唯一性（如果提供了芯片外部号码）
            if (!string.IsNullOrWhiteSpace(participant.LabelNumber))
            {
                var exists = await repository.ExistsByLabelNumberAsync(participant.LabelNumber);
                if (exists)
                {
                    // 如果是更新操作，需要检查是否是同一条记录
                    var existing = await repository.GetByIdAsync(participant.Id);
                    if (existing == null || existing.LabelNumber != participant.LabelNumber)
                    {
                        result.AddError($"芯片外部号码'{participant.LabelNumber}'已存在");
                    }
                }
            }
        }
    }
}

