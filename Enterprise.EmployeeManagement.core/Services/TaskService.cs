using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.DAL.DTO;
using Enterprise.EmployeeManagement.DAL.Repositories;
using Enterprise.EmployeeManagement.core.Interfaces;
using Enterprise.EmployeeManagement.core.Common.Responses;

namespace Enterprise.EmployeeManagement.core.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskMapper _taskMapper;
        private readonly IEmployeeRepository _employeeRepository;

        public TaskService(ITaskRepository taskRepository, ITaskMapper taskMapper, IEmployeeRepository employeeRepository)
        {
            _taskRepository = taskRepository;
            _taskMapper = taskMapper;
            _employeeRepository = employeeRepository;
        }

        public async Task<ResponseMessage<TaskDTO>> GetByIdAsync(int id)
        {
            try
            {
                var entity = await _taskRepository.GetTaskByIdAsync(id);
                if (entity == null)
                {
                    return ResponseMessage<TaskDTO>.FailureResult($"Task with ID {id} not found");
                }

                var taskDto = _taskMapper.MapToDTO(entity);
                return ResponseMessage<TaskDTO>.SuccessResult(taskDto);
            }
            catch (Exception ex)
            {
                return ResponseMessage<TaskDTO>.FailureResult($"Error retrieving task: {ex.Message}");
            }
        }

        public async Task<ResponseMessage<IEnumerable<TaskDTO>>> GetAllAsync()
        {
            try
            {
                var entities = await _taskRepository.GetAllTasksAsync();
                var taskDtos = _taskMapper.MapToDTOList(entities);

                foreach (var taskDto in taskDtos)
                {
                    taskDto.AssignedEmployeeName = await _employeeRepository.GetEmployeeNameById(taskDto.AssignedEmployeeId);
                    taskDto.ReviewerName = await _employeeRepository.GetEmployeeNameById(taskDto.ReviewerId);
                }

                return ResponseMessage<IEnumerable<TaskDTO>>.SuccessResult(taskDtos);
            }
            catch (Exception ex)
            {
                return ResponseMessage<IEnumerable<TaskDTO>>.FailureResult($"Error retrieving tasks: {ex.Message}");
            }
        }

        public async Task<ResponseMessage<TaskDTO>> CreateAsync(TaskDTO taskDto)
        {
            try
            {
                taskDto.ValidateDates();

                var entity = _taskMapper.MapToEntity(taskDto);
                var created = await _taskRepository.AddTaskAsync(entity);

                var createdTaskDto = _taskMapper.MapToDTO(created);
                createdTaskDto.AssignedEmployeeName = await _employeeRepository.GetEmployeeNameById(created.AssignedEmployeeId);
                createdTaskDto.ReviewerName = await _employeeRepository.GetEmployeeNameById(created.ReviewerId);

                return ResponseMessage<TaskDTO>.SuccessResult(createdTaskDto, "Task created successfully");
            }
            catch (ValidationException ex)
            {
                return ResponseMessage<TaskDTO>.FailureResult($"Validation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ResponseMessage<TaskDTO>.FailureResult($"Error creating task: {ex.Message}");
            }
        }

        public async Task<ResponseMessage<bool>> UpdateAsync(TaskDTO taskDto)
        {
            try
            {
                taskDto.ValidateDates();

                var existingTask = await _taskRepository.GetTaskByIdAsync(taskDto.TaskId);
                if (existingTask == null)
                {
                    return ResponseMessage<bool>.FailureResult($"Task with ID {taskDto.TaskId} not found");
                }

                if (existingTask.Status != TaskStatus.NotStarted && taskDto.StartDate != existingTask.StartDate)
                {
                    return ResponseMessage<bool>.FailureResult("Cannot modify start date for tasks that have already started");
                }

                if (existingTask.Status == TaskStatus.Completed && taskDto.DeadlineDate != existingTask.DeadlineDate)
                {
                    return ResponseMessage<bool>.FailureResult("Cannot modify deadline for completed tasks");
                }

                var entity = _taskMapper.MapToEntity(taskDto);
                await _taskRepository.UpdateTaskAsync(entity);

                return ResponseMessage<bool>.SuccessResult(true, "Task updated successfully");
            }
            catch (ValidationException ex)
            {
                return ResponseMessage<bool>.FailureResult($"Validation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ResponseMessage<bool>.FailureResult($"Error updating task: {ex.Message}");
            }
        }

        public async Task<ResponseMessage<bool>> DeleteAsync(int id)
        {
            try
            {
                await _taskRepository.DeleteTaskAsync(id);
                return ResponseMessage<bool>.SuccessResult(true, "Task deleted successfully");
            }
            catch (KeyNotFoundException)
            {
                return ResponseMessage<bool>.FailureResult($"Task with ID {id} not found");
            }
            catch (Exception ex)
            {
                return ResponseMessage<bool>.FailureResult($"Error deleting task: {ex.Message}");
            }
        }

        public async Task<ResponseMessage<IEnumerable<TaskDTO>>> GetTasksByEmployeeAsync(int employeeId)
        {
            try
            {
                if (!await _taskRepository.EmployeeExistsAsync(employeeId))
                {
                    return ResponseMessage<IEnumerable<TaskDTO>>.FailureResult($"Employee with ID {employeeId} not found");
                }

                var entities = await _taskRepository.GetAllTasksAsync();
                var employeeTasks = entities.Where(t => t.AssignedEmployeeId == employeeId);
                var taskDtos = _taskMapper.MapToDTOList(employeeTasks);

                foreach (var taskDto in taskDtos)
                {
                    taskDto.AssignedEmployeeName = await _employeeRepository.GetEmployeeNameById(taskDto.AssignedEmployeeId);
                    taskDto.ReviewerName = await _employeeRepository.GetEmployeeNameById(taskDto.ReviewerId);
                }

                return ResponseMessage<IEnumerable<TaskDTO>>.SuccessResult(taskDtos);
            }
            catch (Exception ex)
            {
                return ResponseMessage<IEnumerable<TaskDTO>>.FailureResult($"Error retrieving tasks: {ex.Message}");
            }
        }

        public async Task<ResponseMessage<bool>> CanEmployeeModifyTaskAsync(int employeeId, int taskId)
        {
            try
            {
                var task = await _taskRepository.GetTaskByIdAsync(taskId);
                if (task == null)
                {
                    return ResponseMessage<bool>.FailureResult($"Task with ID {taskId} not found");
                }

                bool canModify = (task.AssignedEmployeeId == employeeId || task.ReviewerId == employeeId)
                                && task.Status != TaskStatus.Completed;

                return ResponseMessage<bool>.SuccessResult(canModify);
            }
            catch (Exception ex)
            {
                return ResponseMessage<bool>.FailureResult($"Error checking task modification permissions: {ex.Message}");
            }
        }
    }
}