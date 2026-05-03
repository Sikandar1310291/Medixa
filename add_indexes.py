import sqlite3
import os

db_paths = [
    r'C:\ProgramData\Medixa\PharmaDB.sqlite',
    r'C:\Users\ma516\OneDrive\Desktop\Pharma\bin\Debug\PharmaDB.sqlite'
]

INDEXES = [
    # Medicines - name search is the most frequent operation
    "CREATE INDEX IF NOT EXISTS idx_medicines_name ON Medicines(Name)",
    "CREATE INDEX IF NOT EXISTS idx_medicines_status ON Medicines(Status)",

    # Stocks - joining by MedicineID happens on every page load
    "CREATE INDEX IF NOT EXISTS idx_stocks_medicineid ON Stocks(MedicineID)",
    "CREATE INDEX IF NOT EXISTS idx_stocks_batchno ON Stocks(BatchNo)",

    # Sales - date filters + customerID joins are extremely common
    "CREATE INDEX IF NOT EXISTS idx_sales_saledate ON Sales(SaleDate)",
    "CREATE INDEX IF NOT EXISTS idx_sales_customerid ON Sales(CustomerID)",
    "CREATE INDEX IF NOT EXISTS idx_sales_status ON Sales(Status)",

    # SaleDetails - joined on every sale list and analysis query
    "CREATE INDEX IF NOT EXISTS idx_saledetails_saleid ON SaleDetails(SaleID)",
    "CREATE INDEX IF NOT EXISTS idx_saledetails_medicineid ON SaleDetails(MedicineID)",

    # Purchases - date filters + supplierID joins
    "CREATE INDEX IF NOT EXISTS idx_purchases_purchasedate ON Purchases(PurchaseDate)",
    "CREATE INDEX IF NOT EXISTS idx_purchases_supplierid ON Purchases(SupplierID)",

    # PurchaseDetails - joined on every purchase list
    "CREATE INDEX IF NOT EXISTS idx_purchasedetails_purchaseid ON PurchaseDetails(PurchaseID)",
    "CREATE INDEX IF NOT EXISTS idx_purchasedetails_medicineid ON PurchaseDetails(MedicineID)",

    # Ledger Transactions
    "CREATE INDEX IF NOT EXISTS idx_ledgertx_ledgerid ON LedgerTransactions(LedgerID)",

    # Customers
    "CREATE INDEX IF NOT EXISTS idx_customers_name ON Customers(Name)",

    # Suppliers  
    "CREATE INDEX IF NOT EXISTS idx_suppliers_name ON Suppliers(Name)",
]

for db_path in db_paths:
    if not os.path.exists(db_path):
        print(f"Skipping (not found): {db_path}")
        continue
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        # Enable WAL mode for this DB file immediately
        cursor.execute("PRAGMA journal_mode=WAL;")
        cursor.execute("PRAGMA cache_size=10000;")
        cursor.execute("PRAGMA synchronous=NORMAL;")
        cursor.execute("PRAGMA page_size=4096;")

        for idx_sql in INDEXES:
            try:
                cursor.execute(idx_sql)
                print(f"  OK: {idx_sql[:60]}...")
            except Exception as e:
                print(f"  SKIP: {e}")

        # Run ANALYZE so SQLite uses our new indexes
        cursor.execute("ANALYZE;")
        conn.commit()
        conn.close()
        print(f"\nIndexes created & WAL enabled: {db_path}\n")
    except Exception as e:
        print(f"Error on {db_path}: {e}")

print("Done! All databases optimized.")
