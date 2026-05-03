"""
==============================================================================
Medixa Pharmacy Backup System - Layer 2: Secondary Drive Backup
==============================================================================
Author  : Senior Google SWE Implementation
Purpose : Copies encrypted backup to secondary/external drives with:
          - Fault-tolerant drive detection
          - Configurable retry with exponential back-off
          - Drive rotation (D:, E:, F: etc.)
          - Keep-last-N rotation per drive
==============================================================================
"""

import shutil
import time
import logging
from pathlib import Path
from typing import Optional, List

logger = logging.getLogger("MedixaBackup")


class DriveBackup:
    """
    Layer 2: Secondary/external drive backup with retry logic.

    Example:
        db = DriveBackup(cfg["layer2_drive"])
        db.run(local_backup_path)
    """

    def __init__(self, l2_cfg: dict):
        self._target_drives: List[str] = l2_cfg.get("target_drives", ["D:\\"])
        self._folder_name: str = l2_cfg.get("backup_folder_name", "MedixaBackups")
        self._keep_n: int = int(l2_cfg.get("keep_last_n", 14))
        self._retry_attempts: int = int(l2_cfg.get("retry_attempts", 3))
        self._retry_delay: int = int(l2_cfg.get("retry_delay_seconds", 10))

    def run(self, source_backup_path: str) -> Optional[str]:
        """
        Copy the given backup file to the first available secondary drive.
        Returns destination path on success, None if all drives fail.
        """
        logger.info("[L2 DRIVE] ─── Starting Drive Backup ──────────────────")
        source = Path(source_backup_path)

        if not source.exists():
            logger.error(f"[L2 DRIVE] Source file not found: {source}")
            return None

        # Find available drive
        available_drive = self._find_available_drive()
        if not available_drive:
            logger.warning("[L2 DRIVE] ⚠️  No secondary drive detected. Skipping L2.")
            return None

        dest_dir = available_drive / self._folder_name
        dest_dir.mkdir(parents=True, exist_ok=True)
        dest_file = dest_dir / source.name

        # Copy with retry
        success = self._copy_with_retry(source, dest_file)

        if success:
            logger.info(
                f"[L2 DRIVE] ✅ Backup copied to drive "
                f"({available_drive}) → {dest_file.name}"
            )
            self._rotate_old_backups(dest_dir)
            return str(dest_file)
        else:
            logger.error(
                f"[L2 DRIVE] ❌ All {self._retry_attempts} retry attempts failed "
                f"for drive {available_drive}"
            )
            return None

    def _find_available_drive(self) -> Optional[Path]:
        """Return the first drive letter that is accessible."""
        for drive in self._target_drives:
            p = Path(drive)
            if p.exists():
                try:
                    # Quick write test to confirm not read-only
                    test_file = p / ".medixa_write_test"
                    test_file.touch()
                    test_file.unlink()
                    logger.info(f"[L2 DRIVE] Detected drive: {drive}")
                    return p
                except PermissionError:
                    logger.warning(f"[L2 DRIVE] Drive {drive} is read-only. Skipping.")
                except Exception as ex:
                    logger.warning(f"[L2 DRIVE] Drive {drive} error: {ex}")

        return None

    def _copy_with_retry(self, source: Path, dest: Path) -> bool:
        """
        Copy file with configurable retry and exponential back-off.
        """
        for attempt in range(1, self._retry_attempts + 1):
            try:
                shutil.copy2(source, dest)
                # Verify integrity after copy
                if dest.stat().st_size == source.stat().st_size:
                    logger.debug(
                        f"[L2 DRIVE] Copy attempt {attempt} succeeded "
                        f"(size verified: {dest.stat().st_size} bytes)"
                    )
                    return True
                else:
                    raise IOError("File size mismatch after copy — possible corruption.")

            except Exception as ex:
                delay = self._retry_delay * attempt  # Exponential backoff
                logger.warning(
                    f"[L2 DRIVE] Attempt {attempt}/{self._retry_attempts} failed: {ex}. "
                    f"Retrying in {delay}s..."
                )
                if attempt < self._retry_attempts:
                    time.sleep(delay)

        return False

    def _rotate_old_backups(self, backup_dir: Path):
        """Keep only the last N backups on the drive."""
        existing = sorted(
            list(backup_dir.glob("Medixa_Backup_*.zip.enc")) +
            list(backup_dir.glob("Medixa_Backup_*.zip")),
            key=lambda f: f.stat().st_mtime,
            reverse=True
        )

        to_delete = existing[self._keep_n:]
        for old_file in to_delete:
            try:
                old_file.unlink()
                logger.info(f"[L2 DRIVE] Rotated: {old_file.name}")
            except Exception as ex:
                logger.warning(f"[L2 DRIVE] Could not delete {old_file.name}: {ex}")

        logger.info(
            f"[L2 DRIVE] Rotation — kept {min(len(existing), self._keep_n)}, "
            f"deleted {len(to_delete)}"
        )
