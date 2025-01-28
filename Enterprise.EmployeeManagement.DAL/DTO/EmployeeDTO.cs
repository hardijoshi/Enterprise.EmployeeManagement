using System;
using System.Collections.Generic;
using System.Text;


namespace Enterprise.EmployeeManagement.DAL.DTO
{
    public class EmployeeDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }
    }

}
