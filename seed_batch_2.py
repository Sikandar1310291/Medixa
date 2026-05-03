import sqlite3
import os
import random

def seed_custom_batch_2():
    db_paths = ["bin/Debug/PharmaDB.sqlite", "PharmaDB.sqlite"]
    target_db = next((p for p in db_paths if os.path.exists(p)), None)
    
    if not target_db:
        print("[!] Database not found.")
        return

    conn = sqlite3.connect(target_db)
    cursor = conn.cursor()

    # Data to add (Name, Manufacturer, CategoryHint)
    data = [
        # Nutrifactor
        ("Nutra-C Plus", "Nutrifactor Laboratories", "Vitamin"),
        ("Vitamax Women", "Nutrifactor Laboratories", "Multivitamin"),
        ("Vitamax One A Day Multi", "Nutrifactor Laboratories", "Multivitamin"),
        ("Ginsden", "Nutrifactor Laboratories", "Herbal"),
        ("Glintest", "Nutrifactor Laboratories", "Herbal"),
        ("Gluta Glanza", "Nutrifactor Laboratories", "Skin Care"),
        ("Derma Verm", "Nutrifactor Laboratories", "Skin Care"),
        ("Biotin Plus", "Nutrifactor Laboratories", "Supplement"),
        ("Duramaze", "Nutrifactor Laboratories", "General"),
        ("Nuroton", "Nutrifactor Laboratories", "Neuro"),
        ("Sleep Well", "Nutrifactor Laboratories", "Supplement"),
        ("Jointin-D", "Nutrifactor Laboratories", "Bone Care"),
        ("Bonex-D", "Nutrifactor Laboratories", "Bone Care"),
        ("Fero", "Nutrifactor Laboratories", "Supplement"),
        ("Trylow", "Nutrifactor Laboratories", "Supplement"),
        ("Bio Grow", "Nutrifactor Laboratories", "Supplement"),
        ("Gencell", "Nutrifactor Laboratories", "Supplement"),
        ("Trifactor", "Nutrifactor Laboratories", "Supplement"),
        ("Nuception", "Nutrifactor Laboratories", "Reproductive"),
        ("Lipidex", "Nutrifactor Laboratories", "General"),
        ("Lean-Plus", "Nutrifactor Laboratories", "Supplement"),
        ("Nucal-Z", "Nutrifactor Laboratories", "Supplement"),
        ("Nutra-X", "Nutrifactor Laboratories", "Supplement"),
        ("Ginkgo Biloba", "Nutrifactor Laboratories", "Herbal"),
        ("Mag-250", "Nutrifactor Laboratories", "Supplement"),
        ("B-50 Complex", "Nutrifactor Laboratories", "Vitamin"),
        ("Zinc Cell", "Nutrifactor Laboratories", "Supplement"),
        ("L-Arginine", "Nutrifactor Laboratories", "Supplement"),
        ("Cranflo", "Nutrifactor Laboratories", "Herbal"),
        ("Lecithin", "Nutrifactor Laboratories", "Supplement"),
        ("Omega-3 Fish Oil", "Nutrifactor Laboratories", "Supplement"),
        ("Nutriflax", "Nutrifactor Laboratories", "Supplement"),
        ("Vitasel", "Nutrifactor Laboratories", "Supplement"),
        ("Parsley", "Nutrifactor Laboratories", "Herbal"),
        ("Saffron", "Nutrifactor Laboratories", "Herbal"),
        ("Garlic", "Nutrifactor Laboratories", "Herbal"),
        ("Heptaclean", "Nutrifactor Laboratories", "Herbal"),
        ("Nephroclean", "Nutrifactor Laboratories", "Herbal"),
        ("Gas-Away", "Nutrifactor Laboratories", "Herbal"),
        ("Apple Cider Vinegar Gummies", "Nutrifactor Laboratories", "Supplement"),
        ("Milk Thistle", "Nutrifactor Laboratories", "Herbal"),

        # Ameer Labs
        ("Amlyp", "Ameer Labs", "Cardio"),
        ("Amlyp-M", "Ameer Labs", "Cardio"),
        ("Amlosa", "Ameer Labs", "Cardio"),
        ("Amlosa-H", "Ameer Labs", "Cardio"),
        ("Amrit", "Ameer Labs", "General"),
        ("Amset", "Ameer Labs", "General"),
        ("Amvas", "Ameer Labs", "Cardio"),
        ("Amzide", "Ameer Labs", "General"),
        ("Angipro", "Ameer Labs", "Cardio"),
        ("Atrol", "Ameer Labs", "General"),
        ("Avastin", "Ameer Labs", "Cardio"),
        ("Cardivas", "Ameer Labs", "Cardio"),
        ("Lipid Plus", "Ameer Labs", "Cardio"),
        ("Rosul", "Ameer Labs", "Cardio"),
        ("Vastin", "Ameer Labs", "Cardio"),

        # YSV Group
        ("YSV-M", "YSV Group", "Multivitamin"),
        ("YSV-D3", "YSV Group", "Vitamin"),
        ("YSV-C", "YSV Group", "Vitamin"),
        ("YSV-Zinc", "YSV Group", "Supplement"),
        ("YSV-Cal", "YSV Group", "Supplement"),
        ("YSV-Iron", "YSV Group", "Supplement"),
        ("YSV-Ginseng", "YSV Group", "Herbal"),
        ("YSV-Omega", "YSV Group", "Supplement"),
        ("YSV-B Complex", "YSV Group", "Vitamin"),
        ("YSV-Multivitamin", "YSV Group", "Multivitamin"),
        ("YSV-Ginkgo", "YSV Group", "Herbal"),
        ("YSV-Moringa", "YSV Group", "Herbal"),
        ("YSV-Garlic", "YSV Group", "Herbal"),
        ("YSV-Ashwagandha", "YSV Group", "Herbal"),
        ("YSV-Magnesium", "YSV Group", "Supplement"),
        ("YSV-Folic", "YSV Group", "Supplement"),

        # Martin Dow
        ("Glucophage Regular", "Martin Dow", "Anti-Diabetic"),
        ("Glucophage XR", "Martin Dow", "Anti-Diabetic"),
        ("Evion", "Martin Dow", "Vitamin"),
        ("Sangobion", "Martin Dow", "Supplement"),
        ("Neurobion", "Martin Dow", "Neuro"),
        ("Concor", "Martin Dow", "Cardio"),
        ("Nuberol", "Martin Dow", "Pain Relief"),
        ("Nuberol Forte", "Martin Dow", "Pain Relief"),
        ("Buscopan Plus", "Martin Dow", "Stomach"),
        ("Klaribact", "Martin Dow", "Antibiotic"),
        ("Infexin", "Martin Dow", "Antibiotic"),
        ("Eziday", "Martin Dow", "Cardio"),
        ("Lipirex", "Martin Dow", "Cardio"),
        ("Synflex", "Martin Dow", "Pain Relief"),
        ("Rozanto", "Martin Dow", "Antibiotic"),
        ("Enflor Capsules", "Martin Dow", "General"),
        ("Enflor Sachet", "Martin Dow", "General"),
        ("Laxoberon", "Martin Dow", "General"),
        ("Gonal-F", "Martin Dow", "General"),
        ("Empaphage", "Martin Dow", "Anti-Diabetic"),
        ("Sitaphage", "Martin Dow", "Anti-Diabetic"),
        ("Vastose", "Martin Dow", "Cardio"),
        ("Anexate", "Martin Dow", "General"),
        ("Bon-One", "Martin Dow", "General"),
        ("Bonviva", "Martin Dow", "General"),
        ("Dilatrend", "Martin Dow", "Cardio"),
        ("Dormicum", "Martin Dow", "General"),
        ("Esomax", "Martin Dow", "Gastro"),
        ("Fansidar", "Martin Dow", "General"),
        ("Fusiderm", "Martin Dow", "General"),
        ("Regro", "Martin Dow", "General"),
        ("Wintogeno", "Martin Dow", "General"),
        ("Qalsan", "Martin Dow", "Supplement"),
        ("Advit-D", "Martin Dow", "Vitamin"),
        ("Cosome Lozenges", "Martin Dow", "Cough"),
        ("Indibro-M", "Martin Dow", "General"),
        ("Vonospire", "Martin Dow", "General"),
        ("Tixomer", "Martin Dow", "General"),

        # Wimits
        ("Wintog", "Wimits Pharmaceuticals", "General"),
        ("Wintog Forte", "Wimits Pharmaceuticals", "General"),
        ("Winpraz", "Wimits Pharmaceuticals", "Gastro"),
        ("Wincef", "Wimits Pharmaceuticals", "Antibiotic"),
        ("Winlevo", "Wimits Pharmaceuticals", "Antibiotic"),
        ("Winace", "Wimits Pharmaceuticals", "General"),
        ("Wincard", "Wimits Pharmaceuticals", "Cardio"),
        ("Winfer", "Wimits Pharmaceuticals", "Supplement"),
        ("Winglim", "Wimits Pharmaceuticals", "Anti-Diabetic"),
        ("Winmet", "Wimits Pharmaceuticals", "Anti-Diabetic"),
        ("Winsone", "Wimits Pharmaceuticals", "General"),
        ("Winspas", "Wimits Pharmaceuticals", "General"),

        # Zaco Pharma
        ("Zacogit", "Zaco Pharma", "General"),
        ("Zacovit", "Zaco Pharma", "Vitamin"),
        ("Zacocal-D", "Zaco Pharma", "Supplement"),
        ("Zacofer", "Zaco Pharma", "Supplement"),
        ("Zacomox", "Zaco Pharma", "Antibiotic"),
        ("Zacef", "Zaco Pharma", "Antibiotic"),
        ("Zacoflox", "Zaco Pharma", "Antibiotic"),
        ("Zacogesic", "Zaco Pharma", "Pain Relief"),

        # Xenon
        ("Xencef", "Xenon Pharmaceuticals", "Antibiotic"),
        ("Xenoflox", "Xenon Pharmaceuticals", "Antibiotic"),
        ("Xenon-P", "Xenon Pharmaceuticals", "Antibiotic"),
        ("Xenid", "Xenon Pharmaceuticals", "General"),
        ("Xenim Plus", "Xenon Pharmaceuticals", "Pain Relief"),
        ("Xenogesic", "Xenon Pharmaceuticals", "Pain Relief"),
        ("Xenodol", "Xenon Pharmaceuticals", "Pain Relief"),
        ("Xenomet", "Xenon Pharmaceuticals", "Anti-Diabetic"),
        ("Xenozole", "Xenon Pharmaceuticals", "Gastro"),

        # Wilshire
        ("Wilcef", "Wilshire Laboratories", "Antibiotic"),
        ("Wilfloxin", "Wilshire Laboratories", "Antibiotic"),
        ("Wilsam", "Wilshire Laboratories", "General"),
        ("Wilcox", "Wilshire Laboratories", "General"),
        ("Wilgesic", "Wilshire Laboratories", "Pain Relief"),
        ("Wilpred", "Wilshire Laboratories", "General"),
        ("Wilmox", "Wilshire Laboratories", "Antibiotic"),

        # Unipharma
        ("Unibro", "Unipharma", "General"),
        ("Unicef", "Unipharma", "Antibiotic"),
        ("Uniflox", "Unipharma", "Antibiotic"),
        ("Unimox", "Unipharma", "Antibiotic"),
        ("Unigesic", "Unipharma", "Pain Relief"),

        # Unexolabs
        ("Unexon", "Unexolabs", "General"),
        ("Unexocal-D", "Unexolabs", "Supplement"),
        ("Unexofer", "Unexolabs", "Supplement"),
        ("Unexomox", "Unexolabs", "Antibiotic"),

        # Tariq Haider RG
        ("T-Cef", "RG Pharmaceutica", "Antibiotic"),
        ("T-Flox", "RG Pharmaceutica", "Antibiotic"),
        ("T-Mox", "RG Pharmaceutica", "Antibiotic"),
        ("T-Praz", "RG Pharmaceutica", "Gastro"),
        ("T-Zole", "RG Pharmaceutica", "Gastro"),
        ("T-Met", "RG Pharmaceutica", "Anti-Diabetic"),
        ("T-Gesic", "RG Pharmaceutica", "Pain Relief"),

        # Tagma
        ("Tagcef", "Tagma Pharma", "Antibiotic"),
        ("Tagflox", "Tagma Pharma", "Antibiotic"),
        ("Tagmox", "Tagma Pharma", "Antibiotic"),
        ("Tagpraz", "Tagma Pharma", "Gastro"),
        ("Tagmet", "Tagma Pharma", "Anti-Diabetic"),

        # Synchro
        ("Sencef", "Synchro Pharmaceuticals", "Antibiotic"),
        ("Senflox", "Synchro Pharmaceuticals", "Antibiotic"),
        ("Senpraz", "Synchro Pharmaceuticals", "Gastro"),
        ("Senmet", "Synchro Pharmaceuticals", "Anti-Diabetic")
    ]

    for name, mfr, cat in data:
        # Determine form and unit automatically
        form = "Tab"
        unit = "Strip"
        
        lower_name = name.lower()
        if "cream" in lower_name or "ointment" in lower_name or "gel" in lower_name or "oint" in lower_name or "derm" in lower_name:
            form = "Cream"
            unit = "Tube"
        elif "syrup" in lower_name or "susp" in lower_name or "syp" in lower_name or "wash" in lower_name or "lotion" in lower_name:
            form = "Syp"
            unit = "Bottle"
        elif "soap" in lower_name or "bar" in lower_name:
            form = "Soap"
            unit = "Piece"
        elif "drop" in lower_name or "spray" in lower_name or "vial" in lower_name:
            form = "Drop"
            unit = "Bottle"
        elif "powder" in lower_name or "sachet" in lower_name or "food" in lower_name:
            form = "Powder"
            unit = "Pack"
        elif "inj" in lower_name:
            form = "Inj"
            unit = "Vial"
        elif "cap" in lower_name or "cap " in lower_name or name.endswith("Cap") or "enflor" in lower_name:
            form = "Cap"
            unit = "Strip"

        # Pricing logic
        pp = random.choice([40, 65, 85, 120, 180, 260, 350, 520])
        sp = int(pp * 1.22)
        
        cursor.execute("""
            INSERT INTO Medicines (Name, Type, Category, Manufacturer, Unit, PurchasePrice, SalePrice, Status)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """, (name, form, cat, mfr, unit, pp, sp, 'Active'))

    conn.commit()
    conn.close()
    print(f"[OK] Successfully added {len(data)} batch 2 medicines to {target_db}.")

if __name__ == "__main__":
    seed_custom_batch_2()
