import sqlite3
import csv
import os

csv_path = r'C:\Users\ma516\OneDrive\Desktop\1 . Pharma\1.media\medical_store_item_list.xlsx - SQL Results (2).csv'

databases = {
    r'C:\Users\ma516\OneDrive\Desktop\1 . Pharma\PharmaDB.sqlite': 'dev',
    r'C:\ProgramData\Medixa\PharmaDB.sqlite': 'installed'
}

# Step 1: Read all medicine names from CSV
medicine_names = []
with open(csv_path, 'r', encoding='utf-8') as f:
    reader = csv.reader(f)
    next(reader)  # skip header row
    for row in reader:
        if len(row) >= 2:
            name = row[1].strip()
            if name:
                medicine_names.append(name)

# Step 2: Sort STRICTLY A to Z (case-insensitive)
medicine_names.sort(key=lambda x: x.upper())

print(f"Loaded {len(medicine_names)} medicines from CSV.")
print(f"First 5: {medicine_names[:5]}")
print(f"Last  5: {medicine_names[-5:]}")

# Step 3: Get columns for each DB and seed
for db_path, label in databases.items():
    if not os.path.exists(db_path):
        print(f"[SKIP] {label} DB not found: {db_path}")
        continue

    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # Get actual columns in this DB's Medicines table
    cursor.execute("PRAGMA table_info(Medicines)")
    cols = [row[1] for row in cursor.fetchall()]
    print(f"\n[{label}] Medicines columns: {cols}")

    # Wipe medicine-related tables first
    for table in ['SaleReturnDetails', 'SaleReturns', 'SaleDetails', 'Sales',
                  'PurchaseReturnDetails', 'PurchaseReturns', 'PurchaseDetails',
                  'Purchases', 'Stocks', 'Medicines']:
        try:
            cursor.execute(f"DELETE FROM {table}")
            cursor.execute(f"DELETE FROM sqlite_sequence WHERE name='{table}'")
        except Exception:
            pass

    # Reset ledgers/balances
    try: cursor.execute("UPDATE Ledgers SET Balance = 0")
    except: pass
    try: cursor.execute("UPDATE Customers SET Balance = 0")
    except: pass
    try: cursor.execute("DELETE FROM LedgerTransactions")
    except: pass

    conn.commit()
    print(f"[{label}] Wiped all existing medicines and transactions.")

    # Build insert rows matching this DB's exact schema
    if 'Barcode' in cols and 'GenericFormula' in cols and 'WholesalePrice' in cols:
        # Full schema (installed)
        rows = [(name, 'Generic', 'Uncategorized', 'Unknown', 'Box', '', '', 0.0, 0.0, 0.0, 10, 'Active')
                for name in medicine_names]
        cursor.executemany(
            "INSERT INTO Medicines (Name, Type, Category, Manufacturer, Unit, Barcode, GenericFormula, WholesalePrice, PurchasePrice, SalePrice, MinStock, Status) VALUES (?,?,?,?,?,?,?,?,?,?,?,?)",
            rows
        )
    else:
        # Compact schema (dev)
        rows = [(name, 'Generic', 'Uncategorized', 'Unknown', 'Box', 0.0, 0.0, 10, 'Active')
                for name in medicine_names]
        cursor.executemany(
            "INSERT INTO Medicines (Name, Type, Category, Manufacturer, Unit, PurchasePrice, SalePrice, MinStock, Status) VALUES (?,?,?,?,?,?,?,?,?)",
            rows
        )

    conn.commit()

    # Verify
    count = cursor.execute("SELECT COUNT(*) FROM Medicines").fetchone()[0]
    first = cursor.execute("SELECT Name FROM Medicines ORDER BY Name LIMIT 1").fetchone()[0]
    last  = cursor.execute("SELECT Name FROM Medicines ORDER BY Name DESC LIMIT 1").fetchone()[0]
    print(f"[{label}] SUCCESS: {count} medicines inserted.")
    print(f"[{label}] First in DB: {first}")
    print(f"[{label}] Last  in DB: {last}")
    conn.close()

print("\nDone! Both databases seeded in strict A-Z ascending order.")
