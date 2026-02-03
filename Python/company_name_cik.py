import os
import csv
import glob
from collections import defaultdict

INPUT_DIR = "output"
OUTPUT_FILE = os.path.join(INPUT_DIR, "samples_2010_2023.csv")

def normalize_name(name: str) -> str:
    # Light normalization to reduce false "changes"
    # (you can make this stricter/looser depending on your needs)
    name = name.strip()
    name = " ".join(name.split())
    return name

def main():
    pattern = os.path.join(INPUT_DIR, "edgar_10k_*_Q*.csv")
    files = sorted(glob.glob(pattern))
    if not files:
        raise FileNotFoundError(f"No files found matching: {pattern}")

    # Track all names per CIK + also keep the most recently seen name
    names_by_cik = defaultdict(set)
    latest_name_by_cik = {}
    latest_seen_by_cik = {}  # store sortable key like (year, quarter, datefiled) if available

    total_rows = 0

    for path in files:
        # Try to infer year/quarter from filename for "latest" resolution
        # expected: edgar_10k_2010_Q1.csv
        base = os.path.basename(path)
        year = None
        qtr = None
        try:
            parts = base.replace(".csv", "").split("_")
            year = int(parts[2])
            qtr = int(parts[3].replace("Q", ""))
        except Exception:
            year, qtr = 0, 0  # fallback

        print(f"Reading: {path}")

        with open(path, "r", encoding="utf-8", newline="") as f:
            reader = csv.DictReader(f)

            # Adjust these if your headers differ
            expected = {"Company Name", "CIK"}
            if not expected.issubset(set(reader.fieldnames or [])):
                raise ValueError(
                    f"{path} missing expected columns {expected}. Found: {reader.fieldnames}"
                )

            for row in reader:
                total_rows += 1

                cik = (row.get("CIK") or "").strip()
                name = normalize_name(row.get("Company Name") or "")
                date_filed = (row.get("Date Filed") or "").strip()  # optional

                if not cik or not name:
                    continue

                names_by_cik[cik].add(name)

                # Decide what "latest" means: prefer later filing date if present,
                # otherwise just later quarter file.
                # We build a sortable key.
                key = (year, qtr, date_filed)
                prev_key = latest_seen_by_cik.get(cik)

                if prev_key is None or key > prev_key:
                    latest_seen_by_cik[cik] = key
                    latest_name_by_cik[cik] = name

    # Write summary
    with open(OUTPUT_FILE, "w", encoding="utf-8", newline="") as out:
        writer = csv.writer(out, delimiter=";")
        writer.writerow([
            "CIK",
            "LatestCompanyName",
            "NameChanged",
            "NameVariants",
            "AllNames"
        ])

        for cik in sorted(names_by_cik.keys()):
            all_names = sorted(names_by_cik[cik])
            name_changed = len(all_names) > 1
            latest_name = latest_name_by_cik.get(cik, all_names[0] if all_names else "")

            writer.writerow([
                cik,
                latest_name,
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


