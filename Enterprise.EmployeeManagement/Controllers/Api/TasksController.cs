using Enterprise.EmployeeManagement.DAL.DTO;
using Enterprise.EmployeeManagement.DAL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Enterprise.EmployeeManagement.core.MailService;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.core.Interfaces;
using Enterprise.EmployeeManagement.core.Common.Responses;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;
    private readonly IEmployeeService _employeeService;
    private readonly IEmailService _emailService;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger, IEmployeeService employeeService, IEmailService emailService)
    {
        _taskService = taskService;
        _logger = logger;
        _employeeService = employeeService;
        _emailService = emailService;
        _logger.LogInformation("TasksController Started");

    }
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        _logger.LogInformation("Fetching all tasks...");
        try
        {
            var response = await _taskService.GetAllAsync();

            // Check if response is null or unsuccessful
            if (response == null || !response.Success)
            {
                _logger.LogInformation("Failed to retrieve tasks or response is null");
                return StatusCode(500, "Failed to retrieve tasks");
            }

            var tasks = response.Data; // Extract the IEnumerable<TaskDTO>

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
            //task.CalculateTaskProperties();
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
            return BadRequest(ResponseMessage<TaskDTO>.FailureResult("Invalid model state"));
        }

        try
        {
            taskDto.StartDate = DateTime.Parse(taskDto.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"));
            taskDto.DeadlineDate = DateTime.Parse(taskDto.DeadlineDate.ToString("yyyy-MM-ddTHH:mm:ss"));
            taskDto.ValidateDates();

            var response = await _taskService.CreateAsync(taskDto);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetTask), new { id = response.Data.TaskId }, response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ResponseMessage<TaskDTO>.FailureResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding task");
            return StatusCode(500, ResponseMessage<TaskDTO>.FailureResult("Internal server error occurred while creating the task"));
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
            return BadRequest(ResponseMessage<bool>.FailureResult("ID mismatch"));
        }

        try
        {
            taskDto.ValidateDates();
            var response = await _taskService.UpdateAsync(taskDto);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            _logger.LogInformation("Task with ID {taskId} successfully updated", id);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(ResponseMessage<bool>.FailureResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task with ID {taskId}", id);
            return StatusCode(500, ResponseMessage<bool>.FailureResult("Internal server error occurred while updating the task"));
        }
    }


    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager, Admin")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        _logger.LogInformation("Deleting task with ID {taskId}...", id);
        var response = await _taskService.DeleteAsync(id);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return StatusCode(500, response);
        }

        _logger.LogInformation("Task with ID {taskId} successfully deleted", id);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] TaskStatus status)
    {
        _logger.LogInformation("Updating status for task with ID {taskId}...", id);
        try
        {
            var taskResponse = await _taskService.GetByIdAsync(id);
            if (!taskResponse.Success)
            {
                return NotFound(taskResponse);
            }

            var existingTask = taskResponse.Data;
            if (!IsValidStatusTransition(existingTask.Status, status))
            {
                var errorResponse = ResponseMessage<bool>.FailureResult(
                    $"Invalid status transition from {existingTask.Status} to {status}");
                return BadRequest(errorResponse);
            }

            existingTask.UpdateStatus(status);
            var updateResponse = await _taskService.UpdateAsync(existingTask);

            if (!updateResponse.Success)
            {
                return StatusCode(500, updateResponse);
            }

            _logger.LogInformation("Task status successfully updated to {status}", status);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for task with ID {taskId}", id);
            return StatusCode(500, ResponseMessage<bool>.FailureResult("Internal server error occurred while updating the task status"));
        }
    }




    [HttpGet("{id}/details")]
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


    [HttpPost("{id}/send-reminder")]
    public async Task<IActionResult> SendReminder(int id)
    {
        try
        {
            var taskResponse = await _taskService.GetByIdAsync(id);
            if (!taskResponse.Success)
            {
                return NotFound(taskResponse);
            }

            var task = taskResponse.Data;
            var now = DateTime.UtcNow;
            var isOverdue = task.DeadlineDate.AddMinutes(-5) < now;

            if (!isOverdue)
            {
                return BadRequest(ResponseMessage<bool>.FailureResult(
                    $"Task is not overdue. Deadline: {task.DeadlineDate}, Current Time: {now}"));
            }

            var employee = await _employeeService.GetEmployeeByIdAsync(task.AssignedEmployeeId);
            if (employee == null)
            {
                return NotFound(ResponseMessage<bool>.FailureResult("Assigned employee not found"));
            }

            await _emailService.SendReminderEmailAsync(
                employee.Email,
                task.Title,
                task.DeadlineDate
            );

            return Ok(ResponseMessage<bool>.SuccessResult(true, "Reminder sent successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder for task {taskId}", id);
            return StatusCode(500, ResponseMessage<bool>.FailureResult("Failed to send reminder"));
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