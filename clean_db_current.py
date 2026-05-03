import sqlite3
import os

db_path = r'c:\Users\ma516\OneDrive\Desktop\Pharma\PharmaDB.sqlite'

if not os.path.exists(db_path):
    print(f"Database not found at {db_path}")
    exit(1)

conn = sqlite3.connect(db_path)
cursor = conn.cursor()

print("Beginning deep wipe of medicines and related transaction history...")

tables_to_clear = [
    "SaleReturnDetails", "SaleReturns", "SaleDetails", "Sales",
    "PurchaseReturnDetails", "PurchaseReturns", "PurchaseDetails", "Purchases",
    "Stocks", "Medicines", "LedgerTransactions"
]

for table in tables_to_clear:
    try:
        cursor.execute(f"DELETE FROM {table}")
        print(f"Cleared {table}")
    except sqlite3.OperationalError as e:
        print(f"Skipping {table}: {e}")

try:
    cursor.execute("UPDATE Ledgers SET Balance = 0")
    print("Reset all Ledger Balances to 0")
except Exception as e:
    print(f"Skipping Ledgers: {e}")

try:
    cursor.execute("UPDATE Customers SET Balance = 0")
    print("Reset Customer Balances to 0")
except Exception as e:
    print(f"Skipping Customers: {e}")
    
try:
    cursor.execute("UPDATE Suppliers SET Balance = 0")
    print("Reset Supplier Balances to 0")
except Exception as e:
    print(f"Skipping Suppliers: {e}")

# also delete the sqlite sequence so auto-increment resets
try:
    for table in tables_to_clear:
        cursor.execute(f"DELETE FROM sqlite_sequence WHERE name='{table}'")
    print("Reset sqlite_sequence counters")
except sqlite3.OperationalError as e:
    print(f"Skipping sqlite_sequence: {e}")

conn.commit()
print("Successfully deleted all medicines and completely reset the transaction database.")
conn.close()
