using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enterprise.EmployeeManagement.DAL.DTO;

namespace Enterprise.EmployeeManagement.DAL.Repositories
{
    public class TaskMapper : ITaskMapper
    {
        public TaskDTO MapToDTO(TaskEntity entity)
        {
            if (entity == null) return null;

            return new TaskDTO
            {
                TaskId = entity.TaskId,
                Title = entity.Title,
                Description = entity.Description,
                Status = entity.Status,
                AssignedEmployeeId = entity.AssignedEmployeeId,
                ReviewerId = entity.ReviewerId,
                AssignedEmployeeName = entity.AssignedEmployee?.FirstName,
                ReviewerName = entity.Reviewer?.FirstName,
                StartDate = entity.StartDate,
                DeadlineDate = entity.DeadlineDate,
            };
        }

        public TaskEntity MapToEntity(TaskDTO dto)
        {
            if (dto == null) return null;

            return new TaskEntity
            {
                TaskId = dto.TaskId,
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                AssignedEmployeeId = dto.AssignedEmployeeId,
                ReviewerId = dto.ReviewerId,
                StartDate = dto.StartDate != default ? dto.StartDate : DateTime.UtcNow,
                DeadlineDate = dto.DeadlineDate != default ? dto.DeadlineDate : DateTime.UtcNow.AddDays(7)
            };
        }

        public IEnumerable<TaskDTO> MapToDTOList(IEnumerable<TaskEntity> entities)
        {
            return entities?.Select(MapToDTO);
        }
    }
}
