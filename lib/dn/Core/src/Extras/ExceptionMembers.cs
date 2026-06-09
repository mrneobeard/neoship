using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using NeoBeard.Results;

namespace NeoBeard.Extras;

/// <summary>
/// Represents the ExceptionMembers class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// ArgumentNullException.ThrowIfNull("value");
/// </code>
/// </example>
/// </remarks>
public static partial class ExceptionMembers
{
    extension(ArgumentNullException)
    {
#if !NET8_0_OR_GREATER
        /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
        /// <param name="argument">The reference type argument to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// ArgumentNullException.ThrowIfNull(value, nameof(value));
        /// </code>
        /// </example>
        /// </remarks>
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
                throw new ArgumentNullException(paramName);
        }

        /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
        /// <param name="argument">The pointer argument to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// unsafe
        /// {
        ///     void* ptr = null;
        ///     ArgumentNullException.ThrowIfNull(ptr, nameof(ptr));
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        [CLSCompliant(false)]
        public static unsafe void ThrowIfNull([NotNull] void* argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
                 throw new ArgumentNullException(paramName);
        }

        /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
        /// <param name="argument">The pointer argument to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// ArgumentNullException.ThrowIfNull(IntPtr.Zero, nameof(IntPtr.Zero));
        /// </code>
        /// </example>
        /// </remarks>
        public static void ThrowIfNull(IntPtr argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument == IntPtr.Zero)
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if <paramref name="value"/> is null or empty.
        /// </summary>
        /// <param name="value">The string value to validate as non-null or empty.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// ArgumentNullException.ThrowIfNullOrEmpty(input, nameof(input));
        /// </code>
        /// </example>
        /// </remarks>
        public static void ThrowIfNullOrEmpty([NotNullIfNotNull(nameof(value))] string? value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if <paramref name="value"/> is null, empty, or whitespace.
        /// </summary>
        /// <param name="value">The string value to validate as non-null, empty, or whitespace.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// ArgumentNullException.ThrowIfNullOrWhiteSpace(input, nameof(input));
        /// </code>
        /// </example>
        /// </remarks>
        public static void ThrowIfNullOrWhiteSpace([NotNullIfNotNull(nameof(value))] string? value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(paramName);
        }
#endif
    }
}