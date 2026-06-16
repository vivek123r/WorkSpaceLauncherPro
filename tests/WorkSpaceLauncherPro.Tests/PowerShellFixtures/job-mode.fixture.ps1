# Job Mode fixture (copy of C:\Users\vivek\job-mode.ps1 — used for tests)
# This is a sanitized version for the test suite.

$operaExe = "$env:USERPROFILE\AppData\Local\Programs\Opera GX\opera.exe"

Write-Host "Starting Job Mode..."

Write-Host "  Launching Opera GX..."
Start-Process $operaExe
Start-Sleep -Seconds 2

Write-Host "  Launching Claude PWA windows..."
Start-Process "shell:AppsFolder\Chrome._crx_fmpnlaolkdacoja.UserData.Profile6"
Start-Sleep -Milliseconds 700

Write-Host "  Launching WhatsApp..."
Start-Process "shell:AppsFolder\5319275A.WhatsAppDesktop_cv1g1gvanyjgm!App"
Start-Sleep -Milliseconds 400

Write-Host "  Launching File Explorer (Downloads)..."
Start-Process explorer.exe "$env:USERPROFILE\Downloads"

Start-Sleep -Seconds 7

Write-Host "  Positioning windows..."

# (Mock Move-Win calls — actual implementation is in the user's real script)
Move-Win $operaWins[0] 0 0 1280 720
Move-Win $waWins[0] 0 720 640 720
Move-Win $expWins[0] 640 720 640 720
Move-Win $claudeWins[0] 1280 0 640 720
