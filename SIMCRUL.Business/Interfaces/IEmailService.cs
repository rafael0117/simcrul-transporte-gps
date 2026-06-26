using SIMCRUL.Business.Models;

namespace SIMCRUL.Business.Interfaces;

public interface IEmailService
{
    Task SendPasswordRecoveryEmailAsync(PasswordRecoveryEmailMessage message, CancellationToken cancellationToken = default);
    Task SendAlertEmailAsync(AlertEmailMessage message, CancellationToken cancellationToken = default);
}
