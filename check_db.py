import sqlite3

conn = sqlite3.connect('bin/Debug/PharmaDB.sqlite')
cursor = conn.cursor()
cursor.execute("SELECT MedicineID, Name FROM Medicines ORDER BY MedicineID ASC LIMIT 5;")
for row in cursor.fetchall():
    print(f"{row[0]} - {row[1]}")
conn.close()
