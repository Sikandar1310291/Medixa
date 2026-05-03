import sqlite3
import csv
import os

csv_file = r'c:\Users\ma516\OneDrive\Desktop\Pharma\1.media\medical_store_item_list.xlsx - SQL Results (2).csv'
databases = [
    r'c:\Users\ma516\OneDrive\Desktop\Pharma\PharmaDB.sqlite',
    r'C:\ProgramData\Medixa\PharmaDB.sqlite'
]

if not os.path.exists(csv_file):
    print(f"CSV file not found: {csv_file}")
    exit(1)

medicines_to_insert = []

with open(csv_file, 'r', encoding='utf-8') as f:
    reader = csv.reader(f)
    next(reader) # skip header
    for row in reader:
        if len(row) >= 2:
            name = row[1].strip()
            if name:
                medicines_to_insert.append((name, 'Generic', 'Uncategorized', 'Unknown', 'Box', '', '', 0.0, 0.0, 0.0, 10, 'Active'))

for db_path in databases:
    if os.path.exists(db_path):
        print(f"Seeding database: {db_path}")
        try:
            conn = sqlite3.connect(db_path)
            cursor = conn.cursor()
            
            # Use executemany for fast inserts
            cursor.executemany('''
                INSERT INTO Medicines (Name, Type, Category, Manufacturer, Unit, Barcode, GenericFormula, WholesalePrice, PurchasePrice, SalePrice, MinStock, Status)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', medicines_to_insert)
            
            conn.commit()
            print(f"Successfully inserted {cursor.rowcount} medicines into {db_path}")
            conn.close()
        except Exception as e:
            print(f"Failed to seed {db_path}: {e}")
    else:
        print(f"Database not found: {db_path}")
