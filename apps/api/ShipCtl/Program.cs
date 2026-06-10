using System.Net.Http.Json;
using System.Text.Json;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return 0;
}

var command = args[0];
var options = ParseOptions(args.Skip(1).ToArray());

return command switch
{
    "bootstrap-admin" => await BootstrapAdminAsync(options),
    "login" => await LoginAsync(options),
    "orgs" when args.Length > 1 && args[1] == "list" => await ListOrganizationsAsync(ParseOptions(args.Skip(2).ToArray())),
    _ => UnknownCommand(command),
};

static async Task<int> BootstrapAdminAsync(Dictionary<string, string?> options)
{
    var apiUrl = Required(options, "api-url").TrimEnd('/');
    var rootToken = options.GetValueOrDefault("root-token") ?? Environment.GetEnvironmentVariable("NEO_ROOT_TOKEN");
    var email = Required(options, "email");
    var password = Required(options, "password");
    var name = options.GetValueOrDefault("name") ?? email;
    var orgSlug = options.GetValueOrDefault("org") ?? "default";
    var orgName = options.GetValueOrDefault("org-name") ?? "Default";

    if (string.IsNullOrWhiteSpace(rootToken))
    {
        Console.Error.WriteLine("Missing root token. Pass --root-token or set NEO_ROOT_TOKEN.");
        return 2;
    }

    if (!ValidateEmailAndPassword(email, password))
    {
        return 2;
    }

    using var http = NewHttp(apiUrl, null);
    using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/bootstrap-admin");
    request.Headers.Add("X-NeoShip-Root-Token", rootToken);
    request.Content = JsonContent.Create(new BootstrapAdminRequest(email, password, name, orgSlug, orgName));

    using var response = await http.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        return PrintApiError("Bootstrap failed", response, body);
    }

    var result = JsonSerializer.Deserialize<ApiEnvelope<BootstrapAdminResponse>>(body, JsonOptions());
    if (result?.Data is null)
    {
        Console.Error.WriteLine("Bootstrap failed: API returned an empty response.");
        return 1;
    }

    Console.WriteLine($"Bootstrapped admin user {result.Data.Email} for organization {result.Data.OrgSlug}.");
    return 0;
}

static async Task<int> LoginAsync(Dictionary<string, string?> options)
{
    var apiUrl = Required(options, "api-url").TrimEnd('/');
    var email = Required(options, "email");
    var password = Required(options, "password");
    if (!ValidateEmailAndPassword(email, password))
    {
        return 2;
    }

    using var http = NewHttp(apiUrl, null);
    using var response = await http.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        return PrintApiError("Login failed", response, body);
    }

    var session = TryReadSessionCookie(response);
    if (string.IsNullOrWhiteSpace(session))
    {
        Console.Error.WriteLine("Login failed: API did not return a session cookie.");
        return 1;
    }

    var result = JsonSerializer.Deserialize<ApiEnvelope<UserResponse>>(body, JsonOptions());
    Console.WriteLine($"Logged in as {result?.Data?.Email ?? email}.");
    Console.WriteLine($"Set NEOSHIP_SESSION to use authenticated commands:");
    Console.WriteLine($"export NEOSHIP_SESSION='{session}'");
    return 0;
}

static async Task<int> ListOrganizationsAsync(Dictionary<string, string?> options)
{
    var apiUrl = Required(options, "api-url").TrimEnd('/');
    var session = options.GetValueOrDefault("session") ?? Environment.GetEnvironmentVariable("NEOSHIP_SESSION");
    if (string.IsNullOrWhiteSpace(session))
    {
        Console.Error.WriteLine("Missing session. Pass --session or set NEOSHIP_SESSION from `shipctl login`.");
        return 2;
    }

    using var http = NewHttp(apiUrl, session);
    var query = BuildQuery(options, ["limit", "cursor", "sort", "filter[name]"]);
    using var response = await http.GetAsync($"/api/v1/orgs/{query}");
    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        return PrintApiError("List organizations failed", response, body);
    }

    var result = JsonSerializer.Deserialize<ApiCollectionEnvelope<OrganizationResponse>>(body, JsonOptions());
    if (result?.Data is null)
    {
        Console.Error.WriteLine("List organizations failed: API returned an empty response.");
        return 1;
    }

    foreach (var org in result.Data)
    {
        Console.WriteLine($"{org.Slug}\t{org.Name}\t{org.Status}");
    }

    if (!string.IsNullOrWhiteSpace(result.Pagination?.NextCursor))
    {
        Console.WriteLine($"next-cursor: {result.Pagination.NextCursor}");
    }

    return 0;
}

static HttpClient NewHttp(string apiUrl, string? session)
{
    var http = new HttpClient { BaseAddress = new Uri(apiUrl) };
    if (!string.IsNullOrWhiteSpace(session))
    {
        http.DefaultRequestHeaders.Add("Cookie", $"neoship_sid={session}");
    }

    return http;
}

static Dictionary<string, string?> ParseOptions(string[] values)
{
    var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < values.Length; i++)
    {
        var value = values[i];
        if (!value.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = value[2..];
        result[key] = i + 1 < values.Length && !values[i + 1].StartsWith("--", StringComparison.Ordinal) ? values[++i] : null;
    }

    return result;
}

static string Required(Dictionary<string, string?> options, string key)
{
    if (!options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
    {
        Console.Error.WriteLine($"Missing required option --{key}.");
        Environment.Exit(2);
    }

    return value!;
}

static bool ValidateEmailAndPassword(string email, string password)
{
    if (!IsValidEmail(email))
    {
        Console.Error.WriteLine("Email must be valid.");
        return false;
    }

    if (password.Length is < 12 or > 256)
    {
        Console.Error.WriteLine("Password must be 12 to 256 characters.");
        return false;
    }

    return true;
}

static bool IsValidEmail(string email)
{
    if (email.Length is < 3 or > 320)
    {
        return false;
    }

    var at = email.IndexOf('@', StringComparison.Ordinal);
    return at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1 && email[(at + 1)..].Contains('.', StringComparison.Ordinal);
}

static string? TryReadSessionCookie(HttpResponseMessage response)
{
    if (!response.Headers.TryGetValues("Set-Cookie", out var values))
    {
        return null;
    }

    foreach (var value in values)
    {
        var parts = value.Split(';', 2);
        const string prefix = "neoship_sid=";
        if (parts[0].StartsWith(prefix, StringComparison.Ordinal))
        {
            return parts[0][prefix.Length..];
        }
    }

    return null;
}

static string BuildQuery(Dictionary<string, string?> options, string[] keys)
{
    var parts = new List<string>();
    foreach (var key in keys)
    {
        if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }
    }

    return parts.Count == 0 ? string.Empty : $"?{string.Join('&', parts)}";
}

static int PrintApiError(string operation, HttpResponseMessage response, string body)
{
    Console.Error.WriteLine($"{operation}: {(int)response.StatusCode} {response.ReasonPhrase}");
    var message = ReadErrorMessage(body);
    if (!string.IsNullOrWhiteSpace(message))
    {
        Console.Error.WriteLine(message);
    }

    return 1;
}

static string? ReadErrorMessage(string body)
{
    try
    {
        var error = JsonSerializer.Deserialize<ApiErrorEnvelope>(body, JsonOptions());
        return error?.Error?.Message;
    }
    catch (JsonException)
    {
        return null;
    }
}

static JsonSerializerOptions JsonOptions()
    => new(JsonSerializerDefaults.Web);

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    PrintUsage();
    return 2;
}

static void PrintUsage()
{
    Console.WriteLine("NeoShip shipctl");
    Console.WriteLine("Usage:");
    Console.WriteLine("  bootstrap-admin --api-url <url> --email <email> --password <password> [--root-token <token>] [--name <name>] [--org default] [--org-name Default]");
    Console.WriteLine("  login --api-url <url> --email <email> --password <password>");
    Console.WriteLine("  orgs list --api-url <url> [--session <token>] [--limit <n>] [--cursor <cursor>] [--sort <sort>] [--filter[name] <name>]");
    Console.WriteLine();
    Console.WriteLine("Root token may also be supplied with NEO_ROOT_TOKEN.");
    Console.WriteLine("Authenticated commands may use NEOSHIP_SESSION from `shipctl login`.");
}

/// <summary>
/// Represents an admin bootstrap request sent to the API.
/// </summary>
/// <param name="Email">The admin email address.</param>
/// <param name="Password">The admin password.</param>
/// <param name="Name">The optional admin display name.</param>
/// <param name="Org">The optional organization slug.</param>
/// <param name="OrgName">The optional organization display name.</param>
/// <remarks>
/// Example:
/// <code>
/// var request = new BootstrapAdminRequest("admin@example.com", password, "Admin", "default", "Default");
/// </code>
/// </remarks>
internal sealed record BootstrapAdminRequest(string Email, string Password, string? Name, string? Org, string? OrgName);

/// <summary>
/// Represents a password login request sent to the API.
/// </summary>
/// <param name="Email">The login email address.</param>
/// <param name="Password">The login password.</param>
/// <remarks>
/// Example:
/// <code>
/// var request = new LoginRequest("admin@example.com", password);
/// </code>
/// </remarks>
internal sealed record LoginRequest(string Email, string Password);

/// <summary>
/// Represents an admin bootstrap response returned by the API.
/// </summary>
/// <param name="UserId">The bootstrapped user identifier.</param>
/// <param name="OrgId">The bootstrapped organization identifier.</param>
/// <param name="Email">The bootstrapped user email address.</param>
/// <param name="OrgSlug">The bootstrapped organization slug.</param>
/// <remarks>
/// Example:
/// <code>
/// Console.WriteLine(response.OrgSlug);
/// </code>
/// </remarks>
internal sealed record BootstrapAdminResponse(Guid UserId, Guid OrgId, string Email, string OrgSlug);

/// <summary>
/// Represents the current authenticated user returned by the API.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Email">The user email address.</param>
/// <param name="Name">The user display name.</param>
/// <param name="AvatarUrl">The optional user avatar URL.</param>
/// <remarks>
/// Example:
/// <code>
/// Console.WriteLine(response.Email);
/// </code>
/// </remarks>
internal sealed record UserResponse(Guid Id, string Email, string Name, string? AvatarUrl);

/// <summary>
/// Represents an organization returned by the API.
/// </summary>
/// <param name="Id">The organization identifier.</param>
/// <param name="Name">The organization display name.</param>
/// <param name="Slug">The organization slug.</param>
/// <param name="Status">The organization status.</param>
/// <param name="TenantMode">The tenant mode.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
/// <param name="UpdatedAt">The optional UTC update timestamp.</param>
/// <remarks>
/// Example:
/// <code>
/// Console.WriteLine(response.Slug);
/// </code>
/// </remarks>
internal sealed record OrganizationResponse(Guid Id, string Name, string Slug, string Status, string TenantMode, DateTime CreatedAt, DateTime? UpdatedAt);

/// <summary>
/// Represents a successful API envelope.
/// </summary>
/// <typeparam name="T">The response data type.</typeparam>
/// <param name="Data">The response data.</param>
/// <param name="Meta">The response metadata.</param>
/// <remarks>
/// Example:
/// <code>
/// var data = envelope.Data;
/// </code>
/// </remarks>
internal sealed record ApiEnvelope<T>(T? Data, ApiMeta Meta);

/// <summary>
/// Represents a collection API envelope.
/// </summary>
/// <typeparam name="T">The response item type.</typeparam>
/// <param name="Data">The response items.</param>
/// <param name="Pagination">The response pagination metadata.</param>
/// <param name="Meta">The response metadata.</param>
/// <remarks>
/// Example:
/// <code>
/// foreach (var item in envelope.Data) { }
/// </code>
/// </remarks>
internal sealed record ApiCollectionEnvelope<T>(IReadOnlyList<T> Data, ApiPagination? Pagination, ApiMeta Meta);

/// <summary>
/// Represents collection pagination metadata.
/// </summary>
/// <param name="Limit">The requested item limit.</param>
/// <param name="NextCursor">The cursor for the next page, when present.</param>
/// <param name="HasMore">A value indicating whether another page exists.</param>
/// <remarks>
/// Example:
/// <code>
/// Console.WriteLine(pagination.NextCursor);
/// </code>
/// </remarks>
internal sealed record ApiPagination(int Limit, string? NextCursor, bool HasMore);

/// <summary>
/// Represents API response metadata.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="TraceId">The trace identifier, when available.</param>
/// <param name="ServerTime">The server response time.</param>
/// <remarks>
/// Example:
/// <code>
/// Console.WriteLine(meta.RequestId);
/// </code>
/// </remarks>
internal sealed record ApiMeta(string RequestId, string? TraceId, DateTimeOffset ServerTime);

/// <summary>
/// Represents an API error envelope.
/// </summary>
/// <param name="Error">The API error.</param>
/// <param name="Meta">The response metadata.</param>
/// <remarks>
/// Example:
/// <code>
/// Console.Error.WriteLine(envelope.Error.Message);
/// </code>
/// </remarks>
internal sealed record ApiErrorEnvelope(ApiError Error, ApiMeta Meta);

/// <summary>
/// Represents an API error payload.
/// </summary>
/// <param name="Code">The stable error code.</param>
/// <param name="Message">The safe error message.</param>
/// <param name="Details">The optional error details.</param>
/// <remarks>
/// Example:
/// <code>
/// Console.Error.WriteLine(error.Code);
/// </code>
/// </remarks>
internal sealed record ApiError(string Code, string Message, Dictionary<string, JsonElement>? Details);