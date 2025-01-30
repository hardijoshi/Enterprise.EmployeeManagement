using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Enterprise.EmployeeManagement.DAL.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Get SMTP settings from configuration
            _smtpServer = _configuration["SmtpSettings:Host"];  
            _smtpPort = int.Parse(_configuration["SmtpSettings:Port"]);  
            _smtpUsername = _configuration["SmtpSettings:Username"];
            _smtpPassword = _configuration["SmtpSettings:Password"];
            _senderEmail = _configuration["SmtpSettings:Username"];
        }

        public async Task SendReminderEmailAsync(string recipientEmail, string taskTitle, DateTime deadline)
        {
            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
                };

                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail),
                    Subject = $"Overdue Task Reminder: {taskTitle}",
                    Body = GenerateEmailBody(taskTitle, deadline),
                    IsBodyHtml = true
                };

                message.To.Add(recipientEmail);

                await client.SendMailAsync(message);
                _logger.LogInformation("Reminder email sent successfully to {recipientEmail} for task {taskTitle}",
                    recipientEmail, taskTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder email to {recipientEmail} for task {taskTitle}",
                    recipientEmail, taskTitle);
                throw new ApplicationException("Failed to send reminder email", ex);
            }
        }

        private string GenerateEmailBody(string taskTitle, DateTime deadline)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Task Reminder</h2>
                    <p>This is a reminder about your overdue task:</p>
                    <div style='margin: 20px; padding: 20px; background-color: #f8f9fa; border-radius: 5px;'>
                        <h3>{taskTitle}</h3>
                        <p>The deadline for this task was: <strong>{deadline:MM/dd/yyyy hh:mm tt}</strong></p>
                    </div>
                    <p>Please update the task status or contact your manager if you need assistance.</p>
                    <p>Best regards,<br>Task Management System</p>
                </body>
                </html>";
        }
    }
}
