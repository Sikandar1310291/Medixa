import sqlite3
import os
import random

def seed_custom_batch_3():
    db_paths = ["bin/Debug/PharmaDB.sqlite", "PharmaDB.sqlite"]
    target_db = next((p for p in db_paths if os.path.exists(p)), None)
    
    if not target_db:
        print("[!] Database not found.")
        return

    conn = sqlite3.connect(target_db)
    cursor = conn.cursor()

    # Data to add (Name, Manufacturer, CategoryHint)
    data = [
        # Shrooq
        ("Shrocef", "Shrooq Pharmaceuticals", "Antibiotic"), ("Shroflox", "Shrooq Pharmaceuticals", "Antibiotic"),
        ("Shropraz", "Shrooq Pharmaceuticals", "Gastro"), ("Shrogesic", "Shrooq Pharmaceuticals", "Pain Relief"),

        # Shifa
        ("Shicef", "Shifa Laboratories", "Antibiotic"), ("Shiflox", "Shifa Laboratories", "Antibiotic"),
        ("Shipraz", "Shifa Laboratories", "Gastro"), ("Shigesic", "Shifa Laboratories", "Pain Relief"),

        # Servier
        ("Arcalion", "Servier", "Neuro"), ("Coveram", "Servier", "Cardio"),
        ("Coversyl", "Servier", "Cardio"), ("Daflon", "Servier", "Vascular"),
        ("Diamicron MR", "Servier", "Anti-Diabetic"), ("Natrilix SR", "Servier", "Cardio"),
        ("Procoralan", "Servier", "Cardio"), ("Stablon", "Servier", "Neuro"),
        ("Vastarel MR", "Servier", "Cardio"),

        # Selmore
        ("Selmectin", "Selmore Pharmaceuticals", "Veterinary"), ("Selmectin Plus", "Selmore Pharmaceuticals", "Veterinary"),
        ("Selmox LA", "Selmore Pharmaceuticals", "Veterinary"), ("Selvit-E", "Selmore Pharmaceuticals", "Veterinary"),

        # Bayer / Schering
        ("Xarelto", "Bayer Pakistan", "Cardio"), ("Yasmin", "Bayer Pakistan", "Gynae"),
        ("Yaz", "Bayer Pakistan", "Gynae"), ("Diane-35", "Bayer Pakistan", "Gynae"),
        ("Progynova", "Bayer Pakistan", "Gynae"), ("Nebido", "Bayer Pakistan", "Gynae"),
        ("Testoviron Depot", "Bayer Pakistan", "Gynae"), ("Androcur", "Bayer Pakistan", "Gynae"),

        # Schazoo
        ("Schacef", "Schazoo Laboratories", "Antibiotic"), ("Schaflox", "Schazoo Laboratories", "Antibiotic"),
        ("Schapraz", "Schazoo Laboratories", "Gastro"), ("Schagesic", "Schazoo Laboratories", "Pain Relief"),
        ("Schaclav", "Schazoo Laboratories", "Antibiotic"),

        # Sami (Specific list)
        ("Samicef", "Sami Pharmaceuticals", "Antibiotic"), ("Samizole", "Sami Pharmaceuticals", "Gastro"),
        ("Samipraz", "Sami Pharmaceuticals", "Gastro"), ("Samiflox", "Sami Pharmaceuticals", "Antibiotic"),
        ("Samizith", "Sami Pharmaceuticals", "Antibiotic"), ("Sami-Clav", "Sami Pharmaceuticals", "Antibiotic"),

        # Getz Pharma
        ("Risek", "Getz Pharma", "Gastro"), ("Getryl", "Getz Pharma", "Anti-Diabetic"),
        ("Zest", "Getz Pharma", "Cardio"), ("Lipiget", "Getz Pharma", "Cardio"),
        ("Exforge", "Getz Pharma", "Cardio"), ("Montika", "Getz Pharma", "Respiratory"),
        ("Zetron", "Getz Pharma", "Antibiotic"), ("Tresjow", "Getz Pharma", "Anti-Diabetic"),
        ("Rovista", "Getz Pharma", "Cardio"), ("Leflox", "Getz Pharma", "Antibiotic"),
        ("Febget", "Getz Pharma", "General"), ("Efomet", "Getz Pharma", "Anti-Diabetic"),
        ("Sitaget", "Getz Pharma", "Anti-Diabetic"), ("Vildaget", "Getz Pharma", "Anti-Diabetic")
    ]

    # Bulk add standard prefix brands for the remaining listed companies
    prefixes = {
        "Scharper Pharmaceuticals": "Schar",
        "Sapient Pharma": "Sap",
        "Sanital Pharmaceuticals": "Sani",
        "Renacon Pharma": "Rena",
        "Reko Pharmacal": "Reko",
        "Rasco Pharma": "Ras",
        "Raazee Therapeutics": "Raaz",
        "Prime Laboratories": "Pri"
    }
    
    suffixes = [
        ("cef", "Antibiotic"), ("flox", "Antibiotic"), ("mox", "Antibiotic"),
        ("praz", "Gastro"), ("zole", "Gastro"), ("met", "Anti-Diabetic"),
        ("gesic", "Pain Relief"), ("cid", "Gastro"), ("cal-D", "Supplement"),
        ("vit", "Supplement"), ("fer", "Supplement"), ("sone", "General"),
        ("derm", "Skin Care"), ("spas", "General"), ("vas", "Cardio"),
        ("ace", "Cardio"), ("card", "Cardio"), ("gel", "Skin Care")
    ]

    for company, prefix in prefixes.items():
        for suffix, cat in suffixes:
            name = f"{prefix}{suffix}"
            data.append((name, company, cat))

    for name, mfr, cat in data:
        form = "Tab"
        unit = "Strip"
        lower_name = name.lower()
        if any(x in lower_name for x in ["cream", "gel", "derm", "ointment"]):
            form = "Cream"; unit = "Tube"
        elif any(x in lower_name for x in ["syrup", "susp", "syp", "wash", "lotion", "sol"]):
            form = "Syp"; unit = "Bottle"
        elif "inj" in lower_name:
            form = "Inj"; unit = "Vial"
        elif "cap" in lower_name or "enflor" in lower_name or "risek" in lower_name:
            form = "Cap"; unit = "Strip"

        pp = random.choice([55, 85, 125, 210, 320, 550])
        sp = int(pp * 1.25)
        
        cursor.execute("""
            INSERT INTO Medicines (Name, Type, Category, Manufacturer, Unit, PurchasePrice, SalePrice, Status)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """, (name, form, cat, mfr, unit, pp, sp, 'Active'))

    conn.commit()
    conn.close()
    print(f"[OK] Successfully added {len(data)} batch 3 medicines.")

if __name__ == "__main__":
    seed_custom_batch_3()
