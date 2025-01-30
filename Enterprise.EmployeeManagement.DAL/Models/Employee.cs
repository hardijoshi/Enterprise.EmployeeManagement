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

        [Required(ErrorMessage = "Please enter your first name")]

        [Index(1)]
        public virtual string FirstName { get; set; }

        [Required(ErrorMessage = "Please enter your last name")]

        [Index(2)]
        public virtual string LastName { get; set; }

        [Index(3)]
        [Required(ErrorMessage = "Please select your role")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public virtual RoleType Role { get; set; }

        [IgnoreFormat]
        public virtual string RoleInString => Role.ToString();

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

        [IgnoreFormat]
        public virtual IList<TaskEntity> Tasks { get; set; }
    }

    public enum RoleType
    {
        Admin = 0,
        Manager = 1,
        Employee = 2,
    }
}
