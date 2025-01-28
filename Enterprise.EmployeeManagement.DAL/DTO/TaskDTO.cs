using System;
using System.Collections.Generic;
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

        //public bool IsOverdue { get; set; }
        //public int DaysInCurrentStatus { get; set; }

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
                //CalculateTaskProperties();
            }
        }

        //public void CalculateTaskProperties()
        //{
        //    DateTime? statusDate = StatusChangeDate;
        //    if (statusDate.HasValue)
        //    {
        //        var timeSpan = DateTime.UtcNow - statusDate.Value;
        //        DaysInCurrentStatus = timeSpan.Days;
        //    }
        //    else
        //    {
        //        DaysInCurrentStatus = 0;
        //    }

        //    IsOverdue = Status == TaskStatus.Working && DaysInCurrentStatus > 1;
        //}
    }
}
