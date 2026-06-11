using System.Text;

using NeoShip.ApiSvc.Models;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Provides shared endpoint response helpers.
/// </summary>
/// <example>
/// <code>
/// return EndpointResults.Error(httpContext, 404, "not_found", "Resource not found.");
/// </code>
/// </example>
internal static class EndpointResults
{
    /// <summary>
    /// Creates a successful API envelope.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="data">The response data.</param>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <returns>The <see cref="ApiEnvelope{TData}"/> response envelope.</returns>
    internal static ApiEnvelope<T> Envelope<T>(HttpContext httpContext, T? data)
        => new(data, ApiMeta.FromHttpContext(httpContext));

    /// <summary>
    /// Creates a canonical API error result.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="code">The public error code.</param>
    /// <param name="message">The public error message.</param>
    /// <param name="details">The optional error details.</param>
    /// <returns>The JSON <see cref="IResult"/> error response.</returns>
    internal static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    /// <summary>
    /// Creates a validation error result.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="fields">The field validation errors.</param>
    /// <returns>The JSON <see cref="IResult"/> validation response.</returns>
    internal static IResult ValidationError(HttpContext httpContext, IReadOnlyDictionary<string, string[]> fields)
        => Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = fields });

    /// <summary>
    /// Creates a not-found error result.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>The JSON <see cref="IResult"/> not-found response.</returns>
    internal static IResult NotFound(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status404NotFound, "not_found", "Resource not found.");

    /// <summary>
    /// Encodes an offset cursor.
    /// </summary>
    /// <param name="offset">The zero-based offset.</param>
    /// <returns>The encoded cursor.</returns>
    internal static string EncodeCursor(int offset)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(offset.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    /// <summary>
    /// Decodes an offset cursor.
    /// </summary>
    /// <param name="cursor">The encoded cursor.</param>
    /// <param name="offset">The decoded offset.</param>
    /// <returns><see langword="true"/> when decoding succeeded; otherwise <see langword="false"/>.</returns>
    internal static bool TryDecodeCursor(string cursor, out int offset)
    {
        offset = 0;
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            return int.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out offset) && offset >= 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
