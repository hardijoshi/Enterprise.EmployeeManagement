using System.Collections.Generic;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.DAL.Models;

public interface ITaskRepository
{
    Task<IEnumerable<TaskEntity>> GetAllTasksAsync();
    Task<TaskEntity> GetTaskByIdAsync(int id);
    Task<TaskEntity> AddTaskAsync(TaskEntity task);
    Task<bool> UpdateTaskAsync(TaskEntity task);
    Task<bool> DeleteTaskAsync(int id);
    Task<bool> TaskExistsAsync(int id);
    Task<bool> EmployeeExistsAsync(int employeeId);
    Task<bool> UpdateTaskStatusAsync(int id, TaskStatus status);
}

