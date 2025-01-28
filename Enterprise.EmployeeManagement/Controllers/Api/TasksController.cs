using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
        _logger.LogInformation("TasksController Started");
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        _logger.LogInformation("Fetching all tasks...");
        try
        {
            var tasks = await _taskService.GetAllAsync();
            if (tasks == null || !tasks.Any())
            {
                _logger.LogInformation("No tasks found");
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved tasks");
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks");
            return StatusCode(500, "Internal server error occurred while retrieving tasks");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id)
    {
        _logger.LogInformation("Fetching task with ID {taskId}...", id);
        try
        {
            var task = await _taskService.GetByIdAsync(id);
            if (task == null)
            {
                _logger.LogWarning("Task with ID {taskId} not found", id);
                return NotFound();
            }
            task.CalculateTaskProperties();
            _logger.LogInformation("Successfully retrieved task with ID {taskId}", id);

            return Ok(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task with ID {taskId}", id);
            return StatusCode(500, "Internal server error occurred while retrieving the task");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Manager, Admin")]
    public async Task<IActionResult> AddTask([FromBody] TaskDTO taskDto)
    {
        _logger.LogInformation("Adding new task...");
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state");
            return BadRequest(ModelState);
        }

        try
        {
            var createdTask = await _taskService.CreateAsync(taskDto);
            _logger.LogInformation("Task successfully added with ID {taskId}", createdTask.TaskId);
            return CreatedAtAction(nameof(GetTask), new { id = createdTask.TaskId }, createdTask);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invalid employee reference in task creation");
            return BadRequest("Invalid AssignedEmployeeId or ReviewerId");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding task");
            return StatusCode(500, "Internal server error occurred while creating the task");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager, Admin")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskDTO taskDto)
    {
        _logger.LogInformation("Updating task with ID {taskId}...", id);
        if (id != taskDto.TaskId)
        {
            _logger.LogWarning("Task ID mismatch");
            return BadRequest("ID mismatch");
        }

        try
        {
            await _taskService.UpdateAsync(taskDto);
            _logger.LogInformation("Task with ID {taskId} successfully updated", id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Task with ID {taskId} not found", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task with ID {taskId}", id);
            return StatusCode(500, "Internal server error occurred while updating the task");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager, Admin")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        _logger.LogInformation("Deleting task with ID {taskId}...", id);
        try
        {
            await _taskService.DeleteAsync(id);
            _logger.LogInformation("Task with ID {taskId} successfully deleted", id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Task with ID {taskId} not found", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task with ID {taskId}", id);
            return StatusCode(500, "Internal server error occurred while deleting the task");
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] TaskStatus status)
    {
        _logger.LogInformation("Updating status for task with ID {taskId}...", id);
        try
        {
            var existingTask = await _taskService.GetByIdAsync(id);
            if (existingTask == null)
            {
                _logger.LogWarning("Task with ID {taskId} not found", id);
                return NotFound();
            }

            if (!IsValidStatusTransition(existingTask.Status, status))
            {
                _logger.LogWarning("Invalid status transition from {currentStatus} to {newStatus}",
                    existingTask.Status, status);
                return BadRequest($"Invalid status transition from {existingTask.Status} to {status}");
            }

            // Use the new UpdateStatus method
            existingTask.UpdateStatus(status);
            await _taskService.UpdateAsync(existingTask);

            _logger.LogInformation("Task status successfully updated to {status}", status);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for task with ID {taskId}", id);
            return StatusCode(500, "Internal server error occurred while updating the task status");
        }
    }




    [HttpGet("{id}")]
    public async Task<ActionResult<TaskDTO>> GetTaskById(int id)
    {
        try
        {
            var taskDto = await _taskService.GetByIdAsync(id);
            return Ok(taskDto); // Return the TaskDTO in the response
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }
    


    private bool IsValidStatusTransition(TaskStatus currentStatus, TaskStatus newStatus)
    {
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