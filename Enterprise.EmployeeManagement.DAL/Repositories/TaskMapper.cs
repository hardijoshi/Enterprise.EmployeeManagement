using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enterprise.EmployeeManagement.DAL.Models;

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
                ReviewerName = entity.Reviewer?.FirstName
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
                ReviewerId = dto.ReviewerId
            };
        }

        public IEnumerable<TaskDTO> MapToDTOList(IEnumerable<TaskEntity> entities)
        {
            return entities?.Select(MapToDTO);
        }
    }
}
