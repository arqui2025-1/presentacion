using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Presentacion.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Principal;



namespace Presentacion.Controllers;

public class RegisterController : Controller
{
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(ILogger<RegisterController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
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

    public async Task<IActionResult> CreateUser(string form_username, string form_email, string form_firstName, string form_lastName, string password)
    {
        var token = await GetTokenAsync();
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Crear usuario
        var user = new 
        {
            username = form_username,
            enabled = true,
            email = form_email,
            firstName = form_firstName,
            lastName = form_lastName,
            credentials = new[]
            {
            new {
                type = "password",
                value = password,
                temporary = false
            }
        }
        };

        var jsonUser = JsonSerializer.Serialize(user);
        var contentUser = new StringContent(jsonUser, System.Text.Encoding.UTF8, "application/json");

        var createResponse = await client.PostAsync("http://localhost:9090/admin/realms/springboot-microservice-realm/users", contentUser);

        if (!createResponse.IsSuccessStatusCode)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Error al crear usuario: {error}");
            return View();
        }

        Console.WriteLine("Usuario creado correctamente.");

        // 2. Obtener ID del usuario creado
        string userId = await GetUserIdByUsername(client, "nuevo_usuario");
        if (userId == null)
        {
            Console.WriteLine("No se pudo obtener el ID del usuario.");
            return View();
        }

        // 3. Obtener ID interno del cliente microservice-auth
        string clientInternalId = await GetClientId(client, "microservice-auth");
        if (clientInternalId == null)
        {
            Console.WriteLine("No se pudo obtener el ID del cliente.");
            return View();
        }

        // 4. Obtener rol AGRICULTOR del cliente
        var role = await GetClientRole(client, clientInternalId, "AGRICULTOR");
        if (role == null)
        {
            Console.WriteLine("No se pudo obtener el rol AGRICULTOR.");
            return View();
        }

        // 5. Asignar rol AGRICULTOR al usuario
        var assignSuccess = await AssignClientRoleToUser(client, userId, clientInternalId, role);
        if (assignSuccess)
        {
            Console.WriteLine("Rol AGRICULTOR asignado correctamente.");

            return RedirectToAction("Index", "Home");
        }
        else
        {
            Console.WriteLine("Error asignando rol AGRICULTOR.");
            return View();
        }
    }



    private async Task<string> GetUserIdByUsername(HttpClient client, string username)
    {
        var response = await client.GetAsync($"http://localhost:9090/admin/realms/springboot-microservice-realm/users?username={username}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var users = JsonDocument.Parse(json).RootElement;

        foreach (var user in users.EnumerateArray())
        {
            if (user.GetProperty("username").GetString() == username)
            {
                return user.GetProperty("id").GetString();
            }
        }
        return null;
    }


    private async Task<string> GetClientId(HttpClient client, string clientId)
    {
        var response = await client.GetAsync($"http://localhost:9090/admin/realms/springboot-microservice-realm/clients?clientId={clientId}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var clients = JsonDocument.Parse(json).RootElement;

        foreach (var c in clients.EnumerateArray())
        {
            if (c.GetProperty("clientId").GetString() == clientId)
            {
                return c.GetProperty("id").GetString();
            }
        }
        return null;
    }


    private async Task<JsonElement?> GetClientRole(HttpClient client, string clientInternalId, string roleName)
    {
        var response = await client.GetAsync($"http://localhost:9090/admin/realms/springboot-microservice-realm/clients/{clientInternalId}/roles/{roleName}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var role = JsonDocument.Parse(json).RootElement;
        return role;
    }


    private async Task<bool> AssignClientRoleToUser(HttpClient client, string userId, string clientInternalId, JsonElement? role)
    {
        if (role == null) return false;

        var rolesToAdd = new[]
        {
        new {
            id = role.Value.GetProperty("id").GetString(),
            name = role.Value.GetProperty("name").GetString()
        }
    };

        var rolesJson = JsonSerializer.Serialize(rolesToAdd);
        var content = new StringContent(rolesJson, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            $"http://localhost:9090/admin/realms/springboot-microservice-realm/users/{userId}/role-mappings/clients/{clientInternalId}",
            content);

        return response.IsSuccessStatusCode;
    }


    public async Task<string> GetTokenAsync()
    {
        var client = new HttpClient();
        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", "microservice-auth" },
            { "username", "admin" },
            { "password", "admin" }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await client.PostAsync("http://localhost:9090/realms/springboot-microservice-realm/protocol/openid-connect/token", content);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(json);
        var token = result.RootElement.GetProperty("access_token").GetString();
        return token;
    }


}
