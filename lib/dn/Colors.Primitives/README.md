# NeoBeard.Colors.Primitives

Low-level, allocation-friendly color primitives and conversions.

## Supported color types

- `Rgb` / `Rgba` / `Argb`
- `Color`
- `Hsl` / `Hwb`
- `Lab` / `Lch`
- `Oklch`
- `DisplayP3` / `Rec2020` / `AdobeRgb`
- `Hex` / `Hexa`

## Key features

- Explicit conversion paths between color models via extension methods.
- Packed/integer-based formats and parse/try-parse helpers for hex formats.
- Deterministic equality semantics for byte-based values.
- Tolerant double comparison for floating-point color spaces.

## Example usage

```csharp
using NeoBeard.Colors;

var rgb = new Rgb(255, 64, 128);
var hsl = rgb.ToHsl();
var displayP3 = rgb.ToDisplayP3();
var roundTrip = displayP3.ToRgb();

var lab = rgb.ToLab();
var lch = lab.ToLch();
var fromLch = lch.ToLab().ToRgb();

var hex = new Hex(rgb.R, rgb.G, rgb.B);
var hexa = new Hexa(hex.R, hex.G, hex.B, Alpha.Opaque);
```

## Conversion test coverage

`ColorFormatConversionTests` exercises conversions across all public space conversions and verifies numeric round-trips with tolerance for floating-point spaces.
