using System;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Enterprise.EmployeeManagement.core.MailService
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Get SMTP settings from configuration with null checks and default values
            _smtpServer = configuration["SmtpSettings:Host"] ??
                throw new InvalidOperationException("SMTP host configuration is missing");

            if (!int.TryParse(configuration["SmtpSettings:Port"], out _smtpPort))
                throw new InvalidOperationException("Invalid SMTP port configuration");

            _smtpUsername = configuration["SmtpSettings:Username"] ??
                throw new InvalidOperationException("SMTP username configuration is missing");

            _smtpPassword = configuration["SmtpSettings:Password"] ??
                throw new InvalidOperationException("SMTP password configuration is missing");

            _senderEmail = _smtpUsername; // Using username as sender email
        }

        public async Task SendReminderEmailAsync(string recipientEmail, string taskTitle, DateTime deadline)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
                throw new ArgumentException("Recipient email cannot be empty", nameof(recipientEmail));

            if (string.IsNullOrWhiteSpace(taskTitle))
                throw new ArgumentException("Task title cannot be empty", nameof(taskTitle));

            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    Timeout = 10000 // 10 second timeout
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

                _logger.LogInformation(
                    "Reminder email sent successfully to {RecipientEmail} for task {TaskTitle}",
                    recipientEmail, taskTitle);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex,
                    "SMTP error sending reminder email to {RecipientEmail} for task {TaskTitle}. Error: {ErrorMessage}",
                    recipientEmail, taskTitle, ex.Message);
                throw new ApplicationException($"SMTP error sending reminder email: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error sending reminder email to {RecipientEmail} for task {TaskTitle}. Error: {ErrorMessage}",
                    recipientEmail, taskTitle, ex.Message);
                throw new ApplicationException("Failed to send reminder email", ex);
            }
        }

        private static string GenerateEmailBody(string taskTitle, DateTime deadline)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Task Reminder</h2>
                        <p>This is a reminder about your overdue task:</p>
                        <div style='margin: 20px 0; padding: 20px; background-color: #f8f9fa; border-radius: 5px; border-left: 4px solid #dc3545;'>
                            <h3 style='color: #dc3545; margin-top: 0;'>{taskTitle}</h3>
                            <p>The deadline for this task was: <strong>{deadline:MM/dd/yyyy hh:mm tt}</strong></p>
                        </div>
                        <p>Please update the task status or contact your manager if you need assistance.</p>
                        <hr style='border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='color: #666; font-size: 12px;'>
                            Best regards,<br>
                            Task Management System
                        </p>
                    </div>
                </body>
                </html>";
        }
    }
}