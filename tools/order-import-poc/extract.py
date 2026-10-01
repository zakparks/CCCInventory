#!/usr/bin/env python3
"""Pilot extraction of historical order forms with the Claude API.

Reads a folder of order-archive files (as downloaded from the Drive "order archives" folder, e.g.
"2166 - Customer Name.pdf", "2166 - page 2.pdf"), groups them by leading order number, sends every
page of an order in one request, and appends one JSON line per order to the output file.

    pip install anthropic pillow
    export ANTHROPIC_API_KEY=...
    python extract.py ./scans --out results.jsonl            # every order in the folder
    python extract.py ./scans --out results.jsonl --only 509,1077,2166
    python report.py results.jsonl ./scans --out report.html  # side-by-side review page

Orders already present in the output file are skipped, so the script can be re-run after a failure.
Optional: --modified manifest.json maps file name -> ISO timestamp (Drive modifiedTime); otherwise the
local file mtime is used. It is passed to the model as a hint for inferring missing years.
"""

import argparse
import base64
import io
import json
import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

import anthropic

HERE = Path(__file__).parent
IMAGE_EXTS = {".jpg", ".jpeg", ".png", ".webp", ".gif"}
MAX_EDGE = 1568  # larger images are downscaled by the API anyway; resizing first keeps requests small

# USD per million tokens (input, output) for the cost estimate printed at the end.
PRICES = {"claude-opus-5-5": (4.00, 20.00), "claude-sonnet-5-5": (2.00, 10.00), "claude-haiku-4-5": (1.00, 5.00)}


def group_files(folder: Path) -> dict[int, list[Path]]:
    groups: dict[int, list[Path]] = {}
    for f in sorted(folder.iterdir()):
        m = re.match(r"\s*(\d+)\s*-", f.name)
        if not m or not f.is_file():
            continue
        if f.suffix.lower() not in IMAGE_EXTS and f.suffix.lower() != ".pdf":
            continue
        groups.setdefault(int(m.group(1)), []).append(f)
    # Main form first (the file named after the customer), then "page 2", "page 3", "z photo"...
    for files in groups.values():
        files.sort(key=lambda p: (bool(re.search(r"- *(page|pg|z )", p.name, re.I)), p.name.lower()))
    return groups


def image_block(path: Path) -> dict:
    data = path.read_bytes()
    media_type = "image/jpeg"
    try:
        from PIL import Image, ImageOps

        img = ImageOps.exif_transpose(Image.open(io.BytesIO(data)))
        if max(img.size) > MAX_EDGE:
            img.thumbnail((MAX_EDGE, MAX_EDGE))
        buf = io.BytesIO()
        img.convert("RGB").save(buf, "JPEG", quality=85)
        data = buf.getvalue()
    except ImportError:
        media_type = {".png": "image/png", ".webp": "image/webp", ".gif": "image/gif"}.get(path.suffix.lower(), "image/jpeg")
    return {"type": "image", "source": {"type": "base64", "media_type": media_type, "data": base64.standard_b64encode(data).decode()}}


def pdf_block(path: Path) -> dict:
    return {"type": "document", "source": {"type": "base64", "media_type": "application/pdf",
                                          "data": base64.standard_b64encode(path.read_bytes()).decode()}}


def neighbor_dates(done: dict[int, dict], number: int, k: int = 6) -> list[dict]:
    known = [(n, r["result"].get("dueDate")) for n, r in done.items() if r.get("result") and r["result"].get("dueDate")]
    known.sort(key=lambda t: abs(t[0] - number))
    return [{"orderNumber": n, "dueDate": d} for n, d in sorted(known[:k])]


def extract_order(client, args, system_prompt, schema, number, files, done, modified):
    content: list[dict] = []
    for f in files:
        content.append({"type": "text", "text": f"File: {f.name}"})
        content.append(pdf_block(f) if f.suffix.lower() == ".pdf" else image_block(f))
    context = {
        "orderNumberFromFileName": number,
        "fileNames": [f.name for f in files],
        "fileModified": {f.name: modified.get(f.name) or datetime.fromtimestamp(f.stat().st_mtime, timezone.utc).isoformat() for f in files},
        "neighborDates": neighbor_dates(done, number),
    }
    content.append({"type": "text", "text": "context:\n" + json.dumps(context, indent=1)
                    + "\n\nTranscribe this order into the JSON schema."})

    response = client.beta.messages.create(
        model=args.model,
        max_tokens=16000,
        betas=["server-side-fallback-2026-07-01"],
        fallbacks="default",  # if a request is declined by a safety classifier, the API retries on a fallback model
        system=[{"type": "text", "text": system_prompt, "cache_control": {"type": "ephemeral"}}],
        output_config={"effort": args.effort, "format": {"type": "json_schema", "schema": schema}},
        messages=[{"role": "user", "content": content}],
    )
    usage = {"input": response.usage.input_tokens, "output": response.usage.output_tokens,
             "cacheRead": getattr(response.usage, "cache_read_input_tokens", 0) or 0,
             "cacheWrite": getattr(response.usage, "cache_creation_input_tokens", 0) or 0}
    if response.stop_reason != "end_turn":
        return {"orderNumber": number, "files": [f.name for f in files], "error": f"stop_reason={response.stop_reason}",
                "usage": usage, "model": response.model}
    text = next(b.text for b in response.content if b.type == "text")
    return {"orderNumber": number, "files": [f.name for f in files], "result": json.loads(text),
            "usage": usage, "model": response.model}


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("folder", type=Path)
    ap.add_argument("--out", type=Path, default=Path("results.jsonl"))
    ap.add_argument("--only", help="comma-separated order numbers")
    ap.add_argument("--limit", type=int)
    ap.add_argument("--model", default="claude-opus-5-5")
    ap.add_argument("--effort", default="medium", choices=["low", "medium", "high", "xhigh", "max"])
    ap.add_argument("--modified", type=Path, help="JSON map of file name -> ISO modified time")
    args = ap.parse_args()

    system_prompt = (HERE / "prompt.md").read_text(encoding="utf-8")
    schema = json.loads((HERE / "schema.json").read_text(encoding="utf-8"))
    modified = json.loads(args.modified.read_text()) if args.modified else {}

    groups = group_files(args.folder)
    if args.only:
        wanted = {int(n) for n in args.only.split(",")}
        groups = {n: f for n, f in groups.items() if n in wanted}
    done: dict[int, dict] = {}
    if args.out.exists():
        for line in args.out.read_text(encoding="utf-8").splitlines():
            if line.strip():
                rec = json.loads(line)
                done[rec["orderNumber"]] = rec
    todo = [n for n in sorted(groups) if n not in done or "error" in done[n]]
    if args.limit:
        todo = todo[: args.limit]
    print(f"{len(groups)} orders in folder, {len(todo)} to extract -> {args.out}")

    client = anthropic.Anthropic()
    totals = {"input": 0, "output": 0, "cacheRead": 0, "cacheWrite": 0}
    with args.out.open("a", encoding="utf-8") as out:
        for i, number in enumerate(todo, 1):
            try:
                rec = extract_order(client, args, system_prompt, schema, number, groups[number], done, modified)
            except anthropic.APIStatusError as e:
                rec = {"orderNumber": number, "files": [f.name for f in groups[number]], "error": f"{e.status_code}: {e.message}"}
            except anthropic.APIConnectionError as e:
                rec = {"orderNumber": number, "files": [f.name for f in groups[number]], "error": f"connection: {e}"}
            done[number] = rec
            out.write(json.dumps(rec, ensure_ascii=False) + "\n")
            out.flush()
            for k in totals:
                totals[k] += rec.get("usage", {}).get(k, 0)
            r = rec.get("result") or {}
            print(f"[{i}/{len(todo)}] #{number}: " + (rec.get("error") or
                  f"{r.get('dueDate')} {r.get('customerName')} — {r.get('title')} (low: {', '.join(r.get('lowConfidenceFields', [])) or '-'})"))

    price_in, price_out = PRICES.get(args.model, (0, 0))
    cost = ((totals["input"] + totals["cacheWrite"] * 1.25 + totals["cacheRead"] * 0.1) * price_in + totals["output"] * price_out) / 1e6
    print(f"tokens: {totals}  ≈ ${cost:.2f} ({'$%.3f' % (cost / len(todo)) if todo else '-'} per order)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
