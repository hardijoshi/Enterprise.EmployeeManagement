﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Enterprise.EmployeeManagement.core.MailService
{
    public interface IEmailService
    {
        Task SendReminderEmailAsync(string recipientEmail, string taskTitle, DateTime deadline);
    }
}
