using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Presentacion.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;


namespace Presentacion.Controllers;

public class MarketplaceController : Controller
{
    private readonly ILogger<MarketplaceController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public MarketplaceController(ILogger<MarketplaceController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
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

        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync("http://localhost:8080/api/inventory");

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "No se pudo obtener el inventario";
            return View(new List<InventoryItem>());
        }

        var json = await response.Content.ReadAsStringAsync();
        var inventario = JsonSerializer.Deserialize<List<InventoryItem>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return View(inventario);
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
