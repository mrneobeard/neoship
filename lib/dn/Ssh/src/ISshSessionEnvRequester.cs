using System.Threading;
using System.Threading.Tasks;

namespace NeoBeard.Ssh;

/// <summary>
/// Defines the transport interaction required to send SSH env requests.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// // Example implementation shape.
/// sealed class Requester : ISshSessionEnvRequester
/// {
///     public ValueTask&lt;bool&gt; SetEnvAsync(string name, string value, CancellationToken cancellationToken = default)
///     {
///         return ValueTask.FromResult(true);
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface ISshSessionEnvRequester
{
    /// <summary>
    /// Sends an SSH env request for a single key/value pair.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <param name="value">The environment variable value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A value indicating whether the remote side accepted the request.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ISshSessionEnvRequester requester = new Requester();
    /// bool accepted = await requester.SetEnvAsync("LANG", "C.UTF-8");
    /// </code>
    /// </example>
    /// </remarks>
    ValueTask<bool> SetEnvAsync(string name, string value, CancellationToken cancellationToken = default);
}