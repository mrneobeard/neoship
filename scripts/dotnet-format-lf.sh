#!/usr/bin/env bash
set -euo pipefail

normalize_lf() {
  while IFS= read -r -d '' path; do
    case "$path" in
      *.cs|*.csproj|*.props|*.targets|*.slnx|*.ts|*.tsx|*.js|*.mjs|*.svelte|*.css|*.html|*.md|*.json|*.jsonc|*.toml|*.yaml|*.yml|*.xml|.editorconfig|.gitattributes)
        perl -pi -e 's/\r$//' "$path"
        ;;
    esac
  done
}

dotnet format "$@"

{
  git diff --name-only -z --diff-filter=ACMRTUXB
  git diff --cached --name-only -z --diff-filter=ACMRTUXB
  git ls-files -z --others --exclude-standard
} | normalize_lf
