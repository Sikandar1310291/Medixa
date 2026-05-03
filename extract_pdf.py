import pdfplumber
import re

pdf_path = r"C:\Users\ma516\OneDrive\Desktop\1 . Pharma\1.media\Medixa - SQL Results.pdf"
sql_path = r"C:\Users\ma516\OneDrive\Desktop\1 . Pharma\import_final.sql"

medicines = []

# Pattern: number, then name (can include letters/symbols/spaces), then TP (number), then Retail (number)
# Example: "1 ARTECXIN FORTE TAB 224.4 264"
# The trick: TP and Retail are always numbers at the END of the line
ROW_RE = re.compile(r'^\d+\s+(.+?)\s+([\d.]+)\s+([\d.]+)\s*$')

print("Extracting text from PDF...")

with pdfplumber.open(pdf_path) as pdf:
    total_pages = len(pdf.pages)
    print(f"Total pages: {total_pages}")
    
    for page_num, page in enumerate(pdf.pages):
        text = page.extract_text()
        if not text:
            continue
        
        for line in text.splitlines():
            line = line.strip()
            if not line:
                continue
            # Skip header
            if line.startswith('TITLE'):
                continue
            
            m = ROW_RE.match(line)
            if m:
                name   = m.group(1).strip()
                tp     = m.group(2).strip()
                retail = m.group(3).strip()
                medicines.append((name, tp, retail))
        
        if (page_num + 1) % 100 == 0:
            print(f"  Page {page_num+1}/{total_pages} — {len(medicines)} medicines so far...")

print(f"\nTotal medicines extracted: {len(medicines)}")
print("First 5:")
for m in medicines[:5]:
    print(f"  {m}")
print("Last 5:")
for m in medicines[-5:]:
    print(f"  {m}")

# Write SQL
print("\nWriting SQL file...")
with open(sql_path, 'w', encoding='utf-8') as f:
    f.write("BEGIN TRANSACTION;\n")
    for name, tp, retail in medicines:
        safe = name.replace("'", "''")
        f.write(f"INSERT OR IGNORE INTO Medicines (Name, PurchasePrice, SalePrice) VALUES ('{safe}', {tp}, {retail});\n")
    f.write("COMMIT;\n")

print(f"Done! SQL file: {sql_path}")
