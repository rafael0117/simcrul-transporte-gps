using Microsoft.AspNetCore.Mvc;
using SIMCRUL.Common.DTOs.Auth;
using SIMCRUL.Common.DTOs.Passenger;
using SIMCRUL.Web.Infrastructure;
using SIMCRUL.Web.Models.Passenger;
using SIMCRUL.Web.Services;

namespace SIMCRUL.Web.Controllers;

public class PassengerController : Controller
{
    private readonly ApiClient _apiClient;
    private readonly IConfiguration _configuration;

    public PassengerController(ApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (SessionAuthHelper.IsPassengerAuthenticated(HttpContext.Session))
        {
            return RedirectToLocal(returnUrl, "Index");
        }

        ViewBag.ReturnUrl = returnUrl;
        PopulateRecaptchaViewBag();
        return View(new LoginRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestDto request, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            PopulateRecaptchaViewBag();
            return View(request);
        }

        try
        {
            var response = await _apiClient.PostAsync<LoginRequestDto, AuthResponseDto>("Auth/login", request);
            if (response == null || string.IsNullOrWhiteSpace(response.Token))
            {
                ModelState.AddModelError(string.Empty, "No se recibio una respuesta valida del servidor.");
                ViewBag.ReturnUrl = returnUrl;
                PopulateRecaptchaViewBag();
                return View(request);
            }

            SetAuthSession(response);
            return RedirectToLocal(returnUrl, "Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"No se pudo iniciar sesion: {ex.Message}");
            ViewBag.ReturnUrl = returnUrl;
            PopulateRecaptchaViewBag();
            return View(request);
        }
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (SessionAuthHelper.IsPassengerAuthenticated(HttpContext.Session))
        {
            return RedirectToLocal(returnUrl, "Index");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new RegisterRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestDto request, string? returnUrl = null)
    {
        request.Rol = "Pasajero";

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(request);
        }

        try
        {
            var response = await _apiClient.PostAsync<RegisterRequestDto, AuthResponseDto>("Auth/register", request);
            if (response == null || string.IsNullOrWhiteSpace(response.Token))
            {
                ModelState.AddModelError(string.Empty, "No se recibio una respuesta valida del servidor.");
                ViewBag.ReturnUrl = returnUrl;
                return View(request);
            }

            SetAuthSession(response);
            TempData["SuccessMessage"] = "Tu cuenta de pasajero fue creada correctamente.";
            return RedirectToLocal(returnUrl, "Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"No se pudo registrar la cuenta: {ex.Message}");
            ViewBag.ReturnUrl = returnUrl;
            return View(request);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Requests()
    {
        if (!SessionAuthHelper.IsAuthenticated(HttpContext.Session))
        {
            return RedirectToAction(nameof(Login), new { returnUrl = Url.Action(nameof(Requests), "Passenger") });
        }

        var model = await BuildRequestsPageViewModelAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(PassengerRequestCreateDto form)
    {
        if (!SessionAuthHelper.IsAuthenticated(HttpContext.Session))
        {
            return RedirectToAction(nameof(Login), new { returnUrl = Url.Action(nameof(Requests), "Passenger") });
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildRequestsPageViewModelAsync(form);
            return View("Requests", invalidModel);
        }

        try
        {
            await _apiClient.PostAsync<PassengerRequestCreateDto, PassengerRequestDto>("PassengerRequests", form);
            TempData["SuccessMessage"] = "Tu solicitud fue registrada correctamente.";
            return RedirectToAction(nameof(Requests));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"No se pudo registrar la solicitud: {ex.Message}");
            var errorModel = await BuildRequestsPageViewModelAsync(form);
            return View("Requests", errorModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadRoutesPdf()
    {
        if (!SessionAuthHelper.IsAuthenticated(HttpContext.Session))
        {
            return RedirectToAction(nameof(Login), new { returnUrl = Url.Action(nameof(DownloadRoutesPdf), "Passenger") });
        }

        var bytes = await _apiClient.GetBytesAsync("Report/routes-pdf");
        if (bytes == null)
        {
            return NotFound("No se pudo generar el catalogo de rutas.");
        }

        return File(bytes, "application/pdf", $"CatalogoRutas_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Index));
    }

    private async Task<PassengerRequestsPageViewModel> BuildRequestsPageViewModelAsync(PassengerRequestCreateDto? form = null)
    {
        var routes = await _apiClient.GetAsync<List<PassengerRouteOptionViewModel>>("Dashboard/routes") ?? new List<PassengerRouteOptionViewModel>();
        var requests = await _apiClient.GetAsync<List<PassengerRequestDto>>("PassengerRequests/mine") ?? new List<PassengerRequestDto>();

        return new PassengerRequestsPageViewModel
        {
            Form = form ?? new PassengerRequestCreateDto
            {
                EmailContacto = HttpContext.Session.GetString("PassengerEmail"),
                TelefonoContacto = HttpContext.Session.GetString("PassengerPhone")
            },
            Routes = routes,
            Requests = requests
        };
    }

    private void SetAuthSession(AuthResponseDto response)
    {
        HttpContext.Session.SetString("Token", response.Token);
        HttpContext.Session.SetString("Rol", response.Rol);
        HttpContext.Session.SetString("Username", response.Username);
        HttpContext.Session.SetString("Nombres", response.Nombres);
        HttpContext.Session.SetString("Apellidos", response.Apellidos);
    }

    private void PopulateRecaptchaViewBag()
    {
        ViewBag.RecaptchaEnabled = _configuration.GetValue<bool>("Recaptcha:Enabled") &&
            !string.IsNullOrWhiteSpace(_configuration["Recaptcha:SiteKey"]);
        ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"] ?? string.Empty;
        ViewBag.RecaptchaAction = _configuration["Recaptcha:LoginAction"] ?? "login";
        ViewBag.RecaptchaVersion = _configuration["Recaptcha:Version"] ?? "v2";
    }

    private IActionResult RedirectToLocal(string? returnUrl, string defaultAction)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(defaultAction);
    }
}
