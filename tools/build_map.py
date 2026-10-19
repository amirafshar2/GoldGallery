"""Rebuild docs/PROJECT_MAP.html from docs/PROJECT_MAP.md (single source of truth).
Usage: python tools/build_map.py
"""
from pathlib import Path
import html

root = Path(__file__).resolve().parent.parent
md = (root / "docs" / "PROJECT_MAP.md").read_text(encoding="utf-8")
tpl = (root / "tools" / "map_template.html").read_text(encoding="utf-8")
vendor = root / "tools" / "vendor"
out = (tpl.replace("{{MARKDOWN}}", html.escape(md))
          .replace("{{MARKED}}", (vendor / "marked.min.js").read_text(encoding="utf-8"))
          .replace("{{MERMAID}}", (vendor / "mermaid.min.js").read_text(encoding="utf-8")))
(root / "docs" / "PROJECT_MAP.html").write_text(out, encoding="utf-8")
print("docs/PROJECT_MAP.html rebuilt")
