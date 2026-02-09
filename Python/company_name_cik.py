import os
import csv
import glob
from collections import defaultdict

INPUT_DIR = "output"
OUTPUT_FILE = os.path.join(INPUT_DIR, "samples_2010_2023.csv")

def normalize_name(name: str) -> str:
    name = (name or "").strip()
    name = " ".join(name.split())
    return name

def normalize_ticker(t: str) -> str:
    t = (t or "").strip().upper()
    # treat "nan"/"none" etc as empty if they appear from messy sources
    if t in {"", "NONE", "N/A", "NULL", "NAN"}:
        return ""
    return t

def main():
    pattern = os.path.join(INPUT_DIR, "edgar_10k_*_Q*.csv")
    files = sorted(glob.glob(pattern))
    if not files:
        raise FileNotFoundError(f"No files found matching: {pattern}")

    names_by_cik = defaultdict(set)
    tickers_by_cik = defaultdict(set)

    latest_name_by_cik = {}
    latest_seen_by_cik = {}          # (year, quarter, date_filed)
    latest_ticker_by_cik = {}        # preferred ticker aligned with latest_seen_by_cik

    total_rows = 0

    for path in files:
        base = os.path.basename(path)
        try:
            parts = base.replace(".csv", "").split("_")
            year = int(parts[2])
            qtr = int(parts[3].replace("Q", ""))
        except Exception:
            year, qtr = 0, 0

        print(f"Reading: {path}")

        with open(path, "r", encoding="utf-8", newline="") as f:
            reader = csv.DictReader(f)

            expected = {"Company Name", "CIK"}
            if not expected.issubset(set(reader.fieldnames or [])):
                raise ValueError(
                    f"{path} missing expected columns {expected}. Found: {reader.fieldnames}"
                )

            for row in reader:
                total_rows += 1

                cik = (row.get("CIK") or "").strip()
                name = normalize_name(row.get("Company Name"))
                date_filed = (row.get("Date Filed") or "").strip()
                ticker = normalize_ticker(row.get("Ticker"))

                if not cik or not name:
                    continue

                names_by_cik[cik].add(name)

                if ticker:
                    tickers_by_cik[cik].add(ticker)

                key = (year, qtr, date_filed)
                prev_key = latest_seen_by_cik.get(cik)

                if prev_key is None or key > prev_key:
                    latest_seen_by_cik[cik] = key
                    latest_name_by_cik[cik] = name
                    # only overwrite preferred ticker if this row has a non-empty one
                    if ticker:
                        latest_ticker_by_cik[cik] = ticker

    with open(OUTPUT_FILE, "w", encoding="utf-8", newline="") as out:
        writer = csv.writer(out, delimiter=";")
        writer.writerow([
            "CIK",
            "LatestCompanyName",
            "Ticker",
            "AllTickers",
            "NameChanged",
            "NameVariants",
            "AllNames"
        ])

        for cik in sorted(names_by_cik.keys()):
            all_names = sorted(names_by_cik[cik])
            name_changed = len(all_names) > 1
            latest_name = latest_name_by_cik.get(cik, all_names[0] if all_names else "")

            preferred_ticker = latest_ticker_by_cik.get(cik, "")
            all_tickers = sorted(tickers_by_cik.get(cik, set()))

            writer.writerow([
                cik,
                latest_name,
                preferred_ticker,
                " | ".join(all_tickers),
                "TRUE" if name_changed else "FALSE",
                len(all_names),
                " | ".join(all_names)
            ])

    print("\nDone.")
    print(f"Quarter files read: {len(files)}")
    print(f"Rows scanned: {total_rows}")
    print(f"Unique CIKs: {len(names_by_cik)}")
    print(f"Saved: {OUTPUT_FILE}")

if __name__ == "__main__":
    main()
