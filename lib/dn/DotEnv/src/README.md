# NeoBeard.DotEnv

A .NET library for parsing and manipulating dotenv (`.env`) files.

## Features

- Preserves order, comments, and formatting for round-trip editing
- Multi-file parsing with key override (later files win)
- Optional files via `?` suffix (e.g., `.env.local?`)
- Multiple quote styles: `"double"`, `'single'`, `` `backtick` ``
- Escape sequences: `\n`, `\t`, `\r`, `\b`, `\\`, `\uXXXX`, `\UXXXXXXXX`
- Multi-line values in quoted strings
- Inline and standalone comments
- Async file reading support
- No regular expressions (character-by-character parsing)

## Usage

### Basic Parsing

```csharp
using NeoBeard.DotEnv;

// Parse from string
var doc = DotEnvFile.Parse("KEY=value\nKEY2=\"quoted value\"");
var value = doc.Get("KEY");

// Parse from file
var doc = DotEnvFile.ParseFile(".env");
var dbUrl = doc.Get("DATABASE_URL");
```

### Multiple Files (with overrides)

```csharp
// .env is required, .env.local? is optional (skipped if missing)
// Keys from later files override earlier ones
var doc = DotEnvFile.ParseFiles(".env", ".env.local?", ".env.production?");
var dbUrl = doc.Get("DATABASE_URL");
```

### TryParse (non-throwing)

```csharp
var result = DotEnvFile.TryParseFiles(".env", ".env.missing");
if (result.IsOk)
{
    var doc = result.Doc;
    var value = doc.Get("KEY");
}
else
{
    Console.WriteLine($"Error: {result.Error?.Message}");
}
```

### Variable Expansion

Variable expansion and command substitution are handled by `Env.Expand` from 
`NeoBeard.Core`. Use the `Expand` method on the document:

```csharp
using NeoBeard;
using NeoBeard.DotEnv;

var doc = DotEnvFile.ParseFiles(".env", ".env.local?");
doc.Expand(value => Env.Expand(value, new EnvExpandOptions 
{ 
    CommandSubstitution = true,
    GetVariable = k => doc.Get(k) ?? Environment.GetEnvironmentVariable(k)
}));

// Or simply:
doc.Expand(value => Env.Expand(value));
```

### Document Manipulation

```csharp
var doc = new EnvDoc();
doc.Set("API_KEY", "secret123", QuoteStyle.Double);
doc.Set("DATABASE_URL", "postgres://localhost/db");
doc.AddComment("Configuration settings");
doc.AddNewline();

// Serialize back to .env format
var envContent = doc.ToString();
```

### Merging Dictionaries

```csharp
var doc = new EnvDoc();
doc.Set("KEY1", "value1");

// Merge from another EnvDoc
var other = DotEnvFile.Parse("KEY2=value2\nKEY1=override");
doc.Merge(other);

// Merge from IDictionary<string, string>
var dict = new Dictionary<string, string> { ["KEY3"] = "value3" };
doc.Merge(dict);
```

### Converting to Dictionaries

```csharp
var doc = DotEnvFile.Parse("KEY1=value1\nKEY2=value2");

// To unordered dictionary (hash-based)
var dict = doc.ToDictionary();

// To ordered dictionary (preserves order)
var ordered = doc.ToOrderedDictionary();

// To dictionary with typed values
var typed = doc.ToDictionary<string, int>(k => k, v => int.Parse(v));
```

### Round-trip Editing

```csharp
// Parse, modify, and save back while preserving comments and order
var doc = DotEnvFile.ParseFile(".env");
doc.Set("API_KEY", "new-value");
File.WriteAllText(".env", doc.ToString());
```

## Quote Styles

| Style | Escape Sequences | Example |
|-------|-----------------|---------|
| None | No | `KEY=value` |
| Single | No | `KEY='value with \n'` (literal `\n`) |
| Double | Yes | `KEY="value with \n"` (actual newline) |
| Backtick | Yes | `KEY=\`value with \n\`` |

## Syntax

```env
# This is a comment
KEY=value                           # Unquoted value
KEY="double quoted"                 # Double quotes (escapes processed)
KEY='single quoted'                 # Single quotes (no escapes)
KEY=`backtick quoted`               # Backticks (escapes processed)
KEY="multi
line"                               # Multi-line values
KEY="escaped \n \t \\ \" \u0041"    # Escape sequences
KEY=value # inline comment          # Comment after value
```

## Optional Files

Paths ending with `?` are optional:

```csharp
// .env is required (throws if missing)
// .env.local? is optional (skipped if missing)
// .env.production? is optional
var doc = DotEnvFile.ParseFiles(".env", ".env.local?", ".env.production?");
```

## Reference

### DotEnvFile Methods

| Method | Description |
|--------|-------------|
| `Parse(string)` | Parse from string content |
| `ParseFile(path)` | Parse a single file |
| `ParseFiles(paths...)` | Parse multiple files with optional `?` suffix |
| `TryParseFile(path)` | Non-throwing parse |
| `TryParseFiles(paths...)` | Non-throwing multi-file parse |
| `ParseFileAsync(path)` | Async file parse |
| `ParseStream(stream)` | Parse from stream |

### EnvDoc Methods

| Method | Description |
|--------|-------------|
| `Get(key)` | Get a variable value |
| `Set(key, value, quote)` | Set or add a variable |
| `Remove(key)` | Remove a variable |
| `Expand(expander)` | Expand all values in-place |
| `Merge(EnvDoc)` | Merge another document |
| `Merge(IDictionary)` | Merge from a dictionary |
| `ToDictionary()` | Convert to dictionary |
| `ToDictionary<TKey, TValue>()` | Convert to typed dictionary |
| `ToOrderedDictionary()` | Convert to ordered dictionary |
| `ToString()` | Serialize to .env format |