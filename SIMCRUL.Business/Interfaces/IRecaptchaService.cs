namespace SIMCRUL.Business.Interfaces;

public interface IRecaptchaService
{
    Task<bool> ValidateAsync(string? token, string? remoteIp, string expectedAction, CancellationToken cancellationToken);
}
