# NeoBeard.Ansi

## Overview

The libraries provides basic ansi terminal formatting, color detection, and setting
the windows console mode to enable 24bit colors.

## Usage

```csharp
using static NeoBeard.Ansi;

Console.WriteLine($"task was ${Bold(Green("completed"))}");
Console.WriteLine($"{Rgb24(0xff7b00, "Orange")} you glad you met me?");
Console.WriteLine($"What rhymes with {Rgb8(202, "orange")}?");
```
