using System.Collections.Generic;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.DAL.Models;

public interface ITaskRepository
{
    Task<List<TaskEntity>> GetTasksByEmployeeIdAsync(int employeeId);
}
