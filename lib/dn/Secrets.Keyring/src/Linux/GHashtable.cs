using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace NeoBeard.Secrets.Linux;

/// <summary>
/// A wrapper around the GLib GHashTable.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var ht = new GHashtable();
/// </code>
/// </example>
/// </remarks>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter")]
internal sealed class GHashtable
{
    private readonly IntPtr handle;

    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GHashtable"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ht = new GHashtable();
    /// </code>
    /// </example>
    /// </remarks>
    public GHashtable()
    {
        this.handle = g_hash_table_new(IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GHashtable"/> class with an existing handle.
    /// </summary>
    /// <param name="handle">The handle to the native GHashTable.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ht = new GHashtable(ptr);
    /// </code>
    /// </example>
    /// </remarks>
    public GHashtable(IntPtr handle)
    {
        this.handle = handle;
    }

    /// <summary>
    /// Gets the handle to the native GHashTable.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var handle = ht.Handle;
    /// </code>
    /// </example>
    /// </remarks>
    public IntPtr Handle => this.handle;

    /// <summary>
    /// Gets the number of elements in the hashtable.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var count = ht.Count;
    /// </code>
    /// </example>
    /// </remarks>
    public int Count
    {
        get
        {
            if (this.handle == IntPtr.Zero)
                return 0;

            return g_hash_table_size(this.handle);
        }
    }

    /// <summary>
    /// Implicitly converts a pointer to a <see cref="GHashtable"/>.
    /// </summary>
    /// <param name="handle">The native handle.</param>
    /// <returns>A new <see cref="GHashtable"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// GHashtable ht = ptr;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator GHashtable(IntPtr handle)
    {
        return new GHashtable(handle);
    }

    /// <summary>
    /// Implicitly converts a <see cref="GHashtable"/> to a pointer.
    /// </summary>
    /// <param name="hashtable">The hashtable instance.</param>
    /// <returns>The native handle.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IntPtr ptr = ht;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator IntPtr(GHashtable hashtable)
    {
        return hashtable.handle;
    }

    /// <summary>
    /// Gets a value from the hashtable as a string.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The string value, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var value = ht.GetAsString(keyPtr);
    /// </code>
    /// </example>
    /// </remarks>
    public string? GetAsString(IntPtr key)
    {
        if (this.handle == IntPtr.Zero)
            return null;

        IntPtr resultPtr = g_hash_table_lookup(this.handle, key);
        return Marshal.PtrToStringAnsi(resultPtr);
    }

    /// <summary>
    /// Gets a value from the hashtable as a pointer.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The pointer value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var valuePtr = ht.Get(keyPtr);
    /// </code>
    /// </example>
    /// </remarks>
    public IntPtr Get(IntPtr key)
    {
        if (this.handle == IntPtr.Zero)
            return IntPtr.Zero;

        return g_hash_table_lookup(this.handle, key);
    }

    /// <summary>
    /// Sets a value in the hashtable.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The string value to set.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ht.Set(keyPtr, "MyValue");
    /// </code>
    /// </example>
    /// </remarks>
    public void Set(IntPtr key, string value)
    {
        if (this.handle == IntPtr.Zero)
            return;

        IntPtr valuePtr = Marshal.StringToHGlobalAnsi(value);

        g_hash_table_replace(this.handle, key, valuePtr);
    }

    /// <summary>
    /// Frees the native hashtable.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ht.Free();
    /// </code>
    /// </example>
    /// </remarks>
    public void Free()
    {
        if (this.disposed)
            return;

        if (this.handle != IntPtr.Zero)
            g_hash_table_destroy(this.handle);

        this.disposed = true;
    }

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int g_hash_table_size(IntPtr hashTable);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_hash_table_replace(IntPtr hashTable, IntPtr key, IntPtr value);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr g_hash_table_new(IntPtr hashFunc, IntPtr equalFunc);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_hash_table_destroy(IntPtr hashTable);

    [DllImport(Libraries.Glib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr g_hash_table_lookup(IntPtr hashTable, IntPtr key);
}