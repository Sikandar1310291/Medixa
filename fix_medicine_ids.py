import sqlite3
import os

db_paths = [
    r'C:\ProgramData\Medixa\PharmaDB.sqlite',
    r'C:\Users\ma516\OneDrive\Desktop\Pharma\bin\Debug\PharmaDB.sqlite'
]

for db_path in db_paths:
    if not os.path.exists(db_path):
        continue
        
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Get all current medicines ordered by ID
        cursor.execute("SELECT MedicineID FROM Medicines ORDER BY MedicineID ASC")
        rows = cursor.fetchall()
        
        # We need to update MedicineID to 1, 2, 3...
        # Disable foreign keys temporarily if needed, though SQLite allows updating if not restricted
        cursor.execute("PRAGMA foreign_keys = OFF;")
        
        new_id = 1
        for row in rows:
            old_id = row[0]
            if old_id != new_id:
                # Update Medicines
                cursor.execute("UPDATE Medicines SET MedicineID = ? WHERE MedicineID = ?", (new_id, old_id))
                # Update Stocks
                cursor.execute("UPDATE Stocks SET MedicineID = ? WHERE MedicineID = ?", (new_id, old_id))
                # Update SaleDetails, PurchaseDetails etc just in case
                cursor.execute("UPDATE SaleDetails SET MedicineID = ? WHERE MedicineID = ?", (new_id, old_id))
                cursor.execute("UPDATE PurchaseDetails SET MedicineID = ? WHERE MedicineID = ?", (new_id, old_id))
                cursor.execute("UPDATE PurchaseReturnDetails SET MedicineID = ? WHERE MedicineID = ?", (new_id, old_id))
                cursor.execute("UPDATE SaleReturnDetails SET MedicineID = ? WHERE MedicineID = ?", (new_id, old_id))
            new_id += 1
            
        # Reset the auto-increment sequence
        cursor.execute("UPDATE sqlite_sequence SET seq = ? WHERE name = 'Medicines'", (new_id - 1,))
        # If the sequence didn't exist, insert it
        if cursor.rowcount == 0:
            cursor.execute("INSERT INTO sqlite_sequence (name, seq) VALUES ('Medicines', ?)", (new_id - 1,))
            
        conn.commit()
        cursor.execute("PRAGMA foreign_keys = ON;")
        conn.close()
        print(f"Successfully renumbered medicines in {db_path}")
    except Exception as e:
        print(f"Error updating {db_path}: {e}")
