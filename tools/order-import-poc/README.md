# Order import POC (Phase 12 pilot)

Standalone pilot for the historical order import (`docs/historical-order-import.md`). It is not part of the
app build. It runs the extraction step on a folder of archive scans and renders a review page, so accuracy
and cost can be measured before building the queue and the `/import` page.

| File | |
|---|---|
| `prompt.md` | System prompt: form versions and transcription rules. The app's extraction job will reuse it. |
| `schema.json` | Output schema (structured output). Maps onto `Order` as described in the design doc. |
| `extract.py` | Groups files by leading order number, sends each order's pages to the Claude API, appends JSON lines. Re-runnable (skips orders already done). |
| `report.py` | Builds a standalone HTML page: scans beside extracted fields, low-confidence fields highlighted, filters. |

## Running it

1. Download some order files from the Drive **order archives** folder into one local folder, keeping the
   Drive file names (`2166 - Customer Name.pdf`, `2166 - page 2.pdf`, …).
2. `pip install anthropic pillow` (Python 3.10+). Set `ANTHROPIC_API_KEY`.
3. `python extract.py ./scans --out results.jsonl` (add `--only 509,1077` or `--limit 20` to try a few;
   `--model claude-sonnet-5-5` to compare a cheaper model).
4. `python report.py results.jsonl ./scans --out report.html` and open it in a browser.

`results.jsonl` and `report.html` contain customer names, emails, phone numbers and the scans:
**keep them out of the repo** (`*.jsonl` / `report*.html` under this folder are git-ignored).

The script prints token usage and an estimated cost at the end. It uses `claude-opus-5-5` at `medium`
effort by default, with server-side refusal fallback enabled (`fallbacks: "default"`), so an order that
trips a safety classifier is retried on another model instead of failing.

## First POC (2026-10-01)

43 orders sampled across all four folders (#5 – #3904, every form version), transcribed in-session by
Claude against this prompt and schema (no API key in that environment yet). Results reviewed with the
scans side by side. Findings are summarized in the design doc under **POC results**.
