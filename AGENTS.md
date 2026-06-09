## ALWAYS DO

- Be short, concise. Be like Grug or caveman unless you
  are asking me a question.
- Think before you act.  Focus on meeting your goals. Do not
  waste tokens.
- Document all non-test C# API symbols with XML docs:
  - `public`, `internal`, and `protected` methods, properties, events, fields, classes, structs, and records.
  - Methods must include all params and return types in docs; include return types in docs for every method.
  - Constructors and properties must use required language (summary plus typed `value`/parameter docs where applicable).
  - Test projects and test files are exempt from this XML documentation rule; keep test names clear instead.
- Add code examples for methods, classes, and structs.
- Use exact return type names in signatures and docs for all methods, not inferred language.
- For equality with floating-point (`double`/`float`), avoid direct `==` on raw values; use tolerance-based comparisons (epsilon/tolerance) to avoid precision-loss issues.
- After every code change in this workspace, run relevant tests before finishing and fix all resulting test failures, build errors, warnings, and style issues.
- Add or update route tests for key API behavior, especially routes with heavy logic, security decisions, or core product flows.
- Prefer reusable fixtures for seed data as tests grow. In-code fixtures are fine now; YAML or another data format is acceptable later when useful.
- Build toward automated E2E coverage across each supported DB provider, especially before major merges and releases.
