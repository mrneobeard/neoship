---
name: neo-code-style
description: C# code style, XML docs, .editorconfig, StyleCop, formatting, and repo conventions; use when editing C#, project files, or style-sensitive code in this repo.
---

# Neo Code Style

Use this skill for repo style and documentation rules.

## Required Sources

- Root `AGENTS.md`.
- `.editorconfig`.
- `Directory.Build.props`.
- `Directory.Packages.props` when package versions change.

## C# Rules

- Target framework comes from `$(Fx)` and is currently `net11.0`.
- `LangVersion` is `preview`.
- Nullable and implicit usings are enabled.
- Do not use primary constructors unless the repo changes that rule.
- Prefer `var` over local types unless required.
- Keep using directives outside namespaces.
- Keep braces style from `.editorconfig`.
- Avoid direct `==` on raw `double` or `float`; use tolerance.

## XML Docs

- Document all `public`, `internal`, and `protected` C# API symbols.
- Classes, structs, and records need XML docs and code examples.
- Methods need XML docs, all params, exact return type names, and code examples.
- Constructors and properties need summary plus typed `value` or parameter docs where applicable.
- Use exact return type names in signatures and docs, not inferred language.

## Package And Project Rules

- Use central package versions in `Directory.Packages.props`.
- Do not add package versions inline in `.csproj` unless the repo style changes.
- Keep provider-specific EF packages in provider projects.

## Formatting And Validation

- For .NET style checks, run `dotnet format neoship.slnx --verify-no-changes` when relevant.
- For all .NET changes, run targeted `dotnet test` or the relevant project build/test.
- For UI/apphost formatting, use existing package scripts or `oxfmt` config.
- Fix warnings, style errors, and test failures before finishing.
