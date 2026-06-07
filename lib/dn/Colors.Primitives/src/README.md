# NeoBeard.Colors.Primitives

`NeoBeard.Colors.Primitives` provides lightweight color primitives for .NET including
- RGB/ARGB channels
- Hex and hexa parsing/formatting
- alpha handling
- conversion helpers across common spaces (`HSL`, `HWB`, `Lab`, `LCH`, `OKLCH`, `DisplayP3`, `Rec.2020`, `AdobeRGB`)

## Usage

```csharp
using NeoBeard.Colors;

var rgb = new Rgb(255, 128, 64);
var hex = new Hex(rgb.R, rgb.G, rgb.B);
var hexa = new Hexa(rgb.R, rgb.G, rgb.B, Alpha.Opaque);
var roundTrip = new Rgba(hexa.R, hexa.G, hexa.B, (byte)hexa.A);

var lab = rgb.ToLab();
var fromLab = lab.ToRgb();

var displayP3 = rgb.ToDisplayP3();
var backToRgb = displayP3.ToRgb();

Console.WriteLine(hex.ToString());
Console.WriteLine(hexa.ToString());
Console.WriteLine(roundTrip.A);
```

## Parsing examples

```csharp
var hex = new Hex("#0F6");      // #00FF66
var hexa = new Hexa("#0F66F3CC");

Assert.True(Hexa.TryParse("F6A8CC77", out var parsed));
```

## Supported formats

- `Hex` (`#RRGGBB`, `RRGGBB`, `#RGB`, `RGB`)
- `Hexa` (`#RRGGBBAA`, `RRGGBBAA`, `#RGBA`, `RGBA`)
- `Rgb`, `Rgba`, `Argb`
- `Color`
- `Hsl`, `Hwb`, `Lab`, `Lch`, `Oklch`
- `DisplayP3`, `Rec2020`, `AdobeRgb`
