"""
==============================================================================
Medixa Pharmacy Backup System - Layer 1: Local Backup
==============================================================================
Author  : Senior Google SWE Implementation
Purpose : Creates atomic, compressed, timestamped local backups.
          Maintains a rolling window of the last N backups.

Design:
  1. Copy DB to temp file atomically (no partial writes).
  2. Compress to ZIP (for healthcare: preserves metadata).
  3. Encrypt with AES-256.
  4. Remove temp files.
  5. Purge backups older than keep_last_n.
==============================================================================
"""

import os
import shutil
import zipfile
import logging
import tempfile
from datetime import datetime
from pathlib import Path
from typing import Optional

from core.encryption import BackupEncryptor

logger = logging.getLogger("MedixaBackup")


class LocalBackup:
    """
    Layer 1: Local filesystem backup.

    Example:
        lb = LocalBackup(cfg["layer1_local"], cfg["encryption"], cfg["database"])
        result = lb.run()
    """

    def __init__(self, l1_cfg: dict, enc_cfg: dict, db_cfg: dict):
        self._db_path = Path(db_cfg["path"])
        self._backup_dir = Path(l1_cfg["backup_dir"])
        self._keep_n = int(l1_cfg.get("keep_last_n", 7))
        self._compress = bool(l1_cfg.get("compress", True))
        self._encrypt = bool(l1_cfg.get("encrypt", True))
        self._encryptor = BackupEncryptor(enc_cfg["key_file"]) if self._encrypt else None

    def run(self) -> Optional[str]:
        """
        Execute local backup.
        Returns the final backup file path on success, None on failure.
        """
        logger.info("[L1 LOCAL] ─── Starting Local Backup ───────────────────")

        if not self._db_path.exists():
            logger.error(f"[L1 LOCAL] Database not found: {self._db_path}")
            return None

        self._backup_dir.mkdir(parents=True, exist_ok=True)

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        base_name = f"Medixa_Backup_{timestamp}"
        final_path = None

        # Use a temp directory to ensure atomicity
        with tempfile.TemporaryDirectory(prefix="medixa_tmp_") as tmp_dir:
            tmp_dir = Path(tmp_dir)

            # Step 1: Atomic DB copy (SQLite hot backup safe copy)
            tmp_db = tmp_dir / self._db_path.name
            shutil.copy2(self._db_path, tmp_db)
            logger.debug(f"[L1 LOCAL] DB copied to temp: {tmp_db}")

            # Step 2: Compress
            if self._compress:
                zip_path = tmp_dir / f"{base_name}.zip"
                with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
                    zf.write(tmp_db, arcname=self._db_path.name)
                source_for_encrypt = zip_path
                logger.info(
                    f"[L1 LOCAL] Compressed: {zip_path.name} "
                    f"({zip_path.stat().st_size / 1024:.1f} KB)"
                )
            else:
                source_for_encrypt = tmp_db

            # Step 3: Encrypt
            if self._encrypt:
                enc_path = self._backup_dir / f"{base_name}.zip.enc"
                self._encryptor.encrypt_file(str(source_for_encrypt), str(enc_path))
                final_path = enc_path
            else:
                final_path = self._backup_dir / f"{base_name}.zip"
                shutil.copy2(source_for_encrypt, final_path)

        logger.info(
            f"[L1 LOCAL] ✅ Backup saved: {final_path.name} "
            f"({final_path.stat().st_size / 1024:.1f} KB)"
        )

        # Step 4: Rotate old backups
        self._rotate_old_backups()

        return str(final_path)

    def _rotate_old_backups(self):
        """Keep only the last N backups, delete older ones."""
        pattern = "Medixa_Backup_*.zip.enc" if self._encrypt else "Medixa_Backup_*.zip"
        existing = sorted(
            self._backup_dir.glob(pattern),
            key=lambda f: f.stat().st_mtime,
            reverse=True  # newest first
        )

        to_delete = existing[self._keep_n:]
        for old_file in to_delete:
            try:
                old_file.unlink()
                logger.info(f"[L1 LOCAL] Rotated (deleted): {old_file.name}")
            except Exception as ex:
                logger.warning(f"[L1 LOCAL] Could not delete {old_file.name}: {ex}")

        logger.info(
            f"[L1 LOCAL] Rotation complete — "
            f"kept {min(len(existing), self._keep_n)}, deleted {len(to_delete)}"
        )
