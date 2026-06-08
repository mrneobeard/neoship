namespace NeoBeard.Secrets.Win32;

/// <summary>
/// Represents a credential record from the Windows Credential Manager.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var secret = new WinCredSecret(WinCredType.Generic, "MyService", "MyAccount", "MyPassword", "MyComment");
/// </code>
/// </example>
/// </remarks>
public struct WinCredSecret
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WinCredSecret"/> struct.
    /// </summary>
    /// <param name="type">The type of the credential.</param>
    /// <param name="service">The service or target name.</param>
    /// <param name="account">The account or user name.</param>
    /// <param name="password">The password or secret blob.</param>
    /// <param name="comment">The optional comment.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new WinCredSecret(WinCredType.Generic, "MyService", "MyAccount", "MyPassword", "MyComment");
    /// </code>
    /// </example>
    /// </remarks>
    public WinCredSecret(WinCredType type, string service, string? account, string? password, string? comment)
    {
        this.Type = type;
        this.Service = service;
        this.Account = account;
        this.Password = password;
        this.Comment = comment;
    }

    /// <summary>
    /// Gets the type of the credential.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var type = secret.Type;
    /// </code>
    /// </example>
    /// </remarks>
    public WinCredType Type { get; }

    /// <summary>
    /// Gets the service or target name.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var service = secret.Service;
    /// </code>
    /// </example>
    /// </remarks>
    public string Service { get; }

    /// <summary>
    /// Gets the account or user name.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var account = secret.Account;
    /// </code>
    /// </example>
    /// </remarks>
    public string? Account { get; }

    /// <summary>
    /// Gets the password or secret blob.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var password = secret.Password;
    /// </code>
    /// </example>
    /// </remarks>
    public string? Password { get; }

    /// <summary>
    /// Gets the optional comment.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var comment = secret.Comment;
    /// </code>
    /// </example>
    /// </remarks>
    public string? Comment { get; }
}