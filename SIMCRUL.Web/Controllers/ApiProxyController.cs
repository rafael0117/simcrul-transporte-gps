using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace SIMCRUL.Web.Controllers;

[Route("api/{**path}")]
public class ApiProxyController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ApiProxyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH")]
    public async Task<IActionResult> Forward(string path, CancellationToken cancellationToken)
    {
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5272/api/";
        var targetUri = new Uri(new Uri(apiBaseUrl), path + Request.QueryString);
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), targetUri);

        if (Request.ContentLength > 0)
        {
            request.Content = new StreamContent(Request.Body);
            if (!string.IsNullOrWhiteSpace(Request.ContentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            }
        }

        if (Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization.ToArray());
        }

        using var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        Response.StatusCode = (int)response.StatusCode;
        return File(content, contentType);
    }
}
