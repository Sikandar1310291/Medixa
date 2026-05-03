@echo off
cd /d "C:\Users\ma516\OneDrive\Desktop\1 . Pharma"
"bin\Debug\PharmaBilling.exe" > C:\medixa_crash.txt 2>&1
type C:\medixa_crash.txt
pause
