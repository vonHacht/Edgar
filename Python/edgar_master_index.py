import sys
import os
import time
import csv
import logging
from typing import Iterable, Tuple, Dict

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

def load_cik_ticker_map(session: requests.Session) -> Dict[str, str]:
    """
    Returns {cik (10-digit zero-padded): ticker}
    """
    url = "https://www.sec.gov/files/company_tickers.json"
    log.info("Downloading CIK → ticker map")
    r = session.get(url, timeout=30)
    r.raise_for_status()

    data = r.json()
    cik_to_ticker = {}

    for item in data.values():
        cik = str(item["cik_str"]).zfill(10)
        ticker = item.get("ticker", "").upper()
        if ticker:
            cik_to_ticker[cik] = ticker

    log.info(f"Loaded {len(cik_to_ticker):,} CIK→ticker mappings")
    return cik_to_ticker

# -----------------------
# MAIN
# -----------------------
if __name__ == "__main__":
    session = make_session()
    cik_to_ticker = load_cik_ticker_map(session)
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
                        "Ticker",
                        "CUSIP",
                        "Form Type",
                        "Date Filed",
                        "Filename"
                    ])

                    for cik, name, form, date_filed, filename in parse_master_index(lines):
                        if form not in FORMS:
                            continue

                        cik_padded = cik.zfill(10)
                        ticker = cik_to_ticker.get(cik_padded, "")

                        writer.writerow([
                            name,
                            cik_padded,
                            ticker,
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

