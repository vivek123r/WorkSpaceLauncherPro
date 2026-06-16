# Web Dev mode fixture
$operaExe = "$env:USERPROFILE\AppData\Local\Programs\Opera GX\opera.exe"
Start-Process $operaExe
Start-Process "shell:AppsFolder\5319275A.WhatsAppDesktop_cv1g1gvanyjgm!App"
Start-Process explorer.exe "$env:USERPROFILE\Downloads"
Start-Sleep -Milliseconds 700
Move-Win $operaWins[0] 0 0 1280 720
Move-Win $waWins[0] 0 720 640 720
Move-Win $expWins[0] 640 720 640 720
