namespace NeoBeard.Secrets;

/// <summary>
/// The contract for a secret generator that can create new secrets.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// ISecretGenerator generator = new SecretGenerator()
///     .Add(SecretCharacterSets.LatinAlphaUpperCase)
///     .Add(SecretCharacterSets.Digits);
/// char[] secret = generator.Generate(12);
/// </code>
/// </example>
/// </remarks>
public interface ISecretGenerator
{
    /// <summary>
    /// Add a character to the generator.
    /// </summary>
    /// <param name="character">The character to add.</param>
    /// <returns>The generator to chain methods.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// generator.Add('!');
    /// </code>
    /// </example>
    /// </remarks>
    ISecretGenerator Add(char character);

    /// <summary>
    /// Add a character set to the generator.
    /// </summary>
    /// <param name="characters">The characters to add.</param>
    /// <returns>The generator to chain methods.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// generator.Add("ABCDEF123456");
    /// </code>
    /// </example>
    /// </remarks>
    ISecretGenerator Add(IEnumerable<char> characters);

    /// <summary>
    /// Set the validator for the generator.
    /// </summary>
    /// <param name="validator">The validator function.</param>
    /// <returns>The generator to chain methods.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// generator.SetValidator(pwd => pwd.Length >= 8 &amp;&amp; pwd.Any(char.IsDigit));
    /// </code>
    /// </example>
    /// </remarks>
    ISecretGenerator SetValidator(Func<char[], bool> validator);

    /// <summary>
    /// Generates a new password and returns it as a char array.
    /// </summary>
    /// <param name="length">The length of the secret.</param>
    /// <returns>The generated secret.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// char[] secret = generator.Generate(16);
    /// </code>
    /// </example>
    /// </remarks>
    char[] Generate(int length);

    /// <summary>
    /// Generates a new password and returns it as a char array.
    /// </summary>
    /// <param name="length">The length of the secret.</param>
    /// <param name="characters">The allowed set of characters to use.</param>
    /// <param name="validator">The function used to validate the new secret.</param>
    /// <returns>The generated secret.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// char[] secret = generator.Generate(16, SecretCharacterSets.Digits.ToList(), pwd => true);
    /// </code>
    /// </example>
    /// </remarks>
    char[] Generate(int length, IList<char>? characters, Func<char[], bool>? validator);
}