namespace NeoBeard.Secrets;

/// <summary>
/// Provides common character sets for secret generation.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var charset = SecretCharacterSets.LatinAlphaUpperCase + SecretCharacterSets.Digits;
/// </code>
/// </example>
/// </remarks>
public static class SecretCharacterSets
{
    /// <summary>
    /// Uppercase Latin alphabet characters (A-Z).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string chars = SecretCharacterSets.LatinAlphaUpperCase;
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly string LatinAlphaUpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// Lowercase Latin alphabet characters (a-z).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string chars = SecretCharacterSets.LatinAlphaLowerCase;
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly string LatinAlphaLowerCase = "abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Numeric digits (0-9).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string chars = SecretCharacterSets.Digits;
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly string Digits = "0123456789";

    /// <summary>
    /// The hyphen character (-).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string chars = SecretCharacterSets.Hyphen;
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly string Hyphen = "-";

    /// <summary>
    /// The underscore character (_).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string chars = SecretCharacterSets.Underscore;
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly string Underscore = "_";

    /// <summary>
    /// Bracket characters ([]{}()&lt;&gt;).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string chars = SecretCharacterSets.Brackets;
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly string Brackets = "[]{}()<>";

    /// <summary>
    /// A set of special characters that are generally safe for most systems.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string chars = SecretCharacterSets.SpecialSafe;
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly string SpecialSafe = "~`#@|:;^-_/";

    /// <summary>
    /// A broader set of special characters.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string chars = SecretCharacterSets.Special;
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly string Special = "~`&%$?#@*+=|\\,.:;^";

    /// <summary>
    /// The space character.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string chars = SecretCharacterSets.Space;
    /// </code>
    /// </example>
    /// </remarks>
    public static readonly string Space = " ";
}