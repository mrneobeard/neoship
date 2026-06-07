# NeoBeard.Exec

## Overview

Provides a wrapper for executing apps from the command line similar to
Go and Rust's Command implementation under os/exec.

There are additional commands to make it easier run scripts
using pwsh, bash, sh, windows cmd, deno, bun, node, ruby, or python.

## Usage

```csharp
using NeoBeard.Exec;

var output = new Command("dotnet")
    .WithArgs(["--version"])
    .Run();

Console.WriteLine(output.ExitCode);

output = new Command("dotnet")
    .WithArgs(["--version"])
    .Output();

// get the stdout as text.
Console.WriteLine(output.Text().Trim());
Console.WriteLine(output.Stdout.GetType().Name); // byte[]

// throws an exception if exit code is not zero
output.ThrowOnBadExit();

var pwsh = new PwshCommand();

pwsh.RunScript("echo 'Hello'");
pwsh.RunScript("./path/to/script.ps1");

// piping commands

var output = new Command(["echo", "my test"])
            .Pipe(["grep", "test"])
            .Pipe("cat")
            .Output();

Console.WriteLine(output.Text()); // my test
```
