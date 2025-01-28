using Microsoft.EntityFrameworkCore;
using Enterprise.EmployeeManagement.DAL.Context;
using Enterprise.EmployeeManagement.DAL.Models;
using System.Collections.Generic;
using System.Linq;  // Ensure this namespace is included
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<TaskRepository> _logger;

    public TaskRepository(AppDbContext context, ILogger<TaskRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<TaskEntity>> GetAllTasksAsync()
    {
        _logger.LogInformation("Fetching all tasks from repository");
        return await _context.Tasks
            .Include(t => t.AssignedEmployee)
            .Include(t => t.Reviewer)
            .ToListAsync();
    }

    public async Task<TaskEntity> GetTaskByIdAsync(int id)
    {
        _logger.LogInformation("Fetching task with ID {taskId} from repository", id);
        return await _context.Tasks
            .Include(t => t.AssignedEmployee)
            .Include(t => t.Reviewer)
            .FirstOrDefaultAsync(t => t.TaskId == id);
    }

    public async Task<TaskEntity> AddTaskAsync(TaskEntity task)
    {
        _logger.LogInformation("Adding new task to repository");
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return await GetTaskByIdAsync(task.TaskId);
    }

    public async Task UpdateTaskAsync(TaskEntity task)
    {
        _logger.LogInformation("Updating task with ID {taskId} in repository", task.TaskId);
        var existingTask = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == task.TaskId);
        if (existingTask == null)
        {
            throw new KeyNotFoundException($"Task with ID {task.TaskId} not found");
        }

        existingTask.Title = task.Title;
        existingTask.Description = task.Description;
        existingTask.Status = task.Status;
        existingTask.AssignedEmployeeId = task.AssignedEmployeeId;
        existingTask.ReviewerId = task.ReviewerId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(int id)
    {
        _logger.LogInformation("Deleting task with ID {taskId} from repository", id);
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found");
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TaskExistsAsync(int id)
    {
        return await _context.Tasks.AnyAsync(t => t.TaskId == id);
    }

    public async Task<bool> EmployeeExistsAsync(int employeeId)
    {
        return await _context.Employees.AnyAsync(e => e.Id == employeeId);
    }

    public async Task UpdateTaskStatusAsync(int id, TaskStatus status)
    {
        _logger.LogInformation("Updating status for task with ID {taskId} in repository", id);
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id);
        if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found");
        }

        task.Status = status;
        await _context.SaveChangesAsync();
    }
}