"""
==============================================================================
Medixa Pharmacy Backup System - Core Logger
==============================================================================
Author  : Senior Google SWE Implementation
Purpose : Centralized, production-grade logging with rotation and console
          output. All backup layers write to this single logger.
==============================================================================
"""

import logging
import os
from datetime import datetime, timedelta
from logging.handlers import RotatingFileHandler
from pathlib import Path


def setup_logger(log_dir: str, log_level: str = "INFO", keep_days: int = 90) -> logging.Logger:
    """
    Creates a production-grade logger with:
      - Rotating file handler (max 5 MB per file, 3 backups)
      - Colored console output
      - Auto-cleans logs older than `keep_days`
    """
    Path(log_dir).mkdir(parents=True, exist_ok=True)

    # Clean old logs
    _clean_old_logs(log_dir, keep_days)

    log_filename = os.path.join(
        log_dir, f"backup_{datetime.now().strftime('%Y-%m-%d')}.log"
    )

    logger = logging.getLogger("MedixaBackup")
    logger.setLevel(getattr(logging, log_level.upper(), logging.INFO))

    # Avoid duplicate handlers on re-import
    if logger.handlers:
        logger.handlers.clear()

    # ── File Handler (Rotating) ──────────────────────────────────────────────
    file_fmt = logging.Formatter(
        fmt="%(asctime)s | %(levelname)-8s | %(name)s | %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S"
    )
    file_handler = RotatingFileHandler(
        log_filename, maxBytes=5 * 1024 * 1024, backupCount=3, encoding="utf-8"
    )
    file_handler.setFormatter(file_fmt)
    logger.addHandler(file_handler)

    # ── Console Handler (with color indicators) ──────────────────────────────
    console_fmt = logging.Formatter(
        fmt="%(asctime)s | %(levelname)-8s | %(message)s",
        datefmt="%H:%M:%S"
    )
    console_handler = logging.StreamHandler()
    console_handler.setFormatter(console_fmt)
    logger.addHandler(console_handler)

    logger.info("-" * 70)
    logger.info("MEDIXA PHARMACY BACKUP SYSTEM - Session Started")
    logger.info("Log file: " + log_filename)
    logger.info("-" * 70)

    return logger


def _clean_old_logs(log_dir: str, keep_days: int) -> None:
    """Remove log files older than keep_days to manage disk space."""
    cutoff = datetime.now() - timedelta(days=keep_days)
    for f in Path(log_dir).glob("backup_*.log*"):
        try:
            if datetime.fromtimestamp(f.stat().st_mtime) < cutoff:
                f.unlink()
        except Exception:
            pass
