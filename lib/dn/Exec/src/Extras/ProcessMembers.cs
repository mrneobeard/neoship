using System.Diagnostics;

using NeoBeard.Exec;

namespace NeoBeard.Extras;

public static class ProcessMembers
{
    extension(Process process)
    {
#pragma warning disable S2325, SA1101
#if NETLEGACY
/// <summary>
        /// Waits asynchronously for the process to exit.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token. If invoked, the task will return
        /// immediately as canceled.</param>
        /// <returns>A Task representing waiting for the process to end.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// using var process = Process.Start("dotnet", "--version");
        /// await process.WaitForExitAsync();
        /// Assert.True(process.HasExited);
        /// </code>
        /// </example>
        /// </remarks>
        public Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            if (process.HasExited)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;

            process.Exited += (sender, args) => tcs.TrySetResult(true);
            if (cancellationToken != default)
                cancellationToken.Register(() => tcs.SetCanceled());

            return process.HasExited ? Task.CompletedTask : tcs.Task;
        }

#endif
    }

    extension(Process)
    {
        public static Command Cmd(CommandArgs args)
        {
            return new Command(args);
        }

        public static Command Cmd(CommandOptions options)
        {
            return new Command(options);
        }

        public static Output Output(CommandArgs args)
        {
            return new Command(args).Output();
        }

        public static Output Output(CommandOptions options)
        {
            return new Command(options).Output();
        }

        public static ValueTask<Output> OutputAsync(CommandArgs args, CancellationToken cancellationToken = default)
        {
            return new Command(args).OutputAsync(cancellationToken);
        }

        public static ValueTask<Output> OutputAsync(CommandOptions options, CancellationToken cancellationToken = default)
        {
            return new Command(options).OutputAsync(cancellationToken);
        }

        public static Output Run(CommandArgs args)
        {
            return new Command(args).Run();
        }

        public static Output Run(CommandOptions options)
        {
            return new Command(options).Run();
        }

        public static ValueTask<Output> RunAsync(CommandArgs args, CancellationToken cancellationToken = default)
        {
            return new Command(args).RunAsync(cancellationToken);
        }

        public static ValueTask<Output> RunAsync(CommandOptions options, CancellationToken cancellationToken = default)
        {
            return new Command(options).RunAsync(cancellationToken);
        }

        public static ChildProcess Spawn(CommandArgs args)
        {
            return new Command(args).Spawn();
        }

        public static ChildProcess Spawn(CommandOptions options)
        {
            return new Command(options).Spawn();
        }
    }
}