using Enterprise.EmployeeManagement.DAL.Context;
using Enterprise.EmployeeManagement.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;



[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TasksController> _logger;

    public TasksController(AppDbContext context, ILogger<TasksController> logger)
    {
        _context = context;
        _logger = logger;
        _logger.LogInformation("TasksController Started");
    }

    [HttpGet]
 
    public async Task<IActionResult> GetTasks()
    {
        _logger.LogInformation("Fetching tasks from the database...");
        var tasks = await _context.Tasks
            .Include(t => t.AssignedEmployee)
            .Include(t => t.Reviewer)
            .ToListAsync();
        if (tasks != null && tasks.Any())
        {
            _logger.LogInformation("Successfully retrieved {taskCount} tasks.", tasks.Count);
            return Ok(tasks);
        }

        _logger.LogInformation("No tasks found in the database");
        return NotFound(); 
    }

    
    [HttpPost]
    [Authorize(Roles = "Manager, Admin")]
    public async Task<IActionResult> AddTask([FromBody] TaskEntity taskEntity)
    {
        _logger.LogInformation("Adding new tasks.....");
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid.");
            return BadRequest(ModelState);
        }
            

        // Verify the IDs exist
        var assignedEmployeeExists = await _context.Employees.AnyAsync(e => e.Id == taskEntity.AssignedEmployeeId);
        var reviewerExists = await _context.Employees.AnyAsync(e => e.Id == taskEntity.ReviewerId);

        if (!assignedEmployeeExists || !reviewerExists)
        {
            _logger.LogWarning("Invalid AssignedEmployeeId or ReviewerId provided.");
            return BadRequest("Invalid AssignedEmployeeId or ReviewerId");
        }

        try
        {
            _context.Entry(taskEntity).State = EntityState.Added;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Task with ID {taskId} successfully added.", taskEntity.TaskId);

            var savedTask = await _context.Tasks
                .Include(t => t.AssignedEmployee)
                .Include(t => t.Reviewer)
                .FirstOrDefaultAsync(t => t.TaskId == taskEntity.TaskId);

            return CreatedAtAction(nameof(GetTasks), new { id = taskEntity.TaskId }, savedTask);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error saving task with ID {taskId}.", taskEntity.TaskId);
            return BadRequest($"Error saving task: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager, Admin")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskEntity taskEntity)
    {
        _logger.LogInformation("Updating task with ID {taskId}...", id);
        if (id != taskEntity.TaskId)
        {
            _logger.LogWarning("Task ID in the URL does not match the TaskId in the body.");
            return BadRequest();
        }
           

        // Verify employees exist
        var assignedEmployeeExists = await _context.Employees.AnyAsync(e => e.Id == taskEntity.AssignedEmployeeId);
        var reviewerExists = await _context.Employees.AnyAsync(e => e.Id == taskEntity.ReviewerId);

        if (!assignedEmployeeExists || !reviewerExists)
        {
            _logger.LogWarning("Invalid AssignedEmployeeId or ReviewerId provided.");
            return BadRequest("Invalid AssignedEmployeeId or ReviewerId");
        }

        try
        {
            // Get existing task
            var existingTask = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (existingTask == null)
            {
                _logger.LogInformation("Task with ID {taskId} not found.", id);
                return NotFound();
            }
                

            // Update only the scalar properties
            existingTask.Title = taskEntity.Title;
            existingTask.Description = taskEntity.Description;
            existingTask.Status = taskEntity.Status;
            existingTask.AssignedEmployeeId = taskEntity.AssignedEmployeeId;
            existingTask.ReviewerId = taskEntity.ReviewerId;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task with ID {taskId} successfully updated.", id);
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating task with ID {taskId}.", id);
            return BadRequest($"Error updating task: {ex.Message}");
        }
    }


    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager, Admin")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        _logger.LogInformation("Deleting task with ID {taskId}...", id);
        var taskEntity = await _context.Tasks.FindAsync(id);
        if (taskEntity == null)
        {
            _logger.LogInformation("Task with ID {taskId} not found.", id);
            return NotFound();
        }
            

        _context.Tasks.Remove(taskEntity);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Task with ID {taskId} successfully deleted.", id);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] TaskStatus status)
    {
        _logger.LogInformation("Updating status for task with ID {taskId}...", id);
        var existingTask = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id);
        if (existingTask == null)
        {
            _logger.LogInformation("Task with ID {taskId} not found.", id);
            return NotFound();
        }
            

        try
        {
            // Validate status transition
            if (!IsValidStatusTransition(existingTask.Status, status))
            {
                _logger.LogWarning("Invalid status transition from {currentStatus} to {newStatus} for task with ID {taskId}.", existingTask.Status, status, id);
                return BadRequest($"Invalid status transition from {existingTask.Status} to {status}");
            }

            existingTask.Status = status;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Task status for task ID {taskId} successfully updated to {status}.", id, status);
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating status for task with ID {taskId}.", id);
            return BadRequest($"Error updating task status: {ex.Message}");
        }
    }

    private bool IsValidStatusTransition(TaskStatus currentStatus, TaskStatus newStatus)
    {
        // Define valid status transitions
        switch (currentStatus)
        {
            case TaskStatus.NotStarted:
                return newStatus == TaskStatus.Working;
            case TaskStatus.Working:
                return newStatus == TaskStatus.Pending || newStatus == TaskStatus.Completed;
            case TaskStatus.Pending:
                return newStatus == TaskStatus.Working || newStatus == TaskStatus.Completed;
            case TaskStatus.Completed:
                return false; // Completed is final state
            default:
                return false;
        }
    }


}