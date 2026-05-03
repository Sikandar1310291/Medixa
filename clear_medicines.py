import sqlite3
import os

db_path = r'c:\Users\ma516\OneDrive\Desktop\Pharma\PharmaDB.sqlite'

if not os.path.exists(db_path):
    print(f"Database not found at {db_path}")
    exit(1)

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    print("Beginning deep wipe of medicines and related transaction history...")

    # Delete all details first
    cursor.execute("DELETE FROM SaleReturnDetails")
    print("Cleared SaleReturnDetails")
    
    cursor.execute("DELETE FROM SaleReturns")
    print("Cleared SaleReturns")
    
    cursor.execute("DELETE FROM SaleDetails")
    print("Cleared SaleDetails")
    
    cursor.execute("DELETE FROM Sales")
    print("Cleared Sales")
    
    cursor.execute("DELETE FROM PurchaseReturnDetails")
    print("Cleared PurchaseReturnDetails")
    
    cursor.execute("DELETE FROM PurchaseReturns")
    print("Cleared PurchaseReturns")
    
    cursor.execute("DELETE FROM PurchaseDetails")
    print("Cleared PurchaseDetails")
    
    cursor.execute("DELETE FROM Purchases")
    print("Cleared Purchases")
    
    cursor.execute("DELETE FROM Stocks")
    print("Cleared Stocks")
    
    cursor.execute("DELETE FROM Medicines")
    print("Cleared Medicines")
    
    # Reset Ledger Balances and Transactions since we wiped sales/purchases
    cursor.execute("UPDATE Ledgers SET Balance = 0")
    print("Reset all Ledger Balances to 0")
    
    cursor.execute("DELETE FROM LedgerTransactions")
    print("Cleared LedgerTransactions")
    
    # Optionally delete Customers and Suppliers to be completely clean?
    # We will leave Customers and Suppliers intact but reset their balances.
    cursor.execute("UPDATE Customers SET Balance = 0")
    print("Reset Customer Balances to 0")
    
    conn.commit()
    print("Successfully deleted all medicines and completely reset the transaction database.")
except Exception as e:
    print(f"Error during deletion: {e}")
    conn.rollback()
finally:
    if 'conn' in locals():
        conn.close()
