# 🏥 Medixa Pharmacy — Production 3-Layer Backup System

> **Senior Google SWE Implementation** — Healthcare-grade, automated, encrypted backup.

---

## ⚡ Quick Start (5 minutes)

```
1. Open Terminal in: C:\Users\ma516\OneDrive\Desktop\Pharma\backup_system\
2. Run:  pip install -r requirements.txt
3. Run:  python medixa_backup.py --layer 1   (test Layer 1)
4. Done! Check logs\ folder for confirmation.
```

---

## 📁 Project Structure

```
backup_system/
│
├── medixa_backup.py          ← MAIN ENTRY POINT (run this)
├── backup_config.json        ← ALL SETTINGS (edit this)
├── requirements.txt          ← Python dependencies
├── setup_scheduler.bat       ← Windows Auto-Task Setup (run as Admin)
│
├── core/
│   ├── logger.py             ← Rotating log system
│   ├── encryption.py         ← AES-256-CBC encrypt/decrypt
│   ├── layer1_local.py       ← Local backup (compressed + encrypted)
│   ├── layer2_drive.py       ← Secondary drive (USB/HDD) backup
│   ├── layer3_onedrive.py    ← OneDrive cloud backup (Graph API)
│   └── notifier.py           ← Email alert on success/failure
│
├── local_backups/            ← Layer 1 output (auto-created)
├── logs/                     ← Timestamped log files (auto-created)
├── .backup_key.key           ← AES-256 key (AUTO-GENERATED, KEEP SAFE!)
└── .token_cache.json         ← OneDrive OAuth token (auto-created)
```

---

## 🔧 Configuration (backup_config.json)

| Key | Description |
|-----|-------------|
| `database.path` | Full path to your `PharmaDB.sqlite` |
| `layer1_local.keep_last_n` | How many local backups to keep (default: 7) |
| `layer2_drive.target_drives` | List of drives to try e.g. `["D:\\", "E:\\"]` |
| `layer3_onedrive.client_id` | Azure App Registration Client ID |
| `schedule.time` | Time to run daily backup e.g. `"02:00"` |
| `notifications.enabled` | Set `true` + fill SMTP to get email alerts |

---

## 🚀 Usage Commands

```bash
# Run full 3-layer backup
python medixa_backup.py

# Run only Layer 1 (local)
python medixa_backup.py --layer 1

# Run only Layer 2 (secondary drive)
python medixa_backup.py --layer 2 

# Run only Layer 3 (OneDrive cloud)
python medixa_backup.py --layer 3

# Show backup status
python medixa_backup.py --status

# Restore latest backup
python medixa_backup.py --restore

# Authenticate OneDrive (first time only)  
python medixa_backup.py --auth
```

---

## 🔐 Layer 3: OneDrive Setup (One-Time)

### Step 1 — Create Azure App
1. Go to: https://portal.azure.com
2. Search → **App Registrations** → **New Registration**
3. Name: `MedixaBackup`, Supported: **Personal Microsoft accounts**
4. Redirect URI: `http://localhost:8080` (Web type)
5. Click **Register**

### Step 2 — Add Permissions
1. **API Permissions** → Add → Microsoft Graph
2. **Delegated** → `Files.ReadWrite` + `offline_access`
3. **Grant Admin Consent**

### Step 3 — Copy Credentials
1. Copy **Application (client) ID** → paste in `backup_config.json` → `client_id`
2. **Certificates & Secrets** → New Client Secret → copy value → `client_secret`

### Step 4 — Authenticate
```bash
python medixa_backup.py --auth
```
Browser opens → Login with Microsoft → Token saved automatically ✅

---

## ⏰ Windows Auto-Scheduler (Daily at 2 AM)

```batch
Right-click: setup_scheduler.bat → Run as Administrator
```

This registers a Windows Task Scheduler job that:
- Runs **daily at 2:00 AM** silently
- No window appears on screen
- Runs as SYSTEM user (even if no one is logged in)

**To verify**: Open Task Scheduler → Look for `MedixaPharmacyAutoBackup`

---

## 🛡️ Security Architecture

| Feature | Implementation |
|---------|---------------|
| Encryption | AES-256-CBC with random IV per file |
| Key Storage | `.backup_key.key` (chmod 400) — NEVER upload this |
| Token Storage | `.token_cache.json` (chmod 600) |
| DB Copy | SQLite hot-copy (safe while DB is in use) |
| Drive Copy | Size-verified + retry with exponential backoff |
| Cloud Upload | Chunked upload for large files (>4MB) |

---

## 📧 Email Notifications (Optional)

In `backup_config.json`:
```json
"notifications": {
  "enabled": true,
  "email": {
    "smtp_host": "smtp.gmail.com",
    "smtp_port": 587,
    "sender_email": "your@gmail.com",
    "sender_password": "your_16_char_app_password",
    "recipient_email": "admin@pharmacy.com"
  }
}
```

> **Gmail**: Use App Passwords (my account → Security → App passwords)

---

## 🔄 Backup Rotation Policy

| Layer | Location | Retention |
|-------|----------|-----------|
| Layer 1 | `local_backups/` | **7 days** (configurable) |
| Layer 2 | `D:\MedixaBackups\` | **14 days** (configurable) |
| Layer 3 | `OneDrive/MedixaPharmacyBackups/` | **30 days** (configurable) |

---

## 📋 Log Files

All operations are logged to `logs/backup_YYYY-MM-DD.log`:

```
2026-04-05 02:00:01 | INFO     | ==================================================
2026-04-05 02:00:01 | INFO     | MEDIXA PHARMACY - FULL BACKUP STARTED @ 2026-04-05
2026-04-05 02:00:01 | INFO     | [L1 LOCAL] ─── Starting Local Backup ─────────────
2026-04-05 02:00:02 | INFO     | [ENCRYPT] PharmaDB.sqlite → Medixa_Backup_...enc
2026-04-05 02:00:02 | INFO     | [L1 LOCAL] ✅ Backup saved: Medixa_Backup_20260405.zip.enc
2026-04-05 02:00:03 | INFO     | [L2 DRIVE] ✅ Backup copied to drive (D:\)
2026-04-05 02:00:05 | INFO     | [L3 CLOUD] ✅ Uploaded: Medixa_Backup_20260405.zip.enc
```

---

## ⚠️ IMPORTANT — Key File Safety

> The file `.backup_key.key` is your **AES-256 master key**.
> If lost, **encrypted backups CANNOT be restored**.
>
> Store this key separately: USB drive, printed paper, password manager.

---

## 📦 Dependencies

```
pycryptodome==3.21.0   # AES-256 encryption
requests==2.32.3       # OneDrive Graph API calls
python-dateutil==2.9.0 # Date utilities
```

Install: `pip install -r requirements.txt`

---

*Built for Medixa Pharmacy — Production Healthcare Backup System v1.0*
