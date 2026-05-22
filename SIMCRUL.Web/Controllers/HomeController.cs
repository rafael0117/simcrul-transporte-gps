using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Auth;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _apiClient;

    public HomeController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("Token") != null)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            var response = await _apiClient.PostAsync<LoginRequestDto, AuthResponseDto>("Auth/login", request);
            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                HttpContext.Session.SetString("Token", response.Token);
                HttpContext.Session.SetString("Rol", response.Rol);
                HttpContext.Session.SetString("Username", response.Username);
                HttpContext.Session.SetString("Nombres", response.Nombres);
                HttpContext.Session.SetString("Apellidos", response.Apellidos);

                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError("", "Respuesta inválida del servidor.");
            return View(request);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al iniciar sesión: {ex.Message}");
            return View(request);
        }
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
