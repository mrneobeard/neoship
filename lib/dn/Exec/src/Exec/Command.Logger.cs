using System.Diagnostics;

namespace NeoBeard.Exec;

/// <summary>
/// Partial class for <see cref="Command"/> that provides logging and global command configuration.
/// </summary>
public partial class Command
{
    /// <summary>
    /// Gets or sets a global action that is invoked on every <see cref="ProcessStartInfo"/> before a process starts.
    /// </summary>
    /// <value>An action to modify process start information, or <c>null</c> for no action.</value>
    internal static Action<ProcessStartInfo>? GlobalWriteCommand { get; set; }

    /// <summary>
    /// Sets a global action that is invoked on every <see cref="ProcessStartInfo"/> before a process starts.
    /// This can be used for logging or modifying process configuration globally.
    /// </summary>
    /// <param name="writeCommand">An action to modify process start information, or <c>null</c> to remove the global action.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Command.SetGlobalWriteCommand(si =>
    /// {
    ///     Console.WriteLine($"Starting: {si.FileName} {si.Arguments}");
    /// });
    ///
    /// // All subsequent commands will log their execution
    /// var output = new Command(["dotnet", "--version"]).Output();
    ///
    /// // To disable:
    /// Command.SetGlobalWriteCommand(null);
    /// </code>
    /// </example>
    /// </remarks>
    public static void SetGlobalWriteCommand(Action<ProcessStartInfo>? writeCommand)
    {
        GlobalWriteCommand = writeCommand;
    }
}