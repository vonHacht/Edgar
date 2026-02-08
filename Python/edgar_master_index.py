import sys
import os
import time
import csv
import re
import logging
from typing import Iterable, Tuple, Dict, Optional

import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

import logs

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
    # Put a real UA per SEC guidance: include name + email
    "User-Agent": "Your Name your.email@domain.com",
    "Accept-Encoding": "gzip, deflate",
}

OUTPUT_DIR = "output"
os.makedirs(OUTPUT_DIR, exist_ok=True)

FORMS = {"10-K", "10-K405"}

# Polite pacing: keep modest. If you hit 429s, increase.
REQUEST_DELAY_SECONDS = 0.12
# Only scan top of filing (CUSIP is usually in SEC header)
FILING_SCAN_CHARS = 15000

PRINT_EVERY_N_FILINGS = 100   # how often to print progress
# -----------------------
# HTTP SESSION (reused connections + retries)
# -----------------------
def make_session() -> requests.Session:
    session = requests.Session()
    session.headers.update(HEADERS)

    retry = Retry(
        total=6,
        backoff_factor=0.8,
        status_forcelist=(429, 500, 502, 503, 504),
        allowed_methods=("GET",),
        raise_on_status=False,
        respect_retry_after_header=True,
    )
    adapter = HTTPAdapter(max_retries=retry, pool_connections=20, pool_maxsize=20)
    session.mount("https://", adapter)
    session.mount("http://", adapter)
    return session

# -----------------------
# HELPERS
# -----------------------
def download_master_index(session: requests.Session, year: int, quarter: int) -> list[str]:
    url = f"{BASE_URL}/{year}/QTR{quarter}/master.idx"
    log.info(f"Downloading {url}")
    r = session.get(url, timeout=60)
    r.raise_for_status()
    return r.text.splitlines()

def parse_master_index(lines: list[str]) -> Iterable[Tuple[str, str, str, str, str]]:
    """
    Yields (cik, name, form, date_filed, filename)
    """
    start = False
    for line in lines:
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

# -----------------------
# CUSIP
# -----------------------
# More robust: allow punctuation/colon and weird spacing. Capture 8–9 alnum.
CUSIP_REGEX = re.compile(r"\bCUSIP\b[^0-9A-Z]{0,10}([0-9A-Z]{8,9})")

def extract_cusip_from_filing(
    session: requests.Session,
    filename: str,
    cache: Dict[str, str],
    last_request_time: list[float],
) -> str:
    """
    Downloads filing text (once) and extracts CUSIP from header chunk.
    Uses cache by filename to avoid repeated requests.
    """
    if filename in cache:
        return cache[filename]

    # Throttle
    now = time.time()
    elapsed = now - last_request_time[0]
    if elapsed < REQUEST_DELAY_SECONDS:
        time.sleep(REQUEST_DELAY_SECONDS - elapsed)

    filing_url = f"https://www.sec.gov/Archives/{filename}"
    try:
        r = session.get(filing_url, timeout=60)

        # If still rate-limited after retries, handle gently
        if r.status_code == 429:
            retry_after = r.headers.get("Retry-After")
            sleep_s = float(retry_after) if retry_after and retry_after.isdigit() else 2.0
            log.warning(f"429 rate limited. Sleeping {sleep_s:.1f}s: {filing_url}")
            time.sleep(sleep_s)
            r = session.get(filing_url, timeout=60)

        r.raise_for_status()
        #text = r.text[:FILING_SCAN_CHARS]

        text = r.text

        m = CUSIP_REGEX.search(text)
        cusip = m.group(1) if m else ""
        cache[filename] = cusip
        return cusip

    except Exception as e:
        log.warning(f"Failed CUSIP for {filename}: {e}")
        cache[filename] = ""
        return ""
    finally:
        last_request_time[0] = time.time()

# -----------------------
# MAIN
# -----------------------
if __name__ == "__main__":
    session = make_session()
    cusip_cache: Dict[str, str] = {}
    last_request_time = [0.0]

    print("🚀 Starting EDGAR 10-K CUSIP extraction")
    print(f"📅 Years: {START_YEAR}–{END_YEAR}")
    print("-" * 60)

    for year in range(START_YEAR, END_YEAR + 1):
        for quarter in range(1, 5):
            print(f"\n📂 Processing {year} Q{quarter}...")
            log.info(f"Processing {year} Q{quarter}...")

            out_path = os.path.join(OUTPUT_DIR, f"edgar_10k_{year}_Q{quarter}.csv")

            try:
                print("  ⬇️  Downloading master.idx ...")
                lines = download_master_index(session, year, quarter)
                print(f"  ✅ master.idx loaded ({len(lines):,} lines)")

                wrote = 0
                with open(out_path, "w", newline="", encoding="utf-8") as f:
                    writer = csv.writer(f)
                    writer.writerow([
                        "Company Name",
                        "CIK",
                        "CUSIP",
                        "Form Type",
                        "Date Filed",
                        "Filename"
                    ])

                    for cik, name, form, date_filed, filename in parse_master_index(lines):
                        if form not in FORMS:
                            continue

                        # Does not work
                        #cusip = extract_cusip_from_filing(
                        #    session=session,
                        #    filename=filename,
                        #    cache=cusip_cache,
                        #    last_request_time=last_request_time,
                        #)

                        writer.writerow([
                            name,
                            cik.zfill(10),
                            "",
                            form,
                            date_filed,
                            filename
                        ])

                        wrote += 1

                        if wrote % PRINT_EVERY_N_FILINGS == 0:
                            print(
                                f"  🧾 {wrote:,} filings processed "
                           #     f"(CUSIP cache size: {len(cusip_cache):,})"
                            )

                print(f"  ✅ Finished {year} Q{quarter}: {wrote:,} 10-Ks written")
                log.info(f"Wrote {wrote} rows to {out_path}")

                time.sleep(0.2)

            except Exception:
                print(f"  ❌ ERROR in {year} Q{quarter} — see log")
                log.exception(f"ERROR {year} Q{quarter}")
                raise

    print("\n🎉 All done!")

