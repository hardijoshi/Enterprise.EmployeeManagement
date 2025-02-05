using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Enterprise.EmployeeManagement.DAL.DTO;
using Enterprise.EmployeeManagement.DAL.Repositories;

namespace Enterprise.EmployeeManagement.DAL.Services
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

        public async Task<TaskDTO> GetByIdAsync(int id)
        {
            var entity = await _taskRepository.GetTaskByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            var taskDto = _taskMapper.MapToDTO(entity);
            //taskDto.CalculateTaskProperties();
            return taskDto;
        }



        public async Task<IEnumerable<TaskDTO>> GetAllAsync()
        {
            var entities = await _taskRepository.GetAllTasksAsync();
            var taskDtos = _taskMapper.MapToDTOList(entities);

            foreach (var taskDto in taskDtos)
            {
                taskDto.AssignedEmployeeName = await _employeeRepository.GetEmployeeNameById(taskDto.AssignedEmployeeId);
                taskDto.ReviewerName = await _employeeRepository.GetEmployeeNameById(taskDto.ReviewerId);
                //taskDto.CalculateTaskProperties();
            }

            return taskDtos;
        }

        public async Task<TaskDTO> CreateAsync(TaskDTO taskDto)
        {
            // Validate dates before creating
            taskDto.ValidateDates();

            // Ensure StartDate is not in the past for new tasks
            //if (taskDto.StartDate < DateTime.UtcNow)
            //{
            //    taskDto.StartDate = DateTime.UtcNow;
            //}

            var entity = _taskMapper.MapToEntity(taskDto);
            var created = await _taskRepository.AddTaskAsync(entity);

            var createdTaskDto = _taskMapper.MapToDTO(created);
            createdTaskDto.AssignedEmployeeName = await _employeeRepository.GetEmployeeNameById(created.AssignedEmployeeId);
            createdTaskDto.ReviewerName = await _employeeRepository.GetEmployeeNameById(created.ReviewerId);

            return createdTaskDto;
        }

        public async Task UpdateAsync(TaskDTO taskDto)
        {
            // Validate dates before updating
            taskDto.ValidateDates();

            // Get existing task to check if dates can be modified
            var existingTask = await _taskRepository.GetTaskByIdAsync(taskDto.TaskId);
            if (existingTask == null)
            {
                throw new KeyNotFoundException($"Task with ID {taskDto.TaskId} not found");
            }

            // Don't allow modifying start date if task has already started
            if (existingTask.Status != TaskStatus.NotStarted &&
                taskDto.StartDate != existingTask.StartDate)
            {
                throw new ValidationException("Cannot modify start date for tasks that have already started");
            }

            // Don't allow modifying deadline for completed tasks
            if (existingTask.Status == TaskStatus.Completed &&
                taskDto.DeadlineDate != existingTask.DeadlineDate)
            {
                throw new ValidationException("Cannot modify deadline for completed tasks");
            }

            var entity = _taskMapper.MapToEntity(taskDto);
            await _taskRepository.UpdateTaskAsync(entity);
        }

        public async Task DeleteAsync(int id)
        {
            await _taskRepository.DeleteTaskAsync(id);
        }

        public async Task<IEnumerable<TaskDTO>> GetTasksByEmployeeAsync(int employeeId)
        {
            // First verify if employee exists
            if (!await _taskRepository.EmployeeExistsAsync(employeeId))
            {
                throw new KeyNotFoundException($"Employee with ID {employeeId} not found");
            }

            var entities = await _taskRepository.GetAllTasksAsync();
            var employeeTasks = entities.Where(t => t.AssignedEmployeeId == employeeId);

            // Map to DTOs
            var taskDtos = _taskMapper.MapToDTOList(employeeTasks);

            foreach (var taskDto in taskDtos)
            {
                taskDto.AssignedEmployeeName = await _employeeRepository.GetEmployeeNameById(taskDto.AssignedEmployeeId);
                taskDto.ReviewerName = await _employeeRepository.GetEmployeeNameById(taskDto.ReviewerId);
            }

            return taskDtos;
        }

        public async Task<bool> CanEmployeeModifyTaskAsync(int employeeId, int taskId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            return task != null &&
                   (task.AssignedEmployeeId == employeeId ||
                    task.ReviewerId == employeeId) &&
                   task.Status != TaskStatus.Completed;
        }
    }
}
