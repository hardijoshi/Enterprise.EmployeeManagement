using System;
using System.Collections.Generic;
using System.Text;
using Enterprise.EmployeeManagement.DAL.Models;

namespace Enterprise.EmployeeManagement.DAL.Repositories
{
    public interface ITaskMapper
    {
        TaskDTO MapToDTO(TaskEntity entity);
        TaskEntity MapToEntity(TaskDTO dto);
        IEnumerable<TaskDTO> MapToDTOList(IEnumerable<TaskEntity> entities);
    }
}
