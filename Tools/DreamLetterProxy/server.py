#!/usr/bin/env python3
"""Local Dream Letter proxy. Unity talks only to this process; the DeepSeek key stays here."""
from __future__ import annotations

import json
import os
import ssl
import urllib.error
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PROMPTS = ROOT / "prompts"
ENV_PATH = ROOT / ".env"
PORT = int(os.environ.get("DREAM_LETTER_PORT", "8787"))
DEEPSEEK_URL = "https://api.deepseek.com/chat/completions"
MODEL = "deepseek-chat"


def load_env() -> None:
    if not ENV_PATH.exists():
        return
    for line in ENV_PATH.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        os.environ.setdefault(key.strip(), value.strip().strip('"').strip("'"))


def read_prompt(name: str) -> str:
    return (PROMPTS / name).read_text(encoding="utf-8")


def api_key() -> str:
    key = os.environ.get("DEEPSEEK_API_KEY", "").strip()
    if not key:
        raise RuntimeError("DEEPSEEK_API_KEY missing. Put it in Tools/DreamLetterProxy/.env")
    return key


def call_deepseek(system: str, user: str, temperature: float, max_tokens: int) -> dict:
    payload = {
        "model": MODEL,
        "temperature": temperature,
        "max_tokens": max_tokens,
        "response_format": {"type": "json_object"},
        "messages": [
            {"role": "system", "content": system},
            {"role": "user", "content": user},
        ],
    }
    data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    req = urllib.request.Request(
        DEEPSEEK_URL,
        data=data,
        headers={
            "Authorization": "Bearer " + api_key(),
            "Content-Type": "application/json",
        },
        method="POST",
    )
    ctx = ssl.create_default_context()
    with urllib.request.urlopen(req, timeout=50, context=ctx) as resp:
        body = json.loads(resp.read().decode("utf-8"))
    content = body["choices"][0]["message"]["content"]
    return json.loads(content)


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt: str, *args) -> None:
        print("[dream-proxy]", self.address_string(), fmt % args)

    def _read_json(self) -> dict:
        length = int(self.headers.get("Content-Length", "0"))
        raw = self.rfile.read(length) if length else b"{}"
        try:
            return json.loads(raw.decode("utf-8"))
        except json.JSONDecodeError:
            return {}

    def _send(self, code: int, obj: dict) -> None:
        raw = json.dumps(obj, ensure_ascii=False).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)

    def do_POST(self) -> None:  # noqa: N802
        payload = self._read_json()
        try:
            if self.path.rstrip("/") == "/v1/dream/analyze":
                dream = str(payload.get("dream_text") or "")
                user = "只返回JSON。输入数据：\n" + json.dumps({"dream_text": dream}, ensure_ascii=False)
                result = call_deepseek(read_prompt("analyze_system.txt"), user, 0.2, 600)
                self._send(200, result)
                return
            if self.path.rstrip("/") == "/v1/dream/letter":
                user = "只返回JSON。输入数据：\n" + json.dumps(payload, ensure_ascii=False)
                result = call_deepseek(read_prompt("letter_system.txt"), user, 0.9, 2800)
                self._send(200, result)
                return
            self._send(404, {"error": "not_found"})
        except Exception as exc:
            print("[dream-proxy] error:", exc)
            self._send(200, {"report_body": "", "error": str(exc)})


def main() -> None:
    load_env()
    server = ThreadingHTTPServer(("127.0.0.1", PORT), Handler)
    print(f"[dream-proxy] http://127.0.0.1:{PORT}  (Unity -> /v1/dream/analyze|letter)")
    server.serve_forever()


if __name__ == "__main__":
    main()
