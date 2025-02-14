﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Enterprise.EmployeeManagement.DAL.DTO
{
    public class TaskDTO
    {
        public int TaskId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
        public string StatusDisplayName => Status.ToString();
        public int AssignedEmployeeId { get; set; }
        public int ReviewerId { get; set; }
        public string AssignedEmployeeName { get; set; }
        public string ReviewerName { get; set; }
        public string EmployeeName { get; set; }

        [Required(ErrorMessage = "Start Date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Deadline Date is required")]
        public DateTime DeadlineDate { get; set; }

        public void SetDates(DateTime start, DateTime end)
        {
            StartDate = DateTime.SpecifyKind(start, DateTimeKind.Local);
            DeadlineDate = DateTime.SpecifyKind(end, DateTimeKind.Local);
        }

        private DateTime AdjustToLocalTime(DateTime date)
        {
            return DateTime.SpecifyKind(date, DateTimeKind.Local);
        }
        public decimal CompletionPercentage =>
    Status == TaskStatus.Completed ? 100m :
    Status == TaskStatus.NotStarted ? 0m :
    (DeadlineDate == StartDate) ? 0m : // Prevent division by zero
    Math.Min(100m, Math.Max(0m,
        (decimal)(DateTime.UtcNow - StartDate).TotalDays /
        (decimal)(DeadlineDate - StartDate).TotalDays * 100m));

        public bool IsOverdue => DateTime.Now > DeadlineDate && Status != TaskStatus.Completed;


        public void ValidateDates()
        {
            if (StartDate == default)
            {
                throw new ValidationException("Start date must be set");
            }
            if (DeadlineDate == default)
            {
                throw new ValidationException("Deadline date must be set");
            }

            var localStartDate = AdjustToLocalTime(StartDate);
            var localDeadlineDate = AdjustToLocalTime(DeadlineDate);

            if (localDeadlineDate < localStartDate)
            {
                throw new ValidationException("Deadline date cannot be earlier than start date");
            }
        }

        public bool IsValidDateRange()
        {
            try
            {
                ValidateDates();
                return true;
            }
            catch (ValidationException)
            {
                return false;
            }
        }
    

        private static readonly Dictionary<int, DateTime> StatusChangeDates = new Dictionary<int, DateTime>();

        public DateTime? StatusChangeDate
        {
            get
            {
                if (Status == TaskStatus.NotStarted)
                {
                    return null;
                }
                if (StatusChangeDates.ContainsKey(TaskId))
                {
                    return StatusChangeDates[TaskId];
                }
                return null;
            }
        }

        public void UpdateStatus(TaskStatus newStatus)
        {
            if (Status != newStatus)
            {
                Status = newStatus;
                if (newStatus != TaskStatus.NotStarted)
                {
                    StatusChangeDates[TaskId] = DateTime.UtcNow;
                }
                else
                {
                    if (StatusChangeDates.ContainsKey(TaskId))
                    {
                        StatusChangeDates.Remove(TaskId);
                    }
                }
            }
        }
    }

}
