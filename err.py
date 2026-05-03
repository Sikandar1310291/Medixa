import subprocess, time

exe = r"C:\Users\ma516\OneDrive\Desktop\1 . Pharma\bin\Debug\PharmaBilling.exe"

proc = subprocess.Popen([exe], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
time.sleep(8)
proc.kill()
out, err = proc.communicate()
print("=== STDOUT ===")
print(out.decode('utf-8', errors='replace')[:4000])
print("=== STDERR ===")
print(err.decode('utf-8', errors='replace')[:4000])
