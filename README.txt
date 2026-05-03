================================================================
  MEDIXA - Pharmacy Billing & Inventory System
  Version 1.0 — Setup Instructions
================================================================

QUICK START (Single PC):
─────────────────────────
1. Extract this ZIP to any folder (e.g., C:\Medixa)
2. Open config.json — it already says mode="server" ✓
3. Double-click RunPharma.bat or Medixa.exe
4. Done! App will auto-start the database server.

Login: admin / admin123

────────────────────────────────────────────────────────────────
MULTI-PC LAN SETUP (e.g., 3 PCs in same pharmacy):
────────────────────────────────────────────────────────────────

SERVER PC (the main computer with all data):
  1. Extract the ZIP to C:\Medixa on this PC
  2. config.json:
       "mode": "server",
       "serverIP": "localhost",
       "dbPath": "PharmaDB.sqlite"
  3. Run Medixa.exe — PharmaServer runs automatically
  4. Find this PC's IP: open CMD → type 'ipconfig'
     Note the IPv4 Address (e.g., 192.168.1.10)
  5. Allow port 5000 in Windows Firewall (see below)

CLIENT PCs (other computers in the same office/shop):
  1. Extract the ZIP to C:\Medixa on each client PC
  2. Edit config.json:
       "mode": "client",
       "serverIP": "192.168.1.10"   ← Server PC's IP address
       "serverPort": 5000
  3. Run Medixa.exe — it will connect to the server

Note: Client PCs do NOT need PharmaDB.sqlite or PharmaServer.exe

────────────────────────────────────────────────────────────────
WINDOWS FIREWALL — Allow Port 5000 on SERVER PC:
────────────────────────────────────────────────────────────────
Run in CMD as Administrator on the SERVER PC:
  netsh advfirewall firewall add rule name="Medixa Server" dir=in action=allow protocol=TCP localport=5000

Or manually:
  Windows Defender Firewall → Advanced Settings
  → Inbound Rules → New Rule → Port → TCP 5000 → Allow

────────────────────────────────────────────────────────────────
FILE STRUCTURE:
────────────────────────────────────────────────────────────────
Medixa\
├── Medixa.exe          ← WPF Application (launch this)
├── PharmaServer.exe    ← REST API Server (auto-launched)
├── PharmaDB.sqlite     ← Database (server PC only)
├── config.json         ← Mode + network settings
├── System.Data.SQLite.dll
├── x64\e_sqlite3.dll
├── x86\e_sqlite3.dll
└── README.txt          ← This file

================================================================
  FEATURES:
================================================================
✓ Sales POS with Invoice printing
✓ Purchase management with stock tracking
✓ FIFO batch stock deduction
✓ Medicine inventory with expiry alerts
✓ Customer & Supplier management
✓ Accounting & Ledger system
✓ Reports (Sales, Stock, Low Stock, Expired)
✓ Multi-PC LAN support
✓ Offline — no internet required

================================================================
  SUPPORT:
================================================================
Default Login: admin / admin123
Database backup: copy PharmaDB.sqlite file

================================================================
