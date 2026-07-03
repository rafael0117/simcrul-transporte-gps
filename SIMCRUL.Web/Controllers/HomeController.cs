using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Auth;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _apiClient;
    private readonly IConfiguration _configuration;

    public HomeController(ApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        if (SessionAuthHelper.IsAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["HideChrome"] = true;
        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (SessionAuthHelper.IsAuthenticated(HttpContext.Session))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["HideChrome"] = true;
        PopulateRecaptchaViewBag();
        return View(new LoginRequestDto());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            ViewData["HideChrome"] = true;
            PopulateRecaptchaViewBag();
            return View(request);
        }

        try
        {
            var response = await _apiClient.PostAsync<LoginRequestDto, AuthResponseDto>("Auth/login", request);
            if (response == null || string.IsNullOrWhiteSpace(response.Token))
            {
                ModelState.AddModelError(string.Empty, "No se recibio una respuesta valida del servidor.");
                ViewData["HideChrome"] = true;
                PopulateRecaptchaViewBag();
                return View(request);
            }

            HttpContext.Session.SetString("Token", response.Token);
            HttpContext.Session.SetString("Rol", response.Rol);
            HttpContext.Session.SetString("Username", response.Username);
            HttpContext.Session.SetString("Nombres", response.Nombres);
            HttpContext.Session.SetString("Apellidos", response.Apellidos);

            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"No se pudo iniciar sesion: {ex.Message}");
            ViewData["HideChrome"] = true;
            PopulateRecaptchaViewBag();
            return View(request);
        }
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private void PopulateRecaptchaViewBag()
    {
        ViewBag.RecaptchaEnabled = _configuration.GetValue<bool>("Recaptcha:Enabled") &&
            !string.IsNullOrWhiteSpace(_configuration["Recaptcha:SiteKey"]);
        ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"] ?? string.Empty;
        ViewBag.RecaptchaAction = _configuration["Recaptcha:LoginAction"] ?? "login";
        ViewBag.RecaptchaVersion = _configuration["Recaptcha:Version"] ?? "v2";
    }
}
