"""Cliente mínimo do servidor MCP do pixel-plugin (pixel-mcp), por stdio.

O build lê e escreve arquivos .aseprite só por este servidor, como pede o AGENTS.md.
Configuração: o mesmo arquivo do /pixel-setup (PIXEL_MCP_CONFIG ou ~/.config/pixel-mcp/config.json).
"""

from __future__ import annotations

import json
import os
import pathlib
import platform
import subprocess

BINARIOS = {
    ("Windows", "AMD64"): "pixel-mcp-windows-amd64.exe",
    ("Linux", "x86_64"): "pixel-mcp-linux-amd64",
    ("Linux", "aarch64"): "pixel-mcp-linux-arm64",
    ("Darwin", "x86_64"): "pixel-mcp-darwin-amd64",
    ("Darwin", "arm64"): "pixel-mcp-darwin-arm64",
}


def localizar_binario() -> pathlib.Path:
    nome = BINARIOS.get((platform.system(), platform.machine()))
    if not nome:
        raise RuntimeError(f"plataforma sem pixel-mcp: {platform.system()} {platform.machine()}")
    cache = pathlib.Path.home() / ".claude/plugins/cache/pixel-plugin"
    achados = sorted(cache.glob(f"*/*/bin/{nome}"))
    if not achados:
        raise RuntimeError("pixel-plugin não instalado: rode `claude plugin install pixel-plugin@pixel-plugin`")
    return achados[-1]


class Aseprite:
    def __init__(self):
        config = os.environ.get("PIXEL_MCP_CONFIG") or str(pathlib.Path.home() / ".config/pixel-mcp/config.json")
        if not pathlib.Path(config).exists():
            raise RuntimeError(f"configuração do pixel-mcp ausente em {config}: rode /pixel-setup")
        env = dict(os.environ, PIXEL_MCP_CONFIG=config)
        self._p = subprocess.Popen([str(localizar_binario())], stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                   stderr=subprocess.DEVNULL, env=env)
        self._id = 0
        self._req("initialize", {"protocolVersion": "2024-11-05", "capabilities": {},
                                 "clientInfo": {"name": "wrexercise-avatar", "version": "1"}})
        self._enviar({"jsonrpc": "2.0", "method": "notifications/initialized"})

    def __enter__(self):
        return self

    def __exit__(self, *_):
        self._p.stdin.close()
        self._p.wait(timeout=30)

    def _enviar(self, msg):
        self._p.stdin.write((json.dumps(msg) + "\n").encode("utf-8"))
        self._p.stdin.flush()

    def _req(self, metodo, params):
        self._id += 1
        self._enviar({"jsonrpc": "2.0", "id": self._id, "method": metodo, "params": params})
        while True:
            linha = self._p.stdout.readline()
            if not linha:
                raise RuntimeError("pixel-mcp encerrou")
            if linha.strip().startswith(b"{"):
                r = json.loads(linha)
                if r.get("id") == self._id:
                    if "error" in r:
                        raise RuntimeError(r["error"])
                    return r["result"]

    def __call__(self, ferramenta: str, **args):
        # O pixel-mcp quebra com barras invertidas em caminhos do Windows.
        for k in ("sprite_path", "output_path", "image_path", "reference_path", "source_path"):
            if k in args:
                args[k] = str(args[k]).replace("\\", "/")
        r = self._req("tools/call", {"name": ferramenta, "arguments": args})
        texto = "".join(c.get("text", "") for c in r.get("content", []))
        if r.get("isError"):
            raise RuntimeError(f"{ferramenta}: {texto}")
        return json.loads(texto) if texto.strip().startswith(("{", "[")) else texto

    # --- atalhos ---

    def ler_cel(self, sprite, camada, quadro, w, h) -> dict[tuple[int, int], str]:
        """Pixels opacos de uma camada num quadro (1-base): {(x, y): '#rrggbb'}."""
        # O servidor devolve os pixels da cel inteira numa página (o cursor não avança
        # o conteúdo); com page_size = w*h a resposta já é completa.
        try:
            r = self("get_pixels", sprite_path=str(sprite), layer_name=camada, frame_number=quadro,
                     x=0, y=0, width=w, height=h, page_size=w * h)
        except RuntimeError as e:
            if "No cel found" in str(e):  # camada vazia neste quadro
                return {}
            raise
        out = {}
        for p in r["pixels"]:
            c = p["color"]
            if len(c) == 9 and c[7:9].lower() != "00" and 0 <= p["x"] < w and 0 <= p["y"] < h:
                out[(p["x"], p["y"])] = c[:7].lower()
        return out
