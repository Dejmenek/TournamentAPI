$toolCall = [Console]::In.ReadToEnd() | ConvertFrom-Json

if ($toolCall.tool_input.command -notmatch 'git commit') { exit 0 }

$staged = git diff --cached --unified=0
$patterns = 'password\s*=', 'Server=.*;Password=', 'apikey', 'BEGIN (RSA )?PRIVATE KEY'

foreach ($pattern in $patterns) {
	if ($staged -match $pattern) {
		[Console]::Error.WriteLine("Blocked: staged changes match secret pattern '$pattern'. " + "Move the value to user secrets or environment variables, then commit.")
		exit 2
	}
}
exit 0