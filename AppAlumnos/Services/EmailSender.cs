using Microsoft.AspNetCore.Identity.UI.Services;

namespace AppAlumnos.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Por ahora no enviamos emails reales, solo cumplimos con la interfaz.
            return Task.CompletedTask;
        }
    }
}