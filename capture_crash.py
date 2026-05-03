import subprocess, sys

exe = r"C:\Users\ma516\OneDrive\Desktop\1 . Pharma\bin\Debug\PharmaBilling.exe"
result = subprocess.run([exe], capture_output=True, text=True, timeout=15)
print("STDOUT:", result.stdout[:3000] if result.stdout else "(empty)")
print("STDERR:", result.stderr[:3000] if result.stderr else "(empty)")
print("Return code:", result.returncode)
