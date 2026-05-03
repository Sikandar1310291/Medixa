"""
==============================================================================
Medixa Pharmacy Backup System - Main Orchestrator
==============================================================================
Author  : Senior Google SWE Implementation

Usage:
    python medixa_backup.py             # Run full backup (all 3 layers)
    python medixa_backup.py --auth      # Authenticate OneDrive (first time)
    python medixa_backup.py --layer 1   # Run only Layer 1 (local)
    python medixa_backup.py --layer 2   # Run only Layer 2 (drive)
    python medixa_backup.py --restore   # Decrypt & restore latest local backup
    python medixa_backup.py --status    # Show backup status summary

Architecture:
    medixa_backup.py  (Orchestrator)
    └── core/
        ├── logger.py         - Centralized rotating logger
        ├── encryption.py     - AES-256-CBC encrypt/decrypt
        ├── layer1_local.py   - Local compressed+encrypted backup
        ├── layer2_drive.py   - Secondary drive with retry & rotation
        ├── layer3_onedrive.py - OneDrive via Microsoft Graph API
        └── notifier.py       - Email + log notifications
==============================================================================
"""

import json
import argparse
import io
import sys
import traceback
import zipfile
from datetime import datetime
from pathlib import Path

# Fix Windows PowerShell / cmd.exe encoding for Unicode output
if sys.platform == "win32":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")

# ── Path setup: add backup_system root to sys.path ───────────────────────────
BASE_DIR = Path(__file__).parent
sys.path.insert(0, str(BASE_DIR))

from core.logger import setup_logger  # noqa: E402
from core.layer1_local import LocalBackup  # noqa: E402
from core.layer2_drive import DriveBackup  # noqa: E402
from core.layer3_onedrive import OneDriveBackup  # noqa: E402
from core.encryption import BackupEncryptor  # noqa: E402
from core.notifier import Notifier  # noqa: E402


def load_config() -> dict:
    """Load and validate backup_config.json."""
    config_path = BASE_DIR / "backup_config.json"
    if not config_path.exists():
        print(f"❌ Config file not found: {config_path}")
        sys.exit(1)
    with open(config_path, encoding="utf-8") as f:
        return json.load(f)


def run_full_backup(cfg: dict, logger) -> dict:
    """
    Run all 3 backup layers sequentially.
    Returns a results dict with success/failure per layer.
    """
    results: dict[str, str | None] = {"l1": None, "l2": None, "l3": None}
    start_time = datetime.now()

    logger.info("=" * 70)
    logger.info(
        f"MEDIXA PHARMACY - FULL BACKUP STARTED @ "
        f"{start_time.strftime('%Y-%m-%d %H:%M:%S')}"
    )
    logger.info("=" * 70)

    # ── Layer 1: Local ────────────────────────────────────────────────────────
    if cfg["layer1_local"].get("enabled", True):
        try:
            l1 = LocalBackup(cfg["layer1_local"], cfg["encryption"], cfg["database"])
            results["l1"] = l1.run()
        except Exception as ex:
            logger.error(f"[L1 LOCAL] Unexpected error: {ex}")
            logger.debug(traceback.format_exc())
    else:
        logger.info("[L1 LOCAL] Disabled in config. Skipping.")

    # ── Layer 2: Drive ────────────────────────────────────────────────────────
    if cfg["layer2_drive"].get("enabled", True):
        if results["l1"]:
            try:
                l2 = DriveBackup(cfg["layer2_drive"])
                results["l2"] = l2.run(results["l1"])
            except Exception as ex:
                logger.error(f"[L2 DRIVE] Unexpected error: {ex}")
                logger.debug(traceback.format_exc())
        else:
            logger.warning("[L2 DRIVE] Skipped (Layer 1 did not produce output).")
    else:
        logger.info("[L2 DRIVE] Disabled in config. Skipping.")

    # ── Layer 3: OneDrive ─────────────────────────────────────────────────────
    if cfg["layer3_onedrive"].get("enabled", True):
        if results["l1"]:
            try:
                l3 = OneDriveBackup(cfg["layer3_onedrive"])
                results["l3"] = l3.run(results["l1"])
            except Exception as ex:
                logger.error(f"[L3 CLOUD] Unexpected error: {ex}")
                logger.debug(traceback.format_exc())
        else:
            logger.warning("[L3 CLOUD] Skipped (Layer 1 did not produce output).")
    else:
        logger.info("[L3 CLOUD] Disabled in config. Skipping.")

    elapsed = (datetime.now() - start_time).total_seconds()
    l1_status = "OK: " + results["l1"] if results["l1"] else "FAILED"
    l2_status = "OK: " + results["l2"] if results["l2"] else "FAILED/SKIPPED"
    l3_status = "OK: " + results["l3"] if results["l3"] else "FAILED/SKIPPED"
    logger.info("-" * 70)
    logger.info(f"BACKUP SUMMARY (completed in {elapsed:.1f}s):")
    logger.info(f"  Layer 1 (Local) : {l1_status}")
    logger.info(f"  Layer 2 (Drive) : {l2_status}")
    logger.info(f"  Layer 3 (Cloud) : {l3_status}")
    logger.info("-" * 70)

    return results


def print_status(cfg: dict):
    """Print a human-readable backup status summary."""
    l1_dir = Path(cfg["layer1_local"]["backup_dir"])
    pattern = "Medixa_Backup_*.zip.enc"
    files = sorted(l1_dir.glob(pattern), key=lambda f: f.stat().st_mtime, reverse=True)
    print("\nMEDIXA BACKUP STATUS")
    print("-" * 50)
    print(f"  Local Backup Dir : {l1_dir}")
    print(f"  Total Snapshots  : {len(files)}")
    if files:
        latest = files[0]
        mtime = datetime.fromtimestamp(latest.stat().st_mtime)
        print(f"  Latest Backup    : {latest.name}")
        print(f"  Latest Size      : {latest.stat().st_size / 1024:.1f} KB")
        print(f"  Latest Date      : {mtime.strftime('%Y-%m-%d %H:%M:%S')}")
    print("-" * 50)


def restore_latest(cfg: dict, logger):
    """Decrypt and restore the most recent local backup."""
    l1_dir = Path(cfg["layer1_local"]["backup_dir"])
    files = sorted(l1_dir.glob("Medixa_Backup_*.zip.enc"),
                   key=lambda f: f.stat().st_mtime, reverse=True)
    if not files:
        logger.error("[RESTORE] No encrypted backup files found in local dir.")
        return

    latest = files[0]
    logger.info(f"[RESTORE] Decrypting: {latest.name}")
    enc = BackupEncryptor(cfg["encryption"]["key_file"])

    out_zip = l1_dir / latest.name.replace(".enc", "")
    enc.decrypt_file(str(latest), str(out_zip))

    restore_dir = l1_dir / "restored"
    restore_dir.mkdir(exist_ok=True)
    with zipfile.ZipFile(out_zip, "r") as z:
        z.extractall(restore_dir)
    out_zip.unlink()

    logger.info(f"[RESTORE] SUCCESS - Restored to: {restore_dir}")
    print(f"\nRestore complete! Files extracted to:\n   {restore_dir}")


# ── Entry Point ───────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(
        description="Medixa Pharmacy - 3-Layer Automated Backup System"
    )
    parser.add_argument("--auth", action="store_true", help="Authenticate OneDrive only")
    parser.add_argument("--layer", type=int, choices=[1, 2, 3], help="Run a single layer")
    parser.add_argument("--restore", action="store_true", help="Restore latest local backup")
    parser.add_argument("--status", action="store_true", help="Show backup status")
    args = parser.parse_args()

    cfg = load_config()
    logger = setup_logger(
        cfg["logging"]["log_dir"],
        cfg["logging"].get("log_level", "INFO"),
        cfg["logging"].get("keep_logs_days", 90),
    )
    notifier = Notifier(cfg["notifications"])

    try:
        # ── Status Mode ───────────────────────────────────────────────────────
        if args.status:
            print_status(cfg)
            return

        # ── Restore Mode ──────────────────────────────────────────────────────
        if args.restore:
            restore_latest(cfg, logger)
            return

        # ── Auth Only Mode ────────────────────────────────────────────────────
        if args.auth:
            l3 = OneDriveBackup(cfg["layer3_onedrive"])
            if l3._authenticate():
                print("✅ OneDrive authentication successful! Token saved.")
            else:
                print("❌ Authentication failed.")
            return

        # ── Single Layer Mode ─────────────────────────────────────────────────
        if args.layer:
            if args.layer == 1:
                l1 = LocalBackup(cfg["layer1_local"], cfg["encryption"], cfg["database"])
                result = l1.run()
                print(f"\n{'✅ Success: ' + result if result else '❌ Layer 1 failed.'}")
            elif args.layer == 2:
                l1 = LocalBackup(cfg["layer1_local"], cfg["encryption"], cfg["database"])
                l1_path = l1.run()
                if l1_path:
                    l2 = DriveBackup(cfg["layer2_drive"])
                    result = l2.run(l1_path)
                    print(f"\n{'✅ Success: ' + result if result else '❌ Layer 2 failed.'}")
            elif args.layer == 3:
                l1 = LocalBackup(cfg["layer1_local"], cfg["encryption"], cfg["database"])
                l1_path = l1.run()
                if l1_path:
                    l3 = OneDriveBackup(cfg["layer3_onedrive"])
                    result = l3.run(l1_path)
                    print(f"\n{'✅ Success: ' + result if result else '❌ Layer 3 failed.'}")
            return

        # ── Full Backup Mode (Default) ─────────────────────────────────────────
        results = run_full_backup(cfg, logger)

        saved = [v for v in results.values() if v]
        if saved:
            notifier.send_success(list(results.values()))
        else:
            notifier.send_failure("All backup layers failed. Check logs.")

    except KeyboardInterrupt:
        logger.info("Backup interrupted by user.")
    except Exception as ex:
        logger.critical(f"Fatal error: {ex}")
        logger.debug(traceback.format_exc())
        notifier.send_failure(str(ex))
        sys.exit(1)


if __name__ == "__main__":
    main()
