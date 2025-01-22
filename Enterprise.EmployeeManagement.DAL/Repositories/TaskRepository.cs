using Microsoft.EntityFrameworkCore;
using Enterprise.EmployeeManagement.DAL.Context;
using Enterprise.EmployeeManagement.DAL.Models;
using System.Collections.Generic;
using System.Linq;  // Ensure this namespace is included
using System.Threading.Tasks;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaskEntity>> GetTasksByEmployeeIdAsync(int employeeId)
    {
        return await _context.Tasks
            .Where(t => t.AssignedEmployeeId == employeeId)
            .ToListAsync();
    }
}
