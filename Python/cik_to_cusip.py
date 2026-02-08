#!/usr/bin/env python3
"""
CIK -> CUSIP finder (best-effort) by parsing SEC EDGAR filing text.

Usage:
  python cik_to_cusip.py --cik 0000001750
  python cik_to_cusip.py --csv /mnt/data/samples_2010_2023.csv --cik-col cik --out out_with_cusip.csv

What it does:
- Uses SEC "submissions" JSON to get recent filings.
- Downloads the filing .txt from EDGAR archives.
- Searches for "CUSIP" and extracts nearby CUSIP-looking strings.
"""

import argparse
import csv
import re
import time
from dataclasses import dataclass
from typing import Optional, List, Tuple, Dict

import requests


# --- Config ---
SEC_SUBMISSIONS = "https://data.sec.gov/submissions/CIK{cik10}.json"
ARCHIVES_BASE = "https://www.sec.gov/Archives/edgar/data/{cik_int}/{accession_nodash}/{filename}"

# IMPORTANT: Set a real user agent per SEC guidelines.
USER_AGENT = "Your Name your.email@example.com (CIK-to-CUSIP script)"

# Filing forms to try first (roughly most likely to contain equity CUSIP)
PREFERRED_FORMS = {"10-K", "10-Q", "8-K", "20-F", "40-F", "6-K", "424B", "S-1", "S-3", "F-1", "F-3"}

# Regex for CUSIP candidates (CUSIP9 is 9 chars alnum; last char can be digit or X)
# We'll capture upper-case alnum, length 9, often looks like ######### or ########X or alnum.
CUSIP9_RE = re.compile(r"\b([0-9A-Z]{9})\b")
# A bit stricter for common equity style: often 8 digits + check digit/X
CUSIP9_STRICT_RE = re.compile(r"\b([0-9A-Z]{8}[0-9X])\b")

# Extract around the word "CUSIP"
CONTEXT_RE = re.compile(r"(?i)cusip")


@dataclass
class CusipResult:
    cik: str
    cusip9: Optional[str] = None
    cusip8: Optional[str] = None
    source_accession: Optional[str] = None
    source_form: Optional[str] = None
    source_primary_doc: Optional[str] = None
    note: Optional[str] = None


def normalize_cik(cik: str) -> str:
    """Return a 10-digit zero-padded CIK string."""
    digits = re.sub(r"\D", "", cik or "")
    if not digits:
        raise ValueError("Empty/invalid CIK")
    return digits.zfill(10)


def sec_get_json(url: str, session: requests.Session) -> dict:
    r = session.get(url, headers={"User-Agent": USER_AGENT, "Accept-Encoding": "gzip, deflate"})
    r.raise_for_status()
    return r.json()


def sec_get_text(url: str, session: requests.Session, max_bytes: int = 3_000_000) -> str:
    """
    Download text; cap size to avoid massive files.
    We stream and stop at max_bytes.
    """
    r = session.get(url, headers={"User-Agent": USER_AGENT, "Accept-Encoding": "gzip, deflate"}, stream=True)
    r.raise_for_status()
    chunks = []
    total = 0
    for chunk in r.iter_content(chunk_size=65536):
        if not chunk:
            continue
        total += len(chunk)
        chunks.append(chunk)
        if total >= max_bytes:
            break
    data = b"".join(chunks)
    # EDGAR filing .txt is usually latin-1 safe; utf-8 sometimes works. We'll be tolerant.
    return data.decode("utf-8", errors="ignore")


def pick_recent_filings(submissions: dict, limit: int = 20) -> List[Tuple[str, str, str]]:
    """
    Return a list of (accessionNumber, form, primaryDocument) tuples,
    prioritizing preferred forms.
    """
    recent = submissions.get("filings", {}).get("recent", {})
    accessions = recent.get("accessionNumber", []) or []
    forms = recent.get("form", []) or []
    primary_docs = recent.get("primaryDocument", []) or []

    rows = []
    for acc, form, doc in zip(accessions, forms, primary_docs):
        if not acc or not form or not doc:
            continue
        rows.append((acc, form, doc))

    # Rank: preferred forms first, then keep order (which is already newest-first)
    preferred = [r for r in rows if r[1] in PREFERRED_FORMS]
    other = [r for r in rows if r[1] not in PREFERRED_FORMS]
    ranked = preferred + other
    return ranked[:limit]


def accession_no_dashes(accession: str) -> str:
    return accession.replace("-", "")


def find_cusips_in_text(text: str) -> List[str]:
    """
    Find CUSIP candidates near the word CUSIP.
    Strategy:
      - Find indices where 'CUSIP' appears
      - Take a window around each occurrence
      - Extract strict CUSIP9 candidates
    """
    candidates: List[str] = []

    # If CUSIP isn't in the doc, still do a light scan (some filings list CUSIP without label)
    has_cusip_label = bool(CONTEXT_RE.search(text))

    windows = []
    if has_cusip_label:
        for m in CONTEXT_RE.finditer(text):
            start = max(0, m.start() - 200)
            end = min(len(text), m.end() + 400)
            windows.append(text[start:end])
    else:
        # fallback: sample first N chars and last N chars
        n = min(len(text), 400_000)
        windows = [text[:n], text[-n:] if len(text) > n else text]

    for w in windows:
        for m in CUSIP9_STRICT_RE.finditer(w.upper()):
            candidates.append(m.group(1))

    # De-dup while preserving order
    seen = set()
    out = []
    for c in candidates:
        if c not in seen:
            seen.add(c)
            out.append(c)
    return out


def score_candidate(cusip9: str) -> int:
    """
    Heuristic scoring:
      - Pure digits is common for equities -> +2
      - Ends with X (also common) -> +1
      - Contains many letters -> -1 (still possible, but less common for equities)
    """
    s = cusip9.upper()
    score = 0
    if s.isdigit():
        score += 2
    if s.endswith("X"):
        score += 1
    letters = sum(ch.isalpha() for ch in s)
    if letters >= 2:
        score -= 1
    return score


def choose_best_cusip(candidates: List[str]) -> Optional[str]:
    if not candidates:
        return None
    scored = sorted(((score_candidate(c), i, c) for i, c in enumerate(candidates)), reverse=True)
    # Highest score, then earliest occurrence
    return scored[0][2]


def cik_to_cusip(cik: str, session: requests.Session, filings_to_try: int = 15, sleep_sec: float = 0.2) -> CusipResult:
    cik10 = normalize_cik(cik)
    cik_int = int(cik10)

    res = CusipResult(cik=cik10)

    # 1) Get submissions JSON
    submissions_url = SEC_SUBMISSIONS.format(cik10=cik10)
    try:
        submissions = sec_get_json(submissions_url, session)
    except Exception as e:
        res.note = f"Failed to fetch submissions: {e}"
        return res

    filings = pick_recent_filings(submissions, limit=filings_to_try)

    # 2) Try each filing's primary document as a .txt (EDGAR usually provides filing text as .txt)
    for accession, form, primary_doc in filings:
        acc_nodash = accession_no_dashes(accession)

        # The filing text is often available as "{accession}.txt" at the accession folder.
        # Example path: /edgar/data/{cik}/{acc}/{acc}.txt
        filing_txt = f"{acc_nodash}.txt"
        filing_url = ARCHIVES_BASE.format(
            cik_int=cik_int, accession_nodash=acc_nodash, filename=filing_txt
        )

        try:
            text = sec_get_text(filing_url, session)
        except Exception:
            # If .txt isn't there (rare), try the primary document itself
            filing_url = ARCHIVES_BASE.format(
                cik_int=cik_int, accession_nodash=acc_nodash, filename=primary_doc
            )
            try:
                text = sec_get_text(filing_url, session)
            except Exception:
                time.sleep(sleep_sec)
                continue

        candidates = find_cusips_in_text(text)
        best = choose_best_cusip(candidates)

        if best:
            res.cusip9 = best
            res.cusip8 = best[:8]
            res.source_accession = accession
            res.source_form = form
            res.source_primary_doc = primary_doc
            res.note = f"Found {len(candidates)} candidate(s) in filing text"
            return res

        time.sleep(sleep_sec)

    res.note = "No CUSIP found in recent filings searched"
    return res


def process_csv(input_csv: str, cik_col: str, out_csv: str) -> None:
    with requests.Session() as session:
        with open(input_csv, newline="", encoding="utf-8") as f_in:
            reader = csv.DictReader(f_in, delimiter=";")
            fieldnames = reader.fieldnames or []
            if cik_col not in fieldnames:
                raise ValueError(f"CIK column '{cik_col}' not found. Columns: {fieldnames}")

            # Add output columns if not present
            extra_cols = ["cusip9", "cusip8", "cusip_source_accession", "cusip_source_form", "cusip_note"]
            out_fields = fieldnames + [c for c in extra_cols if c not in fieldnames]

            rows = list(reader)

        with open(out_csv, "w", newline="", encoding="utf-8") as f_out:
            writer = csv.DictWriter(
                f_out,
                fieldnames=out_fields,
                delimiter=";"
            )
            writer.writeheader()

            for i, row in enumerate(rows, start=1):
                cik = str(row.get(cik_col, "")).strip()
                if not cik:
                    row["cusip_note"] = "Missing CIK"
                    writer.writerow(row)
                    continue

                result = cik_to_cusip(cik, session)
                row["cusip9"] = result.cusip9 or ""
                row["cusip8"] = result.cusip8 or ""
                row["cusip_source_accession"] = result.source_accession or ""
                row["cusip_source_form"] = result.source_form or ""
                row["cusip_note"] = result.note or ""
                writer.writerow(row)

                # Gentle pacing for SEC
                time.sleep(0.2)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--cik", help="Single CIK to resolve")
    ap.add_argument("--csv", help="Path to CSV containing CIKs")
    ap.add_argument("--cik-col", default="cik", help="Column name in CSV that contains CIKs (default: cik)")
    ap.add_argument("--out", default="out_with_cusip.csv", help="Output CSV path (default: out_with_cusip.csv)")
    args = ap.parse_args()

    if not args.cik and not args.csv:
        ap.error("Provide either --cik or --csv")

    if args.cik:
        with requests.Session() as session:
            r = cik_to_cusip(args.cik, session)
            print(f"CIK: {r.cik}")
            print(f"CUSIP9: {r.cusip9}")
            print(f"CUSIP8: {r.cusip8}")
            print(f"Source: accession={r.source_accession}, form={r.source_form}, primaryDoc={r.source_primary_doc}")
            print(f"Note: {r.note}")

    if args.csv:
        process_csv(args.csv, args.cik_col, args.out)
        print(f"Wrote: {args.out}")


if __name__ == "__main__":
    main()
