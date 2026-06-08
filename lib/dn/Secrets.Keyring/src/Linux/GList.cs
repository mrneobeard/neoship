using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace NeoBeard.Secrets.Linux;

/// <summary>
/// A wrapper around the GLib GList.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var list = new GList(ptr);
/// </code>
/// </example>
/// </remarks>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter")]
public sealed class GList
{
    private readonly IntPtr list;

    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GList"/> class.
    /// </summary>
    /// <param name="list">The native pointer to the GList.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var list = new GList(ptr);
    /// </code>
    /// </example>
    /// </remarks>
    internal GList(IntPtr list)
    {
        this.list = list;
    }

    /// <summary>
    /// Gets the number of elements in the list.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var count = list.Count;
    /// </code>
    /// </example>
    /// </remarks>
    public int Count
    {
        get
        {
            if (this.list == IntPtr.Zero)
                return 0;

            return g_list_length(this.list);
        }
    }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The native pointer to the element data.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var data = list[0];
    /// </code>
    /// </example>
    /// </remarks>
    public IntPtr this[int index]
    {
        get
        {
            if (index < 0 || index >= this.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return g_list_nth_data(this.list, index);
        }
    }

    /// <summary>
    /// Implicitly converts a pointer to a <see cref="GList"/>.
    /// </summary>
    /// <param name="list">The native pointer.</param>
    /// <returns>A new <see cref="GList"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// GList list = ptr;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator GList(IntPtr list)
    {
        return new GList(list);
    }

    /// <summary>
    /// Frees the native list.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// list.Free();
    /// </code>
    /// </example>
    /// </remarks>
    public void Free()
    {
        if (this.disposed)
            return;

        if (this.list != IntPtr.Zero)
            g_list_free(this.list);

        this.disposed = true;
    }

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_list_nth_data(IntPtr list, int n);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void g_list_free(IntPtr list);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int g_list_length(IntPtr list);
}