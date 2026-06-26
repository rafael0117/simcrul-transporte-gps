using SIMCRUL.Common.DTOs.Auth;

namespace SIMCRUL.Business.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(ForgotPasswordRequestDto request, string? requesterIp, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}
