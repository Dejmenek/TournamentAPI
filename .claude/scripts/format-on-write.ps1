$toolCall = [Console]::In.ReadToEnd() | ConvertFrom-Json

$file = $toolCall.tool_input.file_path
if (-not $file) { $file = $toolCall.tool_input.path }
if (-not $file) { exit 0 }

if ([System.IO.Path]::GetExtension($file) -ne '.cs') { exit 0 }
if (-not (Test-Path $file)) { exit 0 }

$solution = Join-Path $env:CLAUDE_PROJECT_DIR 'TournamentAPI.sln'
dotnet format $solution --include $file | Out-Null
exit 0
