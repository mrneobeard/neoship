namespace NeoBeard.Extras;

/// <summary>
/// Represents the ArrayExtensions class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public static class ArrayExtensions
{
    /// <summary>
    /// Clears a specified range of elements in the array, setting them to their default value.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    /// <param name="array">The array to clear.</param>
    /// <param name="startIndex">The zero-based starting index of the range to clear.</param>
    /// <param name="length">The number of elements to clear.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the array is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the start index or length is out of range.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the specified range exceeds the bounds of the array.
    /// </exception>
    public static void Clear<T>(this T[] array, int startIndex = 0, int length = -1)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        if (startIndex < 0 || startIndex >= array.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index is out of range.");

        if (length < 0)
            length = array.Length - startIndex;

        if (length < 0 || startIndex + length > array.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Length is out of range.");

        Array.Clear(array, startIndex, length);
    }
}