Stop-Process -Name "check_win_perfmon" -Force
c:\windows\system32\lodctr.exe /R
c:\windows\sysWOW64\lodctr.exe /R
WINMGMT.EXE /RESYNCPERF
Get-Service -Name "pla" | Restart-Service -Verbose
Get-Service -Name "winmgmt" | Restart-Service -Force -Verbose
