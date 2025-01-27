using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ZeroFormatter;

namespace Enterprise.EmployeeManagement.DAL.Models
{
    [ZeroFormattable]
    public class Employee
    {
        [Index(0)]
        public virtual int Id { get; set; }

        [Index(1)]
        [Required(ErrorMessage = "Please enter your first name")]
        public virtual string FirstName { get; set; }

        [Index(2)]
        [Required(ErrorMessage = "Please enter your last name")]
        public virtual string LastName { get; set; }

        [Index(3)]
        [Required(ErrorMessage = "Please select your role")]
        public virtual string Role { get; set; }

        [Index(4)]
        [Required(ErrorMessage = "Please enter your email")]
        public virtual string Email { get; set; }

        [Index(5)]
        [Required(ErrorMessage = "Please enter your password")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public virtual string Password { get; set; }

        [Index(6)]
        [Required(ErrorMessage = "Please enter your mobile number")]
        public virtual string MobileNumber { get; set; }

        [Index(7)]
        [JsonIgnore]
        public virtual IList<TaskEntity> Tasks { get; set; }
    }
}