using System.Text;

using Microsoft.Extensions.Configuration;

using NeoBeard.Crypto;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Protects identity-provider client secrets before database storage.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var encrypted = protector.Encrypt("client-secret");
/// </code>
/// </remarks>
public sealed class IdentityProviderSecretProtector
{
    private const string EncryptionKeyPath = "Auth:IdentityProviders:EncryptionKey";

    private readonly string? encryptionKey;

    /// <summary>
    /// Initializes a new <see cref="IdentityProviderSecretProtector"/> instance.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public IdentityProviderSecretProtector(IConfiguration configuration)
    {
        this.encryptionKey = configuration[EncryptionKeyPath];
    }

    /// <summary>
    /// Encrypts a client secret for storage.
    /// </summary>
    /// <param name="secret">The plaintext secret.</param>
    /// <returns>The encrypted secret bytes, or an empty array when no secret is supplied.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a secret is supplied without a configured encryption key.</exception>
    public byte[] Encrypt(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return [];
        }

        var provider = this.CreateProvider();
        var plaintext = Encoding.UTF8.GetBytes(secret);
        try
        {
            return provider.Encrypt(plaintext);
        }
        finally
        {
            Array.Clear(plaintext, 0, plaintext.Length);
        }
    }

    /// <summary>
    /// Decrypts a stored client secret.
    /// </summary>
    /// <param name="encryptedSecret">The encrypted secret bytes.</param>
    /// <returns>The plaintext secret, or <see langword="null"/> when no secret is stored.</returns>
    public string? Decrypt(byte[] encryptedSecret)
    {
        if (encryptedSecret.Length == 0)
        {
            return null;
        }

        var provider = this.CreateProvider();
        var plaintext = provider.Decrypt(encryptedSecret);
        return Encoding.UTF8.GetString(plaintext);
    }

    private AesGcmEncryptionProvider CreateProvider()
    {
        if (string.IsNullOrWhiteSpace(this.encryptionKey))
        {
            throw new InvalidOperationException($"Identity provider secret encryption key is not configured: {EncryptionKeyPath}.");
        }

        return new AesGcmEncryptionProvider(new AesGcmEncryptionProviderOptions().WithKey(this.encryptionKey));
    }
}