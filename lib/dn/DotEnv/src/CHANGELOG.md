# NeoBeard.DotEnv Changelog

## 0.0.0-alpha.1

Initial release.

### Features

- Parse dotenv content from strings, files, and streams
- Multi-file parsing with key override (later files win)
- Optional files via `?` suffix (e.g., `.env.local?`)
- Multiple quote styles: double, single, backtick
- Escape sequence support in double-quoted values
- Multi-line values in quoted strings
- Inline and standalone comments
- Document model preserving order and structure
- Round-trip editing (parse, modify, serialize)
- Expand method for variable expansion via callback
- Async file reading support
- TryParse methods returning results instead of throwing
- Character-by-character parsing (no regular expressions)