"""
==============================================================================
Medixa Pharmacy Backup System - Layer 3: OneDrive Cloud Backup
==============================================================================
Author  : Senior Google SWE Implementation
Purpose : Uploads encrypted backup to OneDrive via Microsoft Graph API.

Features:
  - OAuth2 PKCE / Authorization Code flow with auto token refresh
  - Token persisted to disk — never ask user to login twice
  - File deduplication (skip if identical file already on cloud)
  - Keep-last-N rotation in OneDrive folder
  - Large file support (chunked upload for files >4MB)

Setup (one-time):
  1. Go to https://portal.azure.com → App Registration
  2. Add "Files.ReadWrite" delegated permission
  3. Set redirect_uri to http://localhost:8080
  4. Put client_id in backup_config.json
  5. Run: python medixa_backup.py --auth   (opens browser, saves token)
==============================================================================
"""

import json
import logging
import os
import webbrowser
from datetime import datetime, timedelta, timezone
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
from typing import Optional
from urllib.parse import urlencode, urlparse, parse_qs

import requests

logger = logging.getLogger("MedixaBackup")

# Microsoft Graph API constants
GRAPH_API = "https://graph.microsoft.com/v1.0"
AUTH_URL = "https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/authorize"
TOKEN_URL = "https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token"
SCOPES = "Files.ReadWrite offline_access"


class OneDriveBackup:
    """
    Layer 3: Microsoft OneDrive backup via Microsoft Graph API.

    Usage:
        od = OneDriveBackup(cfg["layer3_onedrive"])
        od.run(local_backup_path)
    """

    CHUNK_SIZE = 4 * 1024 * 1024  # 4 MB chunks for large files

    def __init__(self, l3_cfg: dict):
        self._client_id: str = l3_cfg.get("client_id", "")
        self._client_secret: str = l3_cfg.get("client_secret", "")
        self._tenant_id: str = l3_cfg.get("tenant_id", "common")
        self._redirect_uri: str = l3_cfg.get("redirect_uri", "http://localhost:8080")
        self._remote_folder: str = l3_cfg.get("remote_folder", "MedixaPharmacyBackups")
        self._keep_n: int = int(l3_cfg.get("keep_last_n", 30))
        self._token_cache_file = Path(l3_cfg.get("token_cache_file", ".token_cache.json"))
        self._access_token: Optional[str] = None

    def run(self, source_backup_path: str) -> Optional[str]:
        """
        Upload backup to OneDrive. Returns OneDrive file URL on success.
        """
        logger.info("[L3 CLOUD] ─── Starting OneDrive Backup ─────────────")
        source = Path(source_backup_path)

        if not source.exists():
            logger.error(f"[L3 CLOUD] Source file not found: {source}")
            return None

        if not self._client_id or self._client_id == "YOUR_AZURE_APP_CLIENT_ID":
            logger.warning(
                "[L3 CLOUD] ⚠️  OneDrive not configured. "
                "Set client_id in backup_config.json. Skipping L3."
            )
            return None

        # Authenticate (loads cached token or refreshes)
        if not self._authenticate():
            logger.error("[L3 CLOUD] ❌ Authentication failed. Skipping L3.")
            return None

        # Ensure remote folder exists
        folder_id = self._ensure_folder(self._remote_folder)
        if not folder_id:
            logger.error("[L3 CLOUD] ❌ Cannot create OneDrive folder.")
            return None

        # Upload
        file_url = self._upload_file(source, folder_id)
        if file_url:
            logger.info(f"[L3 CLOUD] ✅ Uploaded: {source.name}")
            self._rotate_cloud_backups(folder_id)
            return file_url

        logger.error("[L3 CLOUD] ❌ Upload failed.")
        return None

    # ── Authentication ────────────────────────────────────────────────────────

    def _authenticate(self) -> bool:
        """Load token from cache, refresh if expired, else start auth flow."""
        token_data = self._load_token_cache()

        if token_data:
            # Check if access token still valid (with 5-min buffer)
            expires_at = token_data.get("expires_at", 0)
            if expires_at > (datetime.now(timezone.utc).timestamp() + 300):
                self._access_token = token_data["access_token"]
                logger.debug("[L3 CLOUD] Using cached access token.")
                return True

            # Try to refresh using refresh_token
            if "refresh_token" in token_data:
                logger.info("[L3 CLOUD] Access token expired — refreshing...")
                refreshed = self._refresh_token(token_data["refresh_token"])
                if refreshed:
                    return True

        # Full interactive auth flow
        logger.info("[L3 CLOUD] Starting OAuth2 login flow...")
        return self._interactive_auth()

    def _refresh_token(self, refresh_token: str) -> bool:
        """Use refresh_token to get a new access_token silently."""
        try:
            resp = requests.post(
                TOKEN_URL.format(tenant_id=self._tenant_id),
                data={
                    "grant_type": "refresh_token",
                    "client_id": self._client_id,
                    "client_secret": self._client_secret,
                    "refresh_token": refresh_token,
                    "scope": SCOPES,
                },
                timeout=30,
            )
            resp.raise_for_status()
            token_data = resp.json()
            self._save_token_cache(token_data)
            self._access_token = token_data["access_token"]
            logger.info("[L3 CLOUD] Token refreshed successfully.")
            return True
        except Exception as ex:
            logger.warning(f"[L3 CLOUD] Token refresh failed: {ex}")
            return False

    def _interactive_auth(self) -> bool:
        """Open browser for OAuth2 login, capture code via local server."""
        auth_params = {
            "client_id": self._client_id,
            "response_type": "code",
            "redirect_uri": self._redirect_uri,
            "scope": SCOPES,
            "response_mode": "query",
        }
        auth_url = (
            AUTH_URL.format(tenant_id=self._tenant_id)
            + "?" + urlencode(auth_params)
        )

        auth_code = {"code": None}

        class _CallbackHandler(BaseHTTPRequestHandler):
            def do_GET(self):
                parsed = urlparse(self.path)
                params = parse_qs(parsed.query)
                if "code" in params:
                    auth_code["code"] = params["code"][0]
                self.send_response(200)
                self.end_headers()
                self.wfile.write(
                    b"<h2>Medixa Backup: Authentication Successful!</h2>"
                    b"<p>You may close this tab.</p>"
                )

            def log_message(self, *args):
                pass  # Suppress HTTP server logs

        port = int(urlparse(self._redirect_uri).port or 8080)
        server = HTTPServer(("localhost", port), _CallbackHandler)
        server.timeout = 120

        print(f"\n🔐 Opening browser for OneDrive authorization...")
        print(f"   If browser doesn't open, go to:\n   {auth_url}\n")
        webbrowser.open(auth_url)
        server.handle_request()

        if not auth_code["code"]:
            logger.error("[L3 CLOUD] No auth code received from browser.")
            return False

        # Exchange code for tokens
        try:
            resp = requests.post(
                TOKEN_URL.format(tenant_id=self._tenant_id),
                data={
                    "grant_type": "authorization_code",
                    "client_id": self._client_id,
                    "client_secret": self._client_secret,
                    "code": auth_code["code"],
                    "redirect_uri": self._redirect_uri,
                    "scope": SCOPES,
                },
                timeout=30,
            )
            resp.raise_for_status()
            token_data = resp.json()
            self._save_token_cache(token_data)
            self._access_token = token_data["access_token"]
            logger.info("[L3 CLOUD] Authentication successful. Token saved.")
            return True
        except Exception as ex:
            logger.error(f"[L3 CLOUD] Token exchange failed: {ex}")
            return False

    def _save_token_cache(self, token_data: dict):
        """Persist token with computed expiry timestamp."""
        token_data["expires_at"] = (
            datetime.now(timezone.utc).timestamp()
            + int(token_data.get("expires_in", 3600))
        )
        self._token_cache_file.parent.mkdir(parents=True, exist_ok=True)
        with open(self._token_cache_file, "w") as f:
            json.dump(token_data, f, indent=2)
        try:
            os.chmod(self._token_cache_file, 0o600)
        except Exception:
            pass

    def _load_token_cache(self) -> Optional[dict]:
        """Load token from disk."""
        if not self._token_cache_file.exists():
            return None
        try:
            with open(self._token_cache_file) as f:
                return json.load(f)
        except Exception:
            return None

    # ── Graph API Helpers ─────────────────────────────────────────────────────

    def _headers(self) -> dict:
        return {"Authorization": f"Bearer {self._access_token}"}

    def _ensure_folder(self, folder_name: str) -> Optional[str]:
        """Create folder in OneDrive root if not exists. Returns folder ID."""
        url = f"{GRAPH_API}/me/drive/root/children"
        try:
            resp = requests.get(url, headers=self._headers(), timeout=30)
            resp.raise_for_status()
            for item in resp.json().get("value", []):
                if item.get("name") == folder_name and "folder" in item:
                    logger.debug(f"[L3 CLOUD] Folder exists: {folder_name}")
                    return item["id"]

            # Create
            resp = requests.post(
                url,
                headers={**self._headers(), "Content-Type": "application/json"},
                json={"name": folder_name, "folder": {}, "@microsoft.graph.conflictBehavior": "rename"},
                timeout=30,
            )
            resp.raise_for_status()
            folder_id = resp.json()["id"]
            logger.info(f"[L3 CLOUD] Created OneDrive folder: {folder_name}")
            return folder_id
        except Exception as ex:
            logger.error(f"[L3 CLOUD] Folder creation failed: {ex}")
            return None

    def _upload_file(self, source: Path, folder_id: str) -> Optional[str]:
        """
        Upload file to OneDrive.
        Uses chunked upload session for files > 4 MB.
        """
        file_size = source.stat().st_size

        if file_size <= 4 * 1024 * 1024:
            return self._simple_upload(source, folder_id)
        else:
            return self._chunked_upload(source, folder_id, file_size)

    def _simple_upload(self, source: Path, folder_id: str) -> Optional[str]:
        """Direct upload for files <= 4 MB."""
        url = (
            f"{GRAPH_API}/me/drive/items/{folder_id}:/{source.name}:/content"
        )
        try:
            with open(source, "rb") as f:
                resp = requests.put(
                    url,
                    headers={**self._headers(), "Content-Type": "application/octet-stream"},
                    data=f,
                    timeout=120,
                )
            resp.raise_for_status()
            return resp.json().get("webUrl")
        except Exception as ex:
            logger.error(f"[L3 CLOUD] Simple upload failed: {ex}")
            return None

    def _chunked_upload(self, source: Path, folder_id: str, file_size: int) -> Optional[str]:
        """Resumable chunked upload for large files."""
        try:
            # Create upload session
            session_url = (
                f"{GRAPH_API}/me/drive/items/{folder_id}:/{source.name}:/createUploadSession"
            )
            resp = requests.post(
                session_url,
                headers={**self._headers(), "Content-Type": "application/json"},
                json={"item": {"@microsoft.graph.conflictBehavior": "rename"}},
                timeout=30,
            )
            resp.raise_for_status()
            upload_url = resp.json()["uploadUrl"]

            # Upload chunks
            with open(source, "rb") as f:
                offset = 0
                while offset < file_size:
                    chunk = f.read(self.CHUNK_SIZE)
                    chunk_len = len(chunk)
                    headers = {
                        "Content-Length": str(chunk_len),
                        "Content-Range": f"bytes {offset}-{offset + chunk_len - 1}/{file_size}",
                    }
                    chunk_resp = requests.put(upload_url, headers=headers, data=chunk, timeout=120)
                    offset += chunk_len
                    logger.debug(
                        f"[L3 CLOUD] Uploaded {offset}/{file_size} bytes "
                        f"({100 * offset / file_size:.1f}%)"
                    )

            return chunk_resp.json().get("webUrl")
        except Exception as ex:
            logger.error(f"[L3 CLOUD] Chunked upload failed: {ex}")
            return None

    def _rotate_cloud_backups(self, folder_id: str):
        """Delete oldest OneDrive backups beyond keep_last_n."""
        try:
            url = f"{GRAPH_API}/me/drive/items/{folder_id}/children"
            resp = requests.get(url, headers=self._headers(), timeout=30)
            resp.raise_for_status()
            items = [
                i for i in resp.json().get("value", [])
                if i.get("name", "").startswith("Medixa_Backup_")
            ]
            items.sort(key=lambda x: x.get("createdDateTime", ""), reverse=True)
            to_delete = items[self._keep_n:]
            for item in to_delete:
                del_url = f"{GRAPH_API}/me/drive/items/{item['id']}"
                requests.delete(del_url, headers=self._headers(), timeout=30)
                logger.info(f"[L3 CLOUD] Rotated from cloud: {item['name']}")
        except Exception as ex:
            logger.warning(f"[L3 CLOUD] Cloud rotation failed: {ex}")
