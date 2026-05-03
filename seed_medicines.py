import sqlite3
import os

medicines_data = [
    # Getz Pharma
    ("Advant", "Getz Pharma", "Cardiology"),
    ("Advantec", "Getz Pharma", "Cardiology"),
    ("Cadwin", "Getz Pharma", "Cardiology"),
    ("Nebil", "Getz Pharma", "Cardiology"),
    ("Rovista", "Getz Pharma", "Cardiology"),
    ("Rovista EZ", "Getz Pharma", "Cardiology"),
    ("Getformin", "Getz Pharma", "Diabetes"),
    ("Diampa", "Getz Pharma", "Diabetes"),
    ("Diampa-M", "Getz Pharma", "Diabetes"),
    ("Treviamet", "Getz Pharma", "Diabetes"),
    ("Basagine", "Getz Pharma", "Diabetes"),
    ("Insuget", "Getz Pharma", "Diabetes"),
    ("Amclav", "Getz Pharma", "Antibiotics"),
    ("Amclav DS", "Getz Pharma", "Antibiotics"),
    ("Cefiget", "Getz Pharma", "Antibiotics"),
    ("Cipesta", "Getz Pharma", "Antibiotics"),
    ("Claritek", "Getz Pharma", "Antibiotics"),
    ("Leflox", "Getz Pharma", "Antibiotics"),
    ("Zolid", "Getz Pharma", "Antibiotics"),
    ("Fexet", "Getz Pharma", "Allergy"),
    ("Fexet-D", "Getz Pharma", "Allergy"),
    ("Larinex", "Getz Pharma", "Allergy"),
    ("Rincit", "Getz Pharma", "Allergy"),
    ("Bekson", "Getz Pharma", "Respiratory"),
    ("Bekson Forte", "Getz Pharma", "Respiratory"),
    ("Salbo", "Getz Pharma", "Respiratory"),
    ("Xaltide", "Getz Pharma", "Respiratory"),
    ("Gabica", "Getz Pharma", "Neurology"),
    ("Gabix", "Getz Pharma", "Neurology"),
    ("Celbexx", "Getz Pharma", "Neurology"),
    ("Starcox", "Getz Pharma", "Neurology"),
    ("Zafon Fast", "Getz Pharma", "Neurology"),
    ("Zafon Rapid", "Getz Pharma", "Neurology"),
    ("Osam-D", "Getz Pharma", "Bone"),
    ("Agnar", "Getz Pharma", "Bone"),
    ("Cartigen", "Getz Pharma", "Bone"),
    ("Cartigen Plus", "Getz Pharma", "Bone"),
    ("Core 24", "Getz Pharma", "Bone"),
    ("Risek", "Getz Pharma", "Gastro"),
    ("Risek Insta", "Getz Pharma", "Gastro"),
    ("Acogest", "Getz Pharma", "Gastro"),
    ("Cytopan", "Getz Pharma", "Gastro"),
    ("Pantra", "Getz Pharma", "Gastro"),
    ("Tirzee", "Getz Pharma", "Specialized"),
    ("Getfil", "Getz Pharma", "Specialized"),
    ("Tamsolin", "Getz Pharma", "Specialized"),
    ("Tamsolin Plus", "Getz Pharma", "Specialized"),
    ("Zurig", "Getz Pharma", "Specialized"),
    ("Mibega", "Getz Pharma", "Specialized"),
    ("Artheget", "Getz Pharma", "Extra"),
    ("Asacol", "Getz Pharma", "Extra"),
    ("Cenova", "Getz Pharma", "Extra"),
    ("Montiget", "Getz Pharma", "Extra"),
    ("Orlift", "Getz Pharma", "Extra"),
    ("Piravir", "Getz Pharma", "Extra"),

    # Abbott
    ("Norvasc", "Abbott", "Cardiology"),
    ("Tricor", "Abbott", "Cardiology"),
    ("Hyzaar", "Abbott", "Cardiology"),
    ("Lipitor", "Abbott", "Cardiology"),
    ("Crestor", "Abbott", "Cardiology"),
    ("Glucophage", "Abbott", "Diabetes"),
    ("Glucovance", "Abbott", "Diabetes"),
    ("Januvia", "Abbott", "Diabetes"),
    ("Victoza", "Abbott", "Diabetes"),
    ("Augmentin", "Abbott", "Antibiotics"),
    ("Zinnat", "Abbott", "Antibiotics"),
    ("Flagyl", "Abbott", "Antibiotics"),
    ("Ciproxin", "Abbott", "Antibiotics"),
    ("Claritin", "Abbott", "Allergy"),
    ("Allegra", "Abbott", "Allergy"),
    ("Flonase", "Abbott", "Allergy"),
    ("Ventolin", "Abbott", "Respiratory"),
    ("Symbicort", "Abbott", "Respiratory"),
    ("Pulmicort", "Abbott", "Respiratory"),
    ("Depakote", "Abbott", "Neurology"),
    ("Neurontin", "Abbott", "Neurology"),
    ("Topamax", "Abbott", "Neurology"),
    ("Ensure", "Abbott", "Bone/Supp"),
    ("Caltrate", "Abbott", "Bone/Supp"),
    ("Glucerna", "Abbott", "Bone/Supp"),
    ("Nexium", "Abbott", "Gastro"),
    ("Creon", "Abbott", "Gastro"),
    ("Humira", "Abbott", "Specialized"),
    ("Similac", "Abbott", "Specialized"),
    ("Pediasure", "Abbott", "Specialized"),
    ("Synthroid", "Abbott", "Specialized"),

    # GSK
    ("Coreg", "GSK", "Cardiology"),
    ("Plavix", "GSK", "Cardiology"),
    ("Avlocardyl", "GSK", "Cardiology"),
    ("Avandia", "GSK", "Diabetes"),
    ("Amoxil", "GSK", "Antibiotics"),
    ("Bactroban", "GSK", "Antibiotics"),
    ("Flixonase", "GSK", "Allergy"),
    ("Rhinocort", "GSK", "Allergy"),
    ("Seretide", "GSK", "Respiratory"),
    ("Advair", "GSK", "Respiratory"),
    ("Flovent", "GSK", "Respiratory"),
    ("Panadol", "GSK", "Pain/Neuro"),
    ("Ibupral", "GSK", "Pain/Neuro"),
    ("Caltra", "GSK", "Supplements"),
    ("Cervarix", "GSK", "Vaccines"),
    ("Infanrix", "GSK", "Vaccines"),
    ("Synflorix", "GSK", "Vaccines"),
    ("Boostrix", "GSK", "Vaccines"),

    # Pfizer
    ("Zithromax", "Pfizer", "Antibiotics"),
    ("Bactrim", "Pfizer", "Antibiotics"),
    ("Septrim", "Pfizer", "Antibiotics"),
    ("Tazocin", "Pfizer", "Antibiotics"),
    ("Xyzal", "Pfizer", "Allergy"),
    ("Spiriva", "Pfizer", "Inhaler"),
    ("Lyrica", "Pfizer", "Pain"),
    ("Celebrex", "Pfizer", "Pain"),
    ("Fosamax", "Pfizer", "Bone"),
    ("Prevnar 13", "Pfizer", "Vaccine"),

    # Ferozsons
    ("Atenolol", "Ferozsons", "Cardiology"),
    ("Amlodipine", "Ferozsons", "Cardiology"),
    ("Feroz ACE", "Ferozsons", "Cardiology"),
    ("Feroz Card", "Ferozsons", "Cardiology"),
    ("Feroz Met", "Ferozsons", "Diabetes"),
    ("Feroz Glucovance", "Ferozsons", "Diabetes"),
    ("Feroz Victoza", "Ferozsons", "Diabetes"),
    ("Feroz Amoxil", "Ferozsons", "Antibiotics"),
    ("Feroz Augmentin", "Ferozsons", "Antibiotics"),
    ("Feroz Zinnat", "Ferozsons", "Antibiotics"),
    ("Feroz Ciproflox", "Ferozsons", "Antibiotics"),
    ("Feroz Loratadine", "Ferozsons", "Allergy"),
    ("Feroz Cetirizine", "Ferozsons", "Allergy"),
    ("Feroz Ventolin", "Ferozsons", "Respiratory"),
    ("Feroz Seretide", "Ferozsons", "Respiratory"),
    ("Feroz Gabapentin", "Ferozsons", "Neurology"),
    ("Feroz Tramadol", "Ferozsons", "Neurology"),
    ("Feroz Caltrate", "Ferozsons", "Bone"),
    ("Feroz Osteo", "Ferozsons", "Bone"),
    ("Feroz Multivitamins", "Ferozsons", "Bone"),
    ("Feroz Nexium", "Ferozsons", "Gastro"),
    ("Feroz Pancreatin", "Ferozsons", "Gastro"),
    ("Feroz Ranitidine", "Ferozsons", "Gastro"),
    ("Feroz Insulin", "Ferozsons", "Specialized"),

    # Searle
    ("Cardace", "Searle", "Cardiology"),
    ("Searle Losartan", "Searle", "Cardiology"),
    ("Tramadol Searle", "Searle", "Neurology"),
    ("Gabapentin Searle", "Searle", "Neurology"),
    ("Osteo-Bone", "Searle", "Bone"),

    # Sami
    ("Sami Atenolol", "Sami", "Cardiology"),
    ("Sami Amlodipine", "Sami", "Cardiology"),
    ("Sami Losartan", "Sami", "Cardiology"),
    ("Sami Carvedilol", "Sami", "Cardiology"),
    ("Sami Metformin", "Sami", "Diabetes"),
    ("Sami Sitagliptin", "Sami", "Diabetes"),
    ("Sami Amoxicillin", "Sami", "Antibiotics"),
    ("Sami Augmentin", "Sami", "Antibiotics"),
    ("Sami Ciprofloxacin", "Sami", "Antibiotics"),
    ("Sami Cefuroxime", "Sami", "Antibiotics"),
    ("Sami Loratadine", "Sami", "Allergy"),
    ("Sami Cetirizine", "Sami", "Allergy"),
    ("Sami Esomeprazole", "Sami", "Gastro"),
    ("Sami Ranitidine", "Sami", "Gastro"),

    # AGP, Hilton, Highnoon, etc (Generic prefixing)
    ("AGP Metformin", "AGP", "Diabetes"),
    ("AGP Amoxicillin", "AGP", "Antibiotics"),
    ("Hilton Metformin", "Hilton", "Diabetes"),
    ("Hilton Augmentin", "Hilton", "Antibiotics"),
    ("Highnoon Losartan", "Highnoon", "Cardiology"),
    ("Highnoon Esomeprazole", "Highnoon", "Gastro"),
    ("Bosch Amlodipine", "Bosch", "Cardiology"),
    ("Zafa Metformin", "Zafa", "Diabetes")
]

# Generate SQL
sql_file = os.path.join(os.getcwd(), 'seed_medicines.sql')
with open(sql_file, 'w', encoding='utf-8') as f:
    f.write("-- Bulk Seed Medicines\n")
    for name, manufacturer, category in medicines_data:
        # Simple Tab/Active defaults
        sql = f"INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('{name}', '{manufacturer}', '{category}', 'Tab', 'Active');\n"
        f.write(sql)

print(f"Generated {len(medicines_data)} inserts in {sql_file}")

# Apply to local DBs
dbs = ["PharmaDB.sqlite", "Release/PharmaDB.sqlite"]
for db_path in dbs:
    if os.path.exists(db_path):
        try:
            conn = sqlite3.connect(db_path)
            cursor = conn.cursor()
            with open(sql_file, 'r', encoding='utf-8') as f:
                cursor.executescript(f.read())
            conn.commit()
            conn.close()
            print(f"Applied to {db_path}")
        except Exception as e:
            print(f"Error applying to {db_path}: {e}")
