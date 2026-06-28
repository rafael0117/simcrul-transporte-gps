using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Auth;
using SIMCRUL.Common.DTOs.Shared;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class CuentaController : Controller
{
    private readonly ApiClient _apiClient;

    public CuentaController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public IActionResult OlvidoContrasena(string? origen = null)
    {
        ViewBag.Origen = origen;
        return View(new ForgotPasswordRequestDto());
    }

    [HttpPost]
    public async Task<IActionResult> OlvidoContrasena(ForgotPasswordRequestDto request, string? origen = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Origen = origen;
            return View(request);
        }

        try
        {
            var response = await _apiClient.PostAsync<ForgotPasswordRequestDto, MessageResponseDto>("Auth/forgot-password", request);
            TempData["SuccessMessage"] = response?.Message ?? "Si el correo existe, recibira instrucciones para restablecer su contrasena.";
            return RedirectToAction(nameof(OlvidoContrasena), new { origen });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"No se pudo enviar el correo: {ex.Message}");
            ViewBag.Origen = origen;
            return View(request);
        }
    }

    [HttpGet]
    public IActionResult RestablecerContrasena(string token)
    {
        return View(new ResetPasswordRequestDto { Token = token ?? string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> RestablecerContrasena(ResetPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            var response = await _apiClient.PostAsync<ResetPasswordRequestDto, MessageResponseDto>("Auth/reset-password", request);
            TempData["SuccessMessage"] = response?.Message ?? "La contrasena se actualizo correctamente.";
            return RedirectToAction("Login", "Home");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"No se pudo restablecer la contrasena: {ex.Message}");
            return View(request);
        }
    }
}
