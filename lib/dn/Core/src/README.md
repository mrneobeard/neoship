# NeoBeard.Core

## Overview

NeoBeard Core extends the the .NET Base Class Library with key functionality such as:

- Result type
- Extension methods and members under the Extras namespaces for strings,
  spans, string builder, arrays, tasks, etc.
- The static FileSystem class which provides an fs module similar
  to other std libraries including functions missing posix functions
  like copy, chown, chmod, stat, etc.
- Enhanced logic for working with environments such as expanding
  bash style variables, appending/prepending paths to the environment
  path, etc

## Usage

## FileSystem examples

```csharp
using static NeoBeard.IO.FileSystem;

if (Environment.IsPrivilegedProcess)
{
    Chown("/home/user/file", "root", "root");
}

// copies recursively
CopyDir("/home/user/.local/bin", "/home/user/tmp/bin", true);
```

## Result example

```csharp
using NeoBeard.Results;

using static NeoBeard.Results.Result;

var strRes = Ok("10");

Console.WriteLine(strRes.IsOk);
Console.WriteLine(strRes.IsError);
Console.WriteLine(strRes.Value);
Console.WriteLine(strRes.Map(o => int.Parse(o)));

var bad = Fail<string>(new InvalidOperationException("Failed to get value"));

Console.WriteLine(strRes.IsOk);
Console.WriteLine(strRes.IsError);
Console.WriteLine(strRes.Error.Message);

var value = strRes.OrDefault("fallback string");
Console.WriteLine(value);

var res = TryCatch(() => {
    NeoBeard.IO.FileSystem.Chown("/opt/app", "user", "user");
});

Console.WriteLine(strRes.IsOk);
Console.WriteLine(strRes.IsError);

```

## Env Example

```cs
using NeoBeard;

Console.WriteLine(Env.Expand("home is '${HOME}' on linux or '${USERPROFILE}' on windows."));

if (!Env.HasPath("C:\\Program Files\\Git\\usr\\bin"))
    Env.PrependPath("C:\\Program Files\\Git\\usr\\bin");

Env.Set(new Dictionary<string, string>{
    ["VALUE_ONE"] = "one",
    ["VALUE_TWO"] = "two"
});

Env.Vars["VALUE_THREE"] = "three"

Console.WriteLine(Env.Get("VALUE_ONE"));
Console.WriteLine(Env.Vars("VALUE_ONE"));

Console.WriteLine(Env.Vars.Home);
Console.WriteLine(Env.Vars.User);

Env.Unset("VALUE_TWO");

var result = Env.TryGet("VALUE_TWO");
var value = result.OrDefault("fallback value")l
Console.WriteLine(value);
```
