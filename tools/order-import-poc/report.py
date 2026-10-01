#!/usr/bin/env python3
"""Build a standalone HTML review page from extract.py results: each order's scan pages beside the
extracted fields, low-confidence fields highlighted, review notes listed.

    python report.py results.jsonl ./scans --out report.html

The page embeds the (downscaled) scans, so it contains customer data: keep it local, don't commit it.
"""

import argparse
import base64
import html
import io
import json
import shutil
import subprocess
import tempfile
from collections import Counter
from pathlib import Path

THUMB = 1000
FIELDS = [
    ("dueDate", "Due date"), ("dueTime", "Time"), ("orderType", "Type"), ("location", "Location"),
    ("customerName", "Name"), ("email", "Email"), ("phone", "Phone"), ("initialContact", "Contact via"),
    ("title", "Title"), ("totalCost", "Total"), ("depositAmount", "Deposit"), ("paidInFull", "Paid in full"),
    ("paymentRefs", "Receipt #"), ("paymentNotes", "Payment notes"), ("dateOrderPlaced", "Placed"),
    ("isWedding", "Wedding"), ("wedding", "Wedding details"), ("formEra", "Form era"),
]
KEY_FIELDS = ["dueDate", "customerName", "email", "phone", "totalCost"]


def page_images(path: Path) -> list[bytes]:
    if path.suffix.lower() == ".pdf":
        if not shutil.which("pdftoppm"):
            return []
        with tempfile.TemporaryDirectory() as tmp:
            subprocess.run(["pdftoppm", "-r", "90", "-jpeg", str(path), f"{tmp}/p"], check=True)
            return [p.read_bytes() for p in sorted(Path(tmp).glob("p*.jpg"))]
    data = path.read_bytes()
    try:
        from PIL import Image, ImageOps
        img = ImageOps.exif_transpose(Image.open(io.BytesIO(data)))
        img.thumbnail((THUMB, THUMB))
        buf = io.BytesIO()
        img.convert("RGB").save(buf, "JPEG", quality=72)
        return [buf.getvalue()]
    except ImportError:
        return [data]


def fmt(v) -> str:
    if v is None or v == [] or v == "":
        return '<span class="empty">—</span>'
    if isinstance(v, bool):
        return "Yes" if v else "No"
    if isinstance(v, list):
        return html.escape(", ".join(map(str, v)))
    if isinstance(v, dict):
        return "<br>".join(f"{html.escape(k)}: {html.escape(str(x))}" for k, x in v.items() if x)
    if isinstance(v, float):
        return f"${v:,.2f}"
    if isinstance(v, int) and not isinstance(v, bool):
        return f"${v:,}"
    return html.escape(str(v))


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("results", type=Path)
    ap.add_argument("scans", type=Path)
    ap.add_argument("--out", type=Path, default=Path("report.html"))
    ap.add_argument("--title", default="Order import POC")
    args = ap.parse_args()

    recs = [json.loads(l) for l in args.results.read_text(encoding="utf-8").splitlines() if l.strip()]
    recs.sort(key=lambda r: r["orderNumber"])
    ok = [r for r in recs if r.get("result")]

    status = Counter(r["result"]["status"] for r in ok)
    eras = Counter(r["result"]["formEra"] for r in ok)
    filled = {f: sum(1 for r in ok if r["result"].get(f) not in (None, "", [])) for f in KEY_FIELDS}
    low = sum(1 for r in ok if r["result"]["lowConfidenceFields"])
    noted = sum(1 for r in ok if r["result"]["reviewNotes"])
    usage = Counter()
    for r in recs:
        usage.update(r.get("usage", {}))

    cards = []
    for r in recs:
        res = r.get("result") or {}
        lowf = set(res.get("lowConfidenceFields", []))
        imgs = []
        for name in r["files"]:
            kind = next((p["kind"] for p in res.get("pages", []) if p["file"] == name), "")
            for data in page_images(args.scans / name):
                b64 = base64.b64encode(data).decode()
                imgs.append(f'<figure><img loading="lazy" src="data:image/jpeg;base64,{b64}" alt="">'
                            f'<figcaption>{html.escape(name)} <em>{html.escape(kind)}</em></figcaption></figure>')
        rows = "".join(
            f'<tr class="{"low" if k in lowf else ""}"><th>{label}</th><td>{fmt(res.get(k))}</td></tr>'
            for k, label in FIELDS if k != "wedding" or res.get("wedding"))
        notes = "".join(f"<li>{html.escape(n)}</li>" for n in res.get("reviewNotes", []))
        st = res.get("status", "Error")
        tags = " ".join(filter(None, [st.lower(), "low" if lowf else "", "notes" if notes else "",
                                      "wedding" if res.get("isWedding") else ""]))
        body = (f'<p class="err">{html.escape(r["error"])}</p>' if r.get("error") else
                f'<table>{rows}</table><h4>Summary</h4><p class="summary">{html.escape(res.get("summary", ""))}</p>'
                f'<p class="evidence"><b>Date:</b> {html.escape(res.get("dueDateEvidence", ""))}</p>'
                + (f'<h4>Review notes</h4><ul>{notes}</ul>' if notes else "")
                + (f'<p class="lowlist">Low confidence: {html.escape(", ".join(sorted(lowf)))}</p>' if lowf else ""))
        cards.append(
            f'<section class="card" data-tags="{tags}"><header><h3>#{r["orderNumber"]}</h3>'
            f'<span class="badge {st.lower()}">{st}</span>'
            + ('<span class="badge wedding">Wedding</span>' if res.get("isWedding") else "")
            + ('' if res.get("isCustomer", True) else '<span class="badge internal">Not a customer</span>')
            + f'<span class="model">{html.escape(r.get("model", ""))}</span></header>'
            f'<div class="cols"><div class="scans">{"".join(imgs)}</div><div class="fields">{body}</div></div></section>')

    stat = lambda d: " · ".join(f"{k} {v}" for k, v in sorted(d.items()))
    page = f"""<!doctype html><html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>{html.escape(args.title)}</title>
<style>
:root{{--bg:#f6f4f1;--card:#fff;--fg:#1d1b19;--mut:#6b655e;--line:#e3ded7;--low:#fff1c2;--lowfg:#6b4e00;--accent:#7a4b8c}}
@media (prefers-color-scheme:dark){{:root{{--bg:#171513;--card:#211e1b;--fg:#ece7e1;--mut:#a39b92;--line:#38332e;--low:#4a3b0c;--lowfg:#ffe08a;--accent:#c9a2d9}}}}
*{{box-sizing:border-box}} body{{margin:0;background:var(--bg);color:var(--fg);font:15px/1.45 system-ui,sans-serif}}
main{{max-width:1400px;margin:0 auto;padding:16px}} h1{{margin:.2em 0}} .stats{{color:var(--mut)}}
.filters button{{margin:4px 4px 0 0;padding:6px 12px;border:1px solid var(--line);border-radius:16px;background:var(--card);color:var(--fg);cursor:pointer}}
.filters button.on{{background:var(--accent);color:#fff;border-color:var(--accent)}}
.card{{background:var(--card);border:1px solid var(--line);border-radius:10px;margin:16px 0;padding:12px 16px}}
.card header{{display:flex;gap:8px;align-items:center;flex-wrap:wrap}} .card h3{{margin:0}}
.model{{margin-left:auto;color:var(--mut);font-size:12px}}
.badge{{font-size:12px;padding:2px 8px;border-radius:10px;background:var(--line)}}
.badge.cancelled,.badge.noshow{{background:#c0392b;color:#fff}} .badge.quote,.badge.unknown{{background:#7f8c8d;color:#fff}}
.badge.completed{{background:#2e7d4f;color:#fff}} .badge.wedding{{background:var(--accent);color:#fff}} .badge.internal{{background:#555;color:#fff}}
.cols{{display:grid;grid-template-columns:minmax(0,1.1fr) minmax(0,1fr);gap:16px;margin-top:8px}}
@media (max-width:800px){{.cols{{grid-template-columns:1fr}}}}
.scans{{display:flex;flex-direction:column;gap:8px;max-height:80vh;overflow:auto}}
figure{{margin:0}} img{{width:100%;border:1px solid var(--line);border-radius:6px;cursor:zoom-in}}
img.zoom{{position:fixed;inset:2vh 2vw;width:96vw;height:96vh;object-fit:contain;background:#000d;z-index:9;cursor:zoom-out}}
figcaption{{font-size:12px;color:var(--mut)}} table{{border-collapse:collapse;width:100%}}
th,td{{text-align:left;vertical-align:top;padding:4px 6px;border-bottom:1px solid var(--line)}} th{{width:8.5em;color:var(--mut);font-weight:500}}
tr.low td{{background:var(--low);color:var(--lowfg)}} .empty{{color:var(--mut)}} .summary{{white-space:pre-wrap}}
.evidence,.lowlist{{color:var(--mut);font-size:13px}} .err{{color:#c0392b}} h4{{margin:12px 0 4px}}
</style></head><body><main>
<h1>{html.escape(args.title)}</h1>
<p class="stats">{len(recs)} orders · {len(recs) - len(ok)} errors · status: {stat(status)} · form era: {stat(eras)}<br>
Key fields filled: {" · ".join(f"{k} {v}/{len(ok)}" for k, v in filled.items())} ·
{low} with low-confidence fields · {noted} with review notes
{"<br>Tokens: " + stat(usage) if usage else ""}</p>
<div class="filters"><button class="on" data-f="">All</button><button data-f="low">Low confidence</button>
<button data-f="notes">Has notes</button><button data-f="cancelled">Cancelled</button><button data-f="wedding">Wedding</button></div>
{"".join(cards)}
</main><script>
document.querySelectorAll('.filters button').forEach(b=>b.onclick=()=>{{
 document.querySelectorAll('.filters button').forEach(x=>x.classList.toggle('on',x===b));
 const f=b.dataset.f; document.querySelectorAll('.card').forEach(c=>c.hidden=!!f&&!c.dataset.tags.split(' ').includes(f));}});
document.addEventListener('click',e=>{{if(e.target.tagName==='IMG')e.target.classList.toggle('zoom')}});
</script></body></html>"""
    args.out.write_text(page, encoding="utf-8")
    print(f"wrote {args.out} ({args.out.stat().st_size / 1e6:.1f} MB, {len(recs)} orders)")


if __name__ == "__main__":
    main()
