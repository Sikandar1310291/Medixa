import sqlite3
import os

db_path = r"bin\Debug\PharmaDB.sqlite"
schema_sql = r"Source\Data\schema.sql"

def column_exists(cursor, table, column):
    cursor.execute(f"PRAGMA table_info({table})")
    columns = [row[1] for row in cursor.fetchall()]
    return column in columns

def table_exists(cursor, table):
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name=?", (table,))
    return cursor.fetchone() is not None

def migrate():
    if not os.path.exists(db_path):
        print("Database not found!")
        return

    conn = sqlite3.connect(db_path)
    c = conn.cursor()

    try:
        # Alter Medicines
        if not column_exists(c, 'Medicines', 'Barcode'):
            c.execute("ALTER TABLE Medicines ADD COLUMN Barcode TEXT;")
            print("Added Barcode to Medicines")
            
        if not column_exists(c, 'Medicines', 'GenericFormula'):
            c.execute("ALTER TABLE Medicines ADD COLUMN GenericFormula TEXT;")
            print("Added GenericFormula to Medicines")
            
        if not column_exists(c, 'Medicines', 'WholesalePrice'):
            c.execute("ALTER TABLE Medicines ADD COLUMN WholesalePrice REAL DEFAULT 0;")
            print("Added WholesalePrice to Medicines")

        # Alter Sales
        if not column_exists(c, 'Sales', 'FBRInvoiceNo'):
            c.execute("ALTER TABLE Sales ADD COLUMN FBRInvoiceNo TEXT;")
            print("Added FBRInvoiceNo to Sales")

        # Alter PurchaseDetails
        if not column_exists(c, 'PurchaseDetails', 'BonusQuantity'):
            c.execute("ALTER TABLE PurchaseDetails ADD COLUMN BonusQuantity INTEGER DEFAULT 0;")
            print("Added BonusQuantity to PurchaseDetails")

        # Create New Tables
        c.execute("""
        CREATE TABLE IF NOT EXISTS Staff (
            StaffID INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Designation TEXT,
            Contact TEXT,
            BaseSalary REAL DEFAULT 0,
            LedgerID INTEGER
        )
        """)
        
        c.execute("""
        CREATE TABLE IF NOT EXISTS Attendance (
            AttendanceID INTEGER PRIMARY KEY AUTOINCREMENT,
            StaffID INTEGER,
            Date TEXT,
            Status TEXT
        )
        """)
        
        c.execute("""
        CREATE TABLE IF NOT EXISTS Payroll (
            PayrollID INTEGER PRIMARY KEY AUTOINCREMENT,
            StaffID INTEGER,
            Month TEXT,
            AmountPaid REAL,
            Bonuses REAL,
            Deductions REAL,
            DatePaid TEXT
        )
        """)
        print("Created Staff, Attendance, and Payroll tables")
        
        conn.commit()
        print("Migration successful! Ready for Abuzar Parity.")
        
    except sqlite3.Error as e:
        print(f"SQLite error: {e}")
        conn.rollback()
    finally:
        conn.close()

if __name__ == "__main__":
    migrate()
