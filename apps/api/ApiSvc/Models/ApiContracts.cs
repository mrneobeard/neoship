using System.Diagnostics;

namespace NeoShip.ApiSvc.Models;

/// <summary>
/// Represents metadata attached to every API response envelope.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var meta = ApiMeta.FromHttpContext(httpContext);
/// </code>
/// </remarks>
public sealed class ApiMeta
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiMeta"/> class.
    /// </summary>
    /// <param name="requestId">The request identifier used for support and audit correlation.</param>
    /// <param name="traceId">The distributed trace identifier, when available.</param>
    /// <param name="serverTime">The server response time.</param>
    /// <param name="query">The normalized query metadata, when available.</param>
    public ApiMeta(string requestId, string? traceId, DateTimeOffset serverTime, ApiQueryMeta? query = null)
    {
        RequestId = requestId;
        TraceId = traceId;
        ServerTime = serverTime;
        Query = query;
    }

    /// <summary>
    /// Gets the request identifier used for support and audit correlation.
    /// </summary>
    /// <value>The request identifier.</value>
    public string RequestId { get; }

    /// <summary>
    /// Gets the distributed trace identifier, when available.
    /// </summary>
    /// <value>The distributed trace identifier, or <see langword="null"/>.</value>
    public string? TraceId { get; }

    /// <summary>
    /// Gets the server response time.
    /// </summary>
    /// <value>The server response time.</value>
    public DateTimeOffset ServerTime { get; }

    /// <summary>
    /// Gets the normalized query metadata, when available.
    /// </summary>
    /// <value>The normalized query metadata, or <see langword="null"/>.</value>
    public ApiQueryMeta? Query { get; }

    /// <summary>
    /// Creates API metadata from the current HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="query">The normalized query metadata, when available.</param>
    /// <returns>An <see cref="ApiMeta"/> value.</returns>
    public static ApiMeta FromHttpContext(HttpContext httpContext, ApiQueryMeta? query = null)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        return new ApiMeta(httpContext.TraceIdentifier, traceId, DateTimeOffset.UtcNow, query);
    }
}

/// <summary>
/// Represents normalized query metadata echoed in API responses.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var query = new ApiQueryMeta(filters, sorts, expands, 50, null);
/// </code>
/// </remarks>
public sealed class ApiQueryMeta
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiQueryMeta"/> class.
    /// </summary>
    /// <param name="filter">The normalized filter values.</param>
    /// <param name="sort">The normalized sort fields.</param>
    /// <param name="expand">The normalized expansion names.</param>
    /// <param name="limit">The page limit.</param>
    /// <param name="cursor">The opaque cursor, when provided.</param>
    public ApiQueryMeta(
        IReadOnlyDictionary<string, string>? filter,
        IReadOnlyList<string>? sort,
        IReadOnlyList<string>? expand,
        int limit,
        string? cursor)
    {
        Filter = filter ?? new Dictionary<string, string>();
        Sort = sort ?? [];
        Expand = expand ?? [];
        Limit = limit;
        Cursor = cursor;
    }

    /// <summary>
    /// Gets the normalized filter values.
    /// </summary>
    /// <value>The normalized filter values.</value>
    public IReadOnlyDictionary<string, string> Filter { get; }

    /// <summary>
    /// Gets the normalized sort fields.
    /// </summary>
    /// <value>The normalized sort fields.</value>
    public IReadOnlyList<string> Sort { get; }

    /// <summary>
    /// Gets the normalized expansion names.
    /// </summary>
    /// <value>The normalized expansion names.</value>
    public IReadOnlyList<string> Expand { get; }

    /// <summary>
    /// Gets the page limit.
    /// </summary>
    /// <value>The page limit.</value>
    public int Limit { get; }

    /// <summary>
    /// Gets the opaque cursor, when provided.
    /// </summary>
    /// <value>The opaque cursor, or <see langword="null"/>.</value>
    public string? Cursor { get; }
}

/// <summary>
/// Represents a successful single-resource API response envelope.
/// </summary>
/// <typeparam name="TData">The data payload type.</typeparam>
/// <remarks>
/// Example:
/// <code>
/// var envelope = new ApiEnvelope&lt;UserResponse&gt;(user, meta);
/// </code>
/// </remarks>
public sealed class ApiEnvelope<TData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEnvelope{TData}"/> class.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <param name="meta">The response metadata.</param>
    public ApiEnvelope(TData? data, ApiMeta meta)
    {
        Data = data;
        Meta = meta;
    }

    /// <summary>
    /// Gets the response data.
    /// </summary>
    /// <value>The response data.</value>
    public TData? Data { get; }

    /// <summary>
    /// Gets the response metadata.
    /// </summary>
    /// <value>The response metadata.</value>
    public ApiMeta Meta { get; }
}

/// <summary>
/// Represents pagination metadata for collection API responses.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var pagination = new ApiPagination(50, nextCursor, null, hasMore: true);
/// </code>
/// </remarks>
public sealed class ApiPagination
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiPagination"/> class.
    /// </summary>
    /// <param name="limit">The page limit used by the server.</param>
    /// <param name="nextCursor">The cursor for the next page, when available.</param>
    /// <param name="previousCursor">The cursor for the previous page, when available.</param>
    /// <param name="hasMore">A value indicating whether more data exists.</param>
    public ApiPagination(int limit, string? nextCursor, string? previousCursor, bool hasMore)
    {
        Limit = limit;
        NextCursor = nextCursor;
        PreviousCursor = previousCursor;
        HasMore = hasMore;
    }

    /// <summary>
    /// Gets the page limit used by the server.
    /// </summary>
    /// <value>The page limit.</value>
    public int Limit { get; }

    /// <summary>
    /// Gets the cursor for the next page, when available.
    /// </summary>
    /// <value>The next cursor, or <see langword="null"/>.</value>
    public string? NextCursor { get; }

    /// <summary>
    /// Gets the cursor for the previous page, when available.
    /// </summary>
    /// <value>The previous cursor, or <see langword="null"/>.</value>
    public string? PreviousCursor { get; }

    /// <summary>
    /// Gets a value indicating whether more data exists.
    /// </summary>
    /// <value><see langword="true"/> when another page exists; otherwise, <see langword="false"/>.</value>
    public bool HasMore { get; }
}

/// <summary>
/// Represents a successful collection API response envelope.
/// </summary>
/// <typeparam name="TData">The collection item type.</typeparam>
/// <remarks>
/// Example:
/// <code>
/// var envelope = new ApiCollectionEnvelope&lt;RoleResponse&gt;(roles, pagination, meta);
/// </code>
/// </remarks>
public sealed class ApiCollectionEnvelope<TData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiCollectionEnvelope{TData}"/> class.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <param name="pagination">The pagination metadata.</param>
    /// <param name="meta">The response metadata.</param>
    public ApiCollectionEnvelope(IReadOnlyList<TData> data, ApiPagination pagination, ApiMeta meta)
    {
        Data = data;
        Pagination = pagination;
        Meta = meta;
    }

    /// <summary>
    /// Gets the response data.
    /// </summary>
    /// <value>The response data.</value>
    public IReadOnlyList<TData> Data { get; }

    /// <summary>
    /// Gets the pagination metadata.
    /// </summary>
    /// <value>The pagination metadata.</value>
    public ApiPagination Pagination { get; }

    /// <summary>
    /// Gets the response metadata.
    /// </summary>
    /// <value>The response metadata.</value>
    public ApiMeta Meta { get; }
}

/// <summary>
/// Represents a stable API error payload.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var error = new ApiError("permission_denied", "Permission denied.", details);
/// </code>
/// </remarks>
public sealed class ApiError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiError"/> class.
    /// </summary>
    /// <param name="code">The stable machine-readable error code.</param>
    /// <param name="message">The safe human-readable error message.</param>
    /// <param name="details">The safe error details, when available.</param>
    public ApiError(string code, string message, IReadOnlyDictionary<string, object?>? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }

    /// <summary>
    /// Gets the stable machine-readable error code.
    /// </summary>
    /// <value>The stable machine-readable error code.</value>
    public string Code { get; }

    /// <summary>
    /// Gets the safe human-readable error message.
    /// </summary>
    /// <value>The safe human-readable error message.</value>
    public string Message { get; }

    /// <summary>
    /// Gets the safe error details, when available.
    /// </summary>
    /// <value>The safe error details, or <see langword="null"/>.</value>
    public IReadOnlyDictionary<string, object?>? Details { get; }
}

/// <summary>
/// Represents an API error response envelope.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var envelope = new ApiErrorEnvelope(error, meta);
/// </code>
/// </remarks>
public sealed class ApiErrorEnvelope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiErrorEnvelope"/> class.
    /// </summary>
    /// <param name="error">The response error.</param>
    /// <param name="meta">The response metadata.</param>
    public ApiErrorEnvelope(ApiError error, ApiMeta meta)
    {
        Error = error;
        Meta = meta;
    }

    /// <summary>
    /// Gets the response error.
    /// </summary>
    /// <value>The response error.</value>
    public ApiError Error { get; }

    /// <summary>
    /// Gets the response metadata.
    /// </summary>
    /// <value>The response metadata.</value>
    public ApiMeta Meta { get; }
}