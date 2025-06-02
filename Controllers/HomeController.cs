using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Presentacion.Models;
using System.IdentityModel.Tokens.Jwt;


namespace Presentacion.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var token = Request.Cookies["access_token"];
        ViewBag.Mensaje = $"Token recibido correctamente desde el controlador {token}";

        
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Index", "Login");
        }

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        ViewBag.Name = name;

        // Si todo está bien, mostrar la página
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
}
