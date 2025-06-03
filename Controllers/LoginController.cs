using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Presentacion.Models;

namespace Presentacion.Controllers;

public class LoginController : Controller
{
    private readonly ILogger<LoginController> _logger;

    public LoginController(ILogger<LoginController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var token = Request.Cookies["access_token"];
        ViewBag.Mensaje = $"Token recibido correctamente desde el controlador {token}";

        
        if (!string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        // Lógica para obtener el token desde Keycloak
        var client = new HttpClient();
        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", "microservice-auth" },
            { "client_secret", "vTYV2rYe5NuKrbULzj5oUXNZCzzPKqix" },
            { "username", username },
            { "password", password }
        };


        var response = await client.PostAsync("http://localhost:9090/realms/springboot-microservice-realm/protocol/openid-connect/token",
            new FormUrlEncodedContent(parameters));

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Error al obtener el token";
            ModelState.AddModelError("", "Usuario o contraseña inválidos.");
            return View("Index");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = System.Text.Json.JsonDocument.Parse(json).RootElement;

        var accessToken = tokenData.GetProperty("access_token").GetString();

        // Guardar en cookies seguras
        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
        });


        return RedirectToAction("Index", "Home");
    }
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return RedirectToAction("Index");
    }


}
