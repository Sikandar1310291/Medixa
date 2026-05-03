"""
==============================================================================
Medixa Pharmacy Backup System - AES-256 Encryption Module
==============================================================================
Author  : Senior Google SWE Implementation
Purpose : Military-grade AES-256-CBC encryption for backup files.
          Used in all 3 backup layers before writing anywhere.

Security Design:
  - Generates a random 256-bit (32-byte) key on first run, stores in key file.
  - Uses a fresh random IV (16 bytes) per encryption — never reuses IVs.
  - IV is prepended to the ciphertext for transport (standard practice).
  - PKCS7 padding applied via PyCryptodome.
==============================================================================
"""

import os
import logging
from pathlib import Path
from Crypto.Cipher import AES
from Crypto.Random import get_random_bytes
from Crypto.Util.Padding import pad, unpad

logger = logging.getLogger("MedixaBackup")


class BackupEncryptor:
    """
    AES-256-CBC encryptor/decryptor.
    Usage:
        enc = BackupEncryptor("path/to/key.key")
        enc.encrypt_file("backup.zip", "backup.zip.enc")
        enc.decrypt_file("backup.zip.enc", "restored.zip")
    """

    BLOCK_SIZE = 16  # AES block size in bytes

    def __init__(self, key_file: str):
        self._key_file = Path(key_file)
        self._key = self._load_or_generate_key()

    # ── Key Management ────────────────────────────────────────────────────────

    def _load_or_generate_key(self) -> bytes:
        """Load existing key or generate a new 256-bit key and persist it."""
        self._key_file.parent.mkdir(parents=True, exist_ok=True)

        if self._key_file.exists():
            with open(self._key_file, "rb") as f:
                key = f.read()
            if len(key) == 32:
                logger.debug("Encryption key loaded from file.")
                return key
            logger.warning("Key file corrupt — regenerating key.")

        # Generate a cryptographically secure random 256-bit key
        key = get_random_bytes(32)
        with open(self._key_file, "wb") as f:
            f.write(key)

        # Restrict permissions (Windows: read-only for owner)
        try:
            os.chmod(self._key_file, 0o400)
        except Exception:
            pass

        logger.info(f"New AES-256 encryption key generated: {self._key_file}")
        logger.warning(
            "SECURITY: Back up your key file separately! "
            "Without it, encrypted backups CANNOT be restored."
        )
        return key

    # ── Encryption ────────────────────────────────────────────────────────────

    def encrypt_file(self, input_path: str, output_path: str) -> str:
        """
        Encrypt a file using AES-256-CBC.
        Output format: [16-byte IV][ciphertext]
        Returns the output path on success.
        """
        input_path = Path(input_path)
        output_path = Path(output_path)

        if not input_path.exists():
            raise FileNotFoundError(f"Cannot encrypt: {input_path} not found.")

        iv = get_random_bytes(self.BLOCK_SIZE)
        cipher = AES.new(self._key, AES.MODE_CBC, iv)

        with open(input_path, "rb") as f_in:
            plaintext = f_in.read()

        ciphertext = cipher.encrypt(pad(plaintext, self.BLOCK_SIZE))

        with open(output_path, "wb") as f_out:
            f_out.write(iv + ciphertext)

        size_kb = output_path.stat().st_size / 1024
        logger.info(
            f"[ENCRYPT] {input_path.name} → {output_path.name} ({size_kb:.1f} KB)"
        )
        return str(output_path)

    # ── Decryption ────────────────────────────────────────────────────────────

    def decrypt_file(self, input_path: str, output_path: str) -> str:
        """
        Decrypt an AES-256-CBC encrypted file.
        Input format: [16-byte IV][ciphertext]
        Returns the output path on success.
        """
        input_path = Path(input_path)
        output_path = Path(output_path)

        if not input_path.exists():
            raise FileNotFoundError(f"Cannot decrypt: {input_path} not found.")

        with open(input_path, "rb") as f_in:
            raw = f_in.read()

        iv = raw[:self.BLOCK_SIZE]
        ciphertext = raw[self.BLOCK_SIZE:]
        cipher = AES.new(self._key, AES.MODE_CBC, iv)
        plaintext = unpad(cipher.decrypt(ciphertext), self.BLOCK_SIZE)

        with open(output_path, "wb") as f_out:
            f_out.write(plaintext)

        logger.info(f"[DECRYPT] {input_path.name} → {output_path.name}")
        return str(output_path)
