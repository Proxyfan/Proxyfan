#Requires -Version 7
# PreToolUse hook — GitHub Copilot coding agent
# Blocks git commit and git push so only the human developer can commit/push.
#
# Input: JSON on stdin with shape:
#   { "toolName": "bash", "toolArgs": "<JSON-encoded string>", ... }
# Output: JSON to stdout with shape:
#   { "permissionDecision": "deny"|"allow", "permissionDecisionReason": "..." }

$json    = [Console]::In.ReadToEnd() | ConvertFrom-Json
$command = ''

# toolArgs is a JSON-encoded string — decode it, then extract .command
if ($json.toolArgs) {
    $toolArgs = $json.toolArgs | ConvertFrom-Json
    $command  = $toolArgs.command
}

if ($command -match '(^|\s)git\s+(commit|push)(\s|$)') {
    [Console]::Out.WriteLine(
        ([pscustomobject]@{
            permissionDecision       = 'deny'
            permissionDecisionReason = 'git commit and git push are blocked by repository policy. The human developer handles all commits and pushes.'
        } | ConvertTo-Json -Compress)
    )
    exit 0
}

[Console]::Out.WriteLine('{"permissionDecision":"allow"}')
exit 0
