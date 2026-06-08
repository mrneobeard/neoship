# NeoBeard.Secrets.OperatingSystem

## Overview

Provides the ability to work with operating system credential stores
that store secrets/passwords similar to keytar.

## Usage

```csharp
using Hyperx.Secrets;

OsSecrets.SetSecret("my_service", "my_account", "some secret password");

Console.WriteLine(OsSecrets.GetScret("my_service", "my_account"));
```
