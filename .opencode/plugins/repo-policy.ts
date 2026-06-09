const dangerousCommandPatterns: RegExp[] = [
  /\brm\s+[^\n;]*-rf\s+(?:\/|~|\$HOME|\.|\.\.|\*)(?:\s|$)/,
  /\bgit\s+reset\s+--hard\b/,
  /\bgit\s+clean\s+-[^\n;]*[xfd][^\n;]*/,
  /\bgit\s+checkout\s+--\s+/,
  /\bchmod\s+(?:-R\s+)?777\b/,
  /\bmkfs(?:\.|\s)/,
  /\bdd\s+if=/,
  /\b(?:shred|wipefs)\b/,
]

const verificationCommandPattern =
  /\b(?:dotnet\s+(?:test|build|format)|pnpm\b[^\n;]*(?:\srun\s+)?(?:test|build|check|lint|format|audit)|npm\s+run\s+(?:test|build|check|lint)|vp\s+(?:test|check|build)|playwright\s+test|oxfmt\b|git\s+diff\s+--cached\s+--check)\b/

const codePathPattern = /\.(?:cs|csproj|props|targets|slnx|ts|tsx|mts|js|mjs|svelte|css|json|jsonc|toml|ya?ml)$/i

let codeEditSeen = false
let verificationSeen = false

function isSecretEnvPath(value: unknown): boolean {
  if (typeof value !== "string") return false

  return /(?:^|[/\\])\.env(?:$|[.][A-Za-z0-9_-]+$)/.test(value) && !/(?:^|[/\\])\.env\.example$/.test(value)
}

function commandTouchesSecretEnv(command: string): boolean {
  const matches = command.match(/(?:^|[\s'\"])(?:\.?\.?[/\\])?(?:[\w.-]+[/\\])*\.env(?:\.[\w-]+)?(?=$|[\s'\"])/g) ?? []
  return matches.some((match) => !match.includes(".env.example"))
}

function commandIsDangerous(command: string): string | undefined {
  const match = dangerousCommandPatterns.find((pattern) => pattern.test(command))
  return match?.source
}

function commandLooksLikeCommit(command: string): boolean {
  return /\bgit\s+commit\b/.test(command)
}

function markEditFromPath(value: unknown): void {
  if (typeof value === "string" && codePathPattern.test(value)) codeEditSeen = true
}

function markEditFromPatch(command: unknown): void {
  if (typeof command !== "string") return

  for (const line of command.split("\n")) {
    const match = /^(?:(?:\+\+\+|---) [ab]\/(.+)|\*\*\* (?:Add|Update|Delete) File: (.+))$/.exec(line)
    const filePath = match?.[1] ?? match?.[2] ?? ""
    if (codePathPattern.test(filePath)) codeEditSeen = true
  }
}

export const RepoPolicyPlugin = async ({ $ }: { $: typeof Bun.$ }) => {
  return {
    "tool.execute.before": async (input: { tool?: string }, output: { args?: Record<string, unknown> }) => {
      const tool = input.tool ?? ""
      const args = output.args ?? {}

      if ((tool === "read" || tool === "edit" || tool === "write") && isSecretEnvPath(args.filePath ?? args.path)) {
        throw new Error("Repository policy blocks reading or editing secret .env files. Use .env.example or ask the user.")
      }

      if (tool === "edit" || tool === "write") {
        markEditFromPath(args.filePath ?? args.path)
      }

      if (tool === "bash") {
        const command = String(args.command ?? "")
        const dangerous = commandIsDangerous(command)
        if (dangerous) throw new Error(`Repository policy blocks dangerous shell command pattern: ${dangerous}`)
        if (commandTouchesSecretEnv(command)) {
          throw new Error("Repository policy blocks shell access to secret .env files. Use .env.example or ask the user.")
        }

        if (commandLooksLikeCommit(command)) {
          try {
            await $`git diff --cached --check`.quiet()
          } catch {
            throw new Error("Repository policy blocks git commit while staged whitespace errors exist. Run git diff --cached --check.")
          }

          if (codeEditSeen && !verificationSeen) {
            throw new Error("Repository policy blocks agent git commit after code/config edits without verification. Run relevant build, test, format, or check commands first.")
          }
        }
      }
    },

    "tool.execute.after": async (input: { tool?: string }, output: { args?: Record<string, unknown> }) => {
      const tool = input.tool ?? ""
      const args = output.args ?? {}

      if (tool === "bash") {
        const command = String(args.command ?? "")
        if (verificationCommandPattern.test(command)) verificationSeen = true
      }

      if (tool === "edit" || tool === "write") {
        markEditFromPath(args.filePath ?? args.path)
      }

      markEditFromPatch(args.command)
    },
  }
}

export default RepoPolicyPlugin
