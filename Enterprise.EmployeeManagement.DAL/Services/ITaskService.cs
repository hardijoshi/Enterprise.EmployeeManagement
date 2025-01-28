using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.DAL.Models;

namespace Enterprise.EmployeeManagement.DAL.Services
{
    public interface ITaskService
    {
        Task<TaskDTO> GetByIdAsync(int id);
        Task<IEnumerable<TaskDTO>> GetAllAsync();
        Task<TaskDTO> CreateAsync(TaskDTO taskDto);
        Task UpdateAsync(TaskDTO taskDto);
        Task DeleteAsync(int id);
        Task<IEnumerable<TaskDTO>> GetTasksByEmployeeAsync(int employeeId);
        Task<bool> CanEmployeeModifyTaskAsync(int employeeId, int taskId);
    }

}
