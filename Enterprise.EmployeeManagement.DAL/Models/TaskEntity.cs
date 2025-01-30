using System.ComponentModel.DataAnnotations;
using Enterprise.EmployeeManagement.DAL.Models;
using Enterprise.EmployeeManagement.DAL.DTO;
using System;
public enum TaskStatus
{
    NotStarted = 0,
    Working = 1,
    Pending = 2,
    Completed = 3
}

public class TaskEntity
{
    [Key]
    public int TaskId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(100, ErrorMessage = "Title can't be longer than 100 characters.")]
    public string Title { get; set; }

    [StringLength(500, ErrorMessage = "Description can't be longer than 500 characters.")]
    public string Description { get; set; }

    [Required]
    public TaskStatus Status { get; set; }
    //public string StatusInString => Status.ToString();


    [Required(ErrorMessage = "Assigned Employee is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Assigned Employee Id must be a valid positive number.")]
    public int AssignedEmployeeId { get; set; }

    [Required(ErrorMessage = "Reviewer is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Reviewer Id must be a valid positive number.")]
    public int ReviewerId { get; set; }

    [Required(ErrorMessage = "Start Date is required.")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow; // Default to current time

    [Required(ErrorMessage = "Deadline Date is required.")]
    public DateTime DeadlineDate { get; set; }



    public virtual Employee AssignedEmployee { get; set; }
    public virtual Employee Reviewer { get; set; }
}
