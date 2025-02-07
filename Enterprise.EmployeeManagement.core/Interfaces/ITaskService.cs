using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.core.Common.Responses;
using Enterprise.EmployeeManagement.DAL.DTO;

namespace Enterprise.EmployeeManagement.core.Interfaces
{
    public interface ITaskService
    {
        Task<ResponseMessage<TaskDTO>> GetByIdAsync(int id);
        Task<ResponseMessage<IEnumerable<TaskDTO>>> GetAllAsync();
        Task<ResponseMessage<TaskDTO>> CreateAsync(TaskDTO taskDto);
        Task<ResponseMessage<bool>> UpdateAsync(TaskDTO taskDto);
        Task<ResponseMessage<bool>> DeleteAsync(int id);
        Task<ResponseMessage<IEnumerable<TaskDTO>>> GetTasksByEmployeeAsync(int employeeId);
        Task<ResponseMessage<bool>> CanEmployeeModifyTaskAsync(int employeeId, int taskId);
    }

}
