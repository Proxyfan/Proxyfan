#Requires -Version 7
# PreToolUse hook — Claude Code
# Blocks git commit and git push so only the human developer can commit/push.

$json    = [Console]::In.ReadToEnd() | ConvertFrom-Json
$command = $json.tool_input.command

if ($command -match '(^|\s)git\s+(commit|push)(\s|$)') {
    [Console]::Out.WriteLine(
        ([pscustomobject]@{
            hookSpecificOutput = [pscustomobject]@{
                hookEventName            = 'PreToolUse'
                permissionDecision       = 'deny'
                permissionDecisionReason = 'git commit and git push are reserved for the human developer. Prepare your changes and stop — do not commit or push under any circumstances.'
            }
        } | ConvertTo-Json -Compress -Depth 5)
    )
    exit 0
}

exit 0
