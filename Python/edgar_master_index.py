import sys
import os
import requests
import time
import csv
import logs
import logging

# -----------------------
# LOGS
# -----------------------
logs.init_output_formatters(
    'normal',
    stderr=sys.stderr,
    debug_logfile='./edgar_master_index.log'
)
log = logging.getLogger('edgar_master_index')
log.info('logging enabled')

# -----------------------
# CONFIG
# -----------------------
START_YEAR = 2010
END_YEAR = 2023
BASE_URL = "https://www.sec.gov/Archives/edgar/full-index"
HEADERS = {
    "User-Agent": "Your Name your.email@domain.com"  # REQUIRED by SEC
}

OUTPUT_DIR = "output"
os.makedirs(OUTPUT_DIR, exist_ok=True)

# -----------------------
# HELPERS
# -----------------------
def download_master_index(year, quarter):
    url = f"{BASE_URL}/{year}/QTR{quarter}/master.idx"
    log.info(f"Downloading {url}")
    response = requests.get(url, headers=HEADERS, timeout=60)
    response.raise_for_status()
    return response.text.splitlines()

def parse_master_index(lines):
    """
    Skips header and yields parsed rows (cik, name, form, date_filed, filename)
    """
    start = False
    for line in lines:
        # More robust than startswith, in case spacing changes
        if "CIK|Company Name|Form Type|Date Filed|Filename" in line:
            start = True
            continue
        if not start:
            continue

        line = line.strip()
        if not line:
            continue

        parts = line.split("|")
        if len(parts) != 5:
            continue

        cik, name, form, date_filed, filename = (p.strip() for p in parts)
        yield cik, name, form, date_filed, filename

def write_quarter_file(outputfile, rows):
    """
    Writes one CSV for the quarter. Expects rows as iterable of tuples:
    (companyname, cik, formtype, datefiled, filename)
    """
    with open(outputfile, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["Company Name", "CIK", "Form Type", "Date Filed", "Filename"])
        writer.writerows(rows)

# -----------------------
# MAIN
# -----------------------
if __name__ == "__main__":
    for year in range(START_YEAR, END_YEAR + 1):
        for quarter in range(1, 5):
            log.info(f"Processing {year} Q{quarter}...")

            try:
                lines = download_master_index(year, quarter)

                out_path = os.path.join(OUTPUT_DIR, f"edgar_10k_{year}_Q{quarter}.csv")

                matched_rows = []
                for cik, name, form, date_filed, filename in parse_master_index(lines):
                    if form in {"10-K", "10-K405"}:
                        matched_rows.append((
                            name,
                            cik.zfill(10),
                            form,
                            date_filed,
                            filename
                        ))

                write_quarter_file(out_path, matched_rows)
                log.info(f"Wrote {len(matched_rows)} rows to {out_path}")

                time.sleep(0.2)

            except Exception:
                log.exception(f"ERROR {year} Q{quarter}")
                raise
