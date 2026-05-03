import sqlite3
import os
import random

def seed_custom_list():
    db_paths = ["bin/Debug/PharmaDB.sqlite", "PharmaDB.sqlite"]
    target_db = next((p for p in db_paths if os.path.exists(p)), None)
    
    if not target_db:
        print("[!] Database not found.")
        return

    conn = sqlite3.connect(target_db)
    cursor = conn.cursor()

    # Data to add (Name, Manufacturer, CategoryHint)
    data = [
        # Trans Asian Pharma
        ("Cosmelan Brightening Cream", "Trans Asian Pharma", "Skin Care"),
        ("Cosmelan Plus Advanced Rejuvenating Cream", "Trans Asian Pharma", "Skin Care"),
        ("Cosmelan Brightening Serum", "Trans Asian Pharma", "Skin Care"),
        ("Cosmelan Brightening Facewash", "Trans Asian Pharma", "Skin Care"),
        ("Cosmelan Soap", "Trans Asian Pharma", "Skin Care"),
        ("Gluage Advanced Anti-Aging Cream", "Trans Asian Pharma", "Skin Care"),
        ("Gluage Age Defying Serum", "Trans Asian Pharma", "Skin Care"),
        ("Gluage Re-radiance Creamy Facewash", "Trans Asian Pharma", "Skin Care"),
        ("Gluage Glutathione 500mg Tablets", "Trans Asian Pharma", "Supplement"),
        ("Acnefon Anti-Acne Serum", "Trans Asian Pharma", "Skin Care"),
        ("Acnefon Anti-Acne Facewash", "Trans Asian Pharma", "Skin Care"),
        ("Niacinic Pore Minimizing Serum", "Trans Asian Pharma", "Skin Care"),
        ("Borago Ointment", "Trans Asian Pharma", "Skin Care"),
        ("Borago Sensitive Moisturizer", "Trans Asian Pharma", "Skin Care"),
        ("Borago Oil-Free Moisturizer", "Trans Asian Pharma", "Skin Care"),
        ("Borago Oily Moisturizer", "Trans Asian Pharma", "Skin Care"),
        ("Borago Moisturizing Wash", "Trans Asian Pharma", "Skin Care"),
        ("Ezone Sun Guard SPF100", "Trans Asian Pharma", "Sun Protection"),
        ("Solar 60 Sun Guard SPF60", "Trans Asian Pharma", "Sun Protection"),
        ("Aquanil Spray", "Trans Asian Pharma", "Specialty"),
        ("Vitasia Tablets", "Trans Asian Pharma", "Supplement"),
        ("GLA-25 Bar", "Trans Asian Pharma", "Soap"),
        ("Mykos Haut Bar", "Trans Asian Pharma", "Soap"),

        # Bysel Pharma
        ("Salapil", "Bysel Pharma", "Skin Care"),
        ("Salapil Glow", "Bysel Pharma", "Skin Care"),
        ("Sensilux", "Bysel Pharma", "Skin Care"),
        ("Eterniq", "Bysel Pharma", "Skin Care"),
        ("Bysel-C", "Bysel Pharma", "Supplement"),
        ("Bysel-N", "Bysel Pharma", "Supplement"),
        ("Bysel-G", "Bysel Pharma", "Supplement"),
        ("Bysel Zinc", "Bysel Pharma", "Supplement"),
        ("Bysel Multivitamin", "Bysel Pharma", "Supplement"),
        ("Bysel Iron", "Bysel Pharma", "Supplement"),
        ("Bysel-D", "Bysel Pharma", "Supplement"),
        ("Bysel Calcium", "Bysel Pharma", "Supplement"),
        ("Bysel Sunblock", "Bysel Pharma", "Sun Protection"),
        ("Bysel Moisturizing Lotion", "Bysel Pharma", "Skin Care"),

        # Hunoir Pharmaceuticals
        ("Zitnil Facewash", "Hunoir Pharmaceuticals", "Skin Care"),
        ("Zitnil Serum", "Hunoir Pharmaceuticals", "Skin Care"),
        ("Zitnil Soap", "Hunoir Pharmaceuticals", "Skin Care"),
        ("Glunoir Brightening", "Hunoir Pharmaceuticals", "Skin Care"),
        ("Glunoir-C", "Hunoir Pharmaceuticals", "Skin Care"),
        ("Melanoir", "Hunoir Pharmaceuticals", "Skin Care"),
        ("Niacinoir", "Hunoir Pharmaceuticals", "Skin Care"),
        ("Hydranoir", "Hunoir Pharmaceuticals", "Skin Care"),
        ("Revinoir", "Hunoir Pharmaceuticals", "Skin Care"),
        ("Hunoir Sunblock SPF 60", "Hunoir Pharmaceuticals", "Sun Protection"),
        ("Hunoir Sunblock SPF 100", "Hunoir Pharmaceuticals", "Sun Protection"),
        ("U-Noir", "Hunoir Pharmaceuticals", "Skin Care"),
        ("H-Noir", "Hunoir Pharmaceuticals", "Skin Care"),
        ("VGNOIR Intimate Wash", "Hunoir Pharmaceuticals", "Specialty"),
        ("Hunoir Vitamin C Serum", "Hunoir Pharmaceuticals", "Skin Care"),

        # Chemlabshop-online
        ("A-PVP", "Chemlabshop-online", "Specialty"),
        ("2-FDCK", "Chemlabshop-online", "Specialty"),
        ("3-MMC", "Chemlabshop-online", "Specialty"),
        ("4-MMC (Mephedrone)", "Chemlabshop-online", "Specialty"),
        ("5-MeO-DMT", "Chemlabshop-online", "Specialty"),
        ("Alprazolam Powder", "Chemlabshop-online", "Controlled"),
        ("Bromazolam", "Chemlabshop-online", "Specialty"),
        ("Clonazolam", "Chemlabshop-online", "Specialty"),
        ("Crystal Meth", "Chemlabshop-online", "Narcotics"),
        ("Etizolam Powder", "Chemlabshop-online", "Specialty"),
        ("Fentanyl Powder", "Chemlabshop-online", "Narcotics"),
        ("Ketamine Crystal", "Chemlabshop-online", "Narcotics"),
        ("MDMA Crystal", "Chemlabshop-online", "Narcotics"),
        ("O-PCE", "Chemlabshop-online", "Specialty"),
        ("Oxycodone Powder", "Chemlabshop-online", "Narcotics"),
        ("Nembutal", "Chemlabshop-online", "Narcotics"),

        # NMP (New Mahmood Pharmacy)
        ("Apranax", "Nmp Pharmacy", "Pain Relief"),
        ("Arsio L Arginine", "Nmp Pharmacy", "Supplement"),
        ("Cosome Lozenges", "Nmp Pharmacy", "Cough"),
        ("Dr Koff Lozenges", "Nmp Pharmacy", "Cough"),
        ("Gluta Max", "Nmp Pharmacy", "Skin Care"),
        ("Metadetox", "Nmp Pharmacy", "Supplement"),
        ("Nexum", "Nmp Pharmacy", "Gastro"),
        ("Peridots", "Nmp Pharmacy", "Supplement"),
        ("Prezoom", "Nmp Pharmacy", "General"),
        ("Remesol", "Nmp Pharmacy", "General"),
        ("Rexomen", "Nmp Pharmacy", "General"),
        ("Sunny D", "Nmp Pharmacy", "Vitamin"),
        ("Synflex", "Nmp Pharmacy", "Pain Relief"),
        ("A Oxi Formula", "Nmp Pharmacy", "Supplement"),
        ("Glucobex", "Nmp Pharmacy", "General"),
        ("Herbi C", "Nmp Pharmacy", "Vitamin"),
        ("Herbifactor", "Nmp Pharmacy", "Vitamin"),
        ("Profil", "Nmp Pharmacy", "General"),
        ("White Vit", "Nmp Pharmacy", "Supplement"),
        ("Mufasil Owaisy Plus", "Nmp Pharmacy", "Bone Support"),
        ("Mevital-M", "Nmp Pharmacy", "Multivitamin"),

        # Zam Zam Pharma
        ("Melazam Cream", "Zam Zam Pharma", "Skin Care"),
        ("Climbazam Shampoo", "Zam Zam Pharma", "Hair Care"),
        ("Deep Cleanz Face Wash", "Zam Zam Pharma", "Skin Care"),
        ("Heliocare SPF-90 Gel", "Zam Zam Pharma", "Sun Protection"),
        ("Heliocare SPF-50 Gel", "Zam Zam Pharma", "Sun Protection"),
        ("Teen Derm Gel", "Zam Zam Pharma", "Skin Care"),
        ("Deep Moist Lotion", "Zam Zam Pharma", "Skin Care"),
        ("Fucithalmic Eye Oint", "Zam Zam Pharma", "Eye Care"),
        ("Be White Gel", "Zam Zam Pharma", "Skin Care"),
        ("Be White Emulsion", "Zam Zam Pharma", "Skin Care"),
        ("Be White Whitening Foam", "Zam Zam Pharma", "Skin Care"),
        ("Be White Essence", "Zam Zam Pharma", "Skin Care"),
        ("Cicastim Gel", "Zam Zam Pharma", "Skin Care"),
        ("Foltene Men Treatment", "Zam Zam Pharma", "Hair Care"),

        # Herbo Natural
        ("Ashwagandha", "Herbo Natural", "Herbal"),
        ("Fertilex", "Herbo Natural", "Herbal"),
        ("Ginkgo Biloba", "Herbo Natural", "Herbal"),
        ("Gluconil", "Herbo Natural", "Herbal"),
        ("Maca Root", "Herbo Natural", "Herbal"),
        ("Weight Gainer", "Herbo Natural", "Supplement"),
        ("Height Growth", "Herbo Natural", "Supplement"),
        ("Moringa", "Herbo Natural", "Herbal"),
        ("Memory Support", "Herbo Natural", "Herbal"),
        ("Testo Burst", "Herbo Natural", "Supplement"),
        ("Spirulina", "Herbo Natural", "Herbal"),

        # Remedy Counter
        ("Modalert", "Remedy Counter", "Neuro"),
        ("Waklert", "Remedy Counter", "Neuro"),
        ("Artvigil", "Remedy Counter", "Neuro"),
        ("Tretinoin Cream", "Remedy Counter", "Skin Care"),
        ("Careprost", "Remedy Counter", "Eye Care"),
        ("Bimat", "Remedy Counter", "Eye Care"),
        ("Lumigan", "Remedy Counter", "Eye Care"),
        ("Fildena", "Remedy Counter", "Specialty"),
        ("Cenforce", "Remedy Counter", "Specialty"),
        ("Tadalista", "Remedy Counter", "Specialty"),
        ("Kamagra", "Remedy Counter", "Specialty"),

        # Ailaaj (Duplicates omitted, focus on unique)
        ("V-Wash", "Ailaaj", "Skin Care"),

        # Nutrican
        ("Nutrican Adult Dog Food", "Nutrican Pakistan", "Pet Care"),
        ("Nutrican Adult Cat Food", "Nutrican Pakistan", "Pet Care"),
        ("Nutrican Kitten Food", "Nutrican Pakistan", "Pet Care"),

        # Mediate Pharmaceutical
        ("Apizole", "Mediate Pharmaceutical", "General"),
        ("M-Gesic", "Mediate Pharmaceutical", "Pain Relief"),
        ("Zipadone", "Mediate Pharmaceutical", "Neuro"),
        ("Diazomil", "Mediate Pharmaceutical", "Neuro"),
        ("Ecitropa", "Mediate Pharmaceutical", "General"),
        ("Meduxa", "Mediate Pharmaceutical", "General"),
        ("Razepam", "Mediate Pharmaceutical", "Neuro"),
        ("Lexotec", "Mediate Pharmaceutical", "Neuro"),
        ("M-Zolam", "Mediate Pharmaceutical", "Neuro"),
        ("M-Lam", "Mediate Pharmaceutical", "Neuro"),
        ("Phenobarbitone", "Mediate Pharmaceutical", "Neuro"),
        ("Panagon", "Mediate Pharmaceutical", "General"),
        ("Tridol", "Mediate Pharmaceutical", "Pain Relief"),
        ("C-Prox", "Mediate Pharmaceutical", "Antibiotic"),
        ("Ezemox", "Mediate Pharmaceutical", "Antibiotic"),
        ("Clomela", "Mediate Pharmaceutical", "General"),

        # Biomedics
        ("Diakit", "Biomedics Pharmaceutica", "General"),
        ("Arthorid", "Biomedics Pharmaceutica", "Bone Care"),
        ("Sufferex", "Biomedics Pharmaceutica", "General"),
        ("Nemrox", "Biomedics Pharmaceutica", "General"),
        ("Biovit", "Biomedics Pharmaceutica", "Multivitamin"),

        # Izfaar
        ("I-Zit Facewash", "Izfaar Pharmaceuticals", "Skin Care"),
        ("Iz-Glow Serum", "Izfaar Pharmaceuticals", "Skin Care"),
        ("I-Sol Sunblock", "Izfaar Pharmaceuticals", "Sun Protection"),
        ("Iz-Scab", "Izfaar Pharmaceuticals", "Skin Care"),
        ("Mel-Iz", "Izfaar Pharmaceuticals", "Skin Care"),

        # Versatile Herbal
        ("V-Ginkgo", "Versatile Herbal", "Herbal"),
        ("V-Ginseng", "Versatile Herbal", "Herbal"),
        ("V-Moringa", "Versatile Herbal", "Herbal"),
        ("V-Ashwagandha", "Versatile Herbal", "Herbal"),
        ("V-Memory", "Versatile Herbal", "Herbal"),
        ("V-Power", "Versatile Herbal", "Supplement"),

        # Veterinary
        ("Vinkosel", "Veterinary", "Animal Health"),
        ("Nilzan Plus", "Veterinary", "Animal Health"),
        ("Oxafax", "Veterinary", "Animal Health"),
        ("Ivercip", "Veterinary", "Animal Health"),
        ("Selmectin", "Veterinary", "Animal Health"),
        ("Starflox", "Veterinary", "Animal Health"),
        ("Simparica", "Veterinary", "Animal Health"),
        ("Synulox", "Veterinary", "Animal Health"),

        # Nabiqasim
        ("Gabica", "Nabiqasim Industries", "Neuro"),
        ("Nuberol", "Nabiqasim Industries", "Pain Relief"),
        ("Nuberol Forte", "Nabiqasim Industries", "Pain Relief"),
        ("Lipirex", "Nabiqasim Industries", "Cardio"),
        ("Vastose", "Nabiqasim Industries", "Cardio"),
        ("Omi", "Nabiqasim Industries", "Gastro"),
        ("Eziday", "Nabiqasim Industries", "Cardio"),
        ("Eziday Duo", "Nabiqasim Industries", "Cardio"),
        ("Zonat", "Nabiqasim Industries", "General"),
        ("Valit", "Nabiqasim Industries", "General"),
        ("Qalsan-D", "Nabiqasim Industries", "Supplement"),
        ("Nabidoc", "Nabiqasim Industries", "General"),
        ("Epival", "Nabiqasim Industries", "Neuro"),
        ("Relaxin", "Nabiqasim Industries", "General")
    ]

    for name, mfr, cat in data:
        # Determine form and unit automatically
        form = "Tab"
        unit = "Strip"
        
        lower_name = name.lower()
        if "cream" in lower_name or "ointment" in lower_name or "gel" in lower_name or "oint" in lower_name:
            form = "Cream"
            unit = "Tube"
        elif "syrup" in lower_name or "susp" in lower_name or "syp" in lower_name or "facewash" in lower_name or "wash" in lower_name or "lotion" in lower_name or "wash" in lower_name:
            form = "Syp"
            unit = "Bottle"
        elif "soap" in lower_name or "bar" in lower_name:
            form = "Soap"
            unit = "Piece"
        elif "serum" in lower_name or "drop" in lower_name or "essence" in lower_name or "spray" in lower_name:
            form = "Drop"
            unit = "Bottle"
        elif "powder" in lower_name or "sachet" in lower_name or "food" in lower_name:
            form = "Powder"
            unit = "Pack"
        elif "inj" in lower_name:
            form = "Inj"
            unit = "Vial"
        elif "capsule" in lower_name or "cap " in lower_name or name.endswith("Cap"):
            form = "Cap"
            unit = "Strip"

        # Pricing logic
        pp = random.choice([80, 120, 150, 250, 450, 600])
        sp = int(pp * 1.25)
        
        cursor.execute("""
            INSERT INTO Medicines (Name, Type, Category, Manufacturer, Unit, PurchasePrice, SalePrice, Status)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """, (name, form, cat, mfr, unit, pp, sp, 'Active'))

    conn.commit()
    conn.close()
    print(f"[OK] Successfully added {len(data)} custom medicines to {target_db}.")

if __name__ == "__main__":
    seed_custom_list()
