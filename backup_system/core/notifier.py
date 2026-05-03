"""
==============================================================================
Medixa Pharmacy Backup System - Email & Log Notifier
==============================================================================
Author  : Senior Google SWE Implementation
Purpose : Send email alerts on backup success or failure.
          Falls back to log-only if email is not configured.
==============================================================================
"""

import logging
import smtplib
import traceback
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from datetime import datetime

logger = logging.getLogger("MedixaBackup")


class Notifier:
    """
    Sends email notifications for backup events.

    Example:
        n = Notifier(cfg["notifications"])
        n.send_success(["L1: local_backup.zip.enc", "L2: D:/...", "L3: OneDrive"])
        n.send_failure("Layer 1 failed: DB not found")
    """

    def __init__(self, notif_cfg: dict):
        self._enabled: bool = bool(notif_cfg.get("enabled", False))
        email_cfg = notif_cfg.get("email", {})
        self._smtp_host: str = email_cfg.get("smtp_host", "smtp.gmail.com")
        self._smtp_port: int = int(email_cfg.get("smtp_port", 587))
        self._sender: str = email_cfg.get("sender_email", "")
        self._password: str = email_cfg.get("sender_password", "")
        self._recipient: str = email_cfg.get("recipient_email", "")

    def send_success(self, saved_paths: list):
        """Notify that backup completed successfully."""
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        subject = f"✅ Medixa Backup SUCCESS — {timestamp}"
        paths_html = "".join(f"<li>{p}</li>" for p in saved_paths if p)
        body = f"""
        <html><body>
        <h2 style="color:green;">✅ Medixa Pharmacy Backup Successful</h2>
        <p><b>Date/Time:</b> {timestamp}</p>
        <h3>Saved Locations:</h3>
        <ul>{paths_html}</ul>
        <p style="color:gray;">This is an automated system notification.</p>
        </body></html>
        """
        logger.info(f"[NOTIFY] ✅ Backup SUCCESS — {len([p for p in saved_paths if p])}/3 layers saved.")
        self._send(subject, body)

    def send_failure(self, error_msg: str):
        """Notify that backup failed."""
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        subject = f"❌ Medixa Backup FAILED — {timestamp}"
        body = f"""
        <html><body>
        <h2 style="color:red;">❌ Medixa Pharmacy Backup FAILED</h2>
        <p><b>Date/Time:</b> {timestamp}</p>
        <p><b>Error:</b> {error_msg}</p>
        <p><b>Action Required:</b> Check backup_system/logs/ for details.</p>
        </body></html>
        """
        logger.error(f"[NOTIFY] ❌ Backup FAILED — {error_msg}")
        self._send(subject, body)

    def _send(self, subject: str, html_body: str):
        """Send HTML email via SMTP."""
        if not self._enabled:
            logger.debug("[NOTIFY] Email notifications disabled. Skipping.")
            return

        if not self._sender or not self._recipient:
            logger.warning("[NOTIFY] Email credentials not configured. Skipping.")
            return

        try:
            msg = MIMEMultipart("alternative")
            msg["Subject"] = subject
            msg["From"] = self._sender
            msg["To"] = self._recipient
            msg.attach(MIMEText(html_body, "html"))

            with smtplib.SMTP(self._smtp_host, self._smtp_port) as server:
                server.ehlo()
                server.starttls()
                server.login(self._sender, self._password)
                server.sendmail(self._sender, self._recipient, msg.as_string())

            logger.info(f"[NOTIFY] Email sent to {self._recipient}: {subject}")
        except Exception as ex:
            logger.warning(f"[NOTIFY] Email send failed: {ex}")
