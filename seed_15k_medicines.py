"""
Medixa PharmaBilling - Pakistan Medicine Database Seeder
Generates 15,000+ unique Pakistani medicine entries and seeds them into PharmaDB.sqlite
"""

import sqlite3
import os
import random
import sys

# ─────────────────────────────────────────────────────────
# 1. REAL PAKISTANI MANUFACTURERS / COMPANIES
# ─────────────────────────────────────────────────────────
MANUFACTURERS = [
    "Getz Pharma", "Abbott Pakistan", "GSK Pakistan", "Pfizer Pakistan",
    "Searle Pakistan", "Sami Pharmaceuticals", "Ferozsons", "AGP Limited",
    "Highnoon Laboratories", "Hilton Pharma", "Martin Dow", "Bosch Pharma",
    "Zafa Pharmaceutical", "IBL Healthcare", "Medipak", "PharmEvo",
    "Brookes Pharma", "Wilshire Laboratories", "Shaigan Pharma", "Adamjee",
    "Obs Pharma", "Pacific Pharmaceuticals", "Novartis Pakistan", "Bayer Pakistan",
    "Roche Pakistan", "Sanofi Pakistan", "AstraZeneca Pakistan", "Merck Pakistan",
    "Johnson & Johnson", "Chiesi Pakistan", "Genix Pharma", "Platinum Pharma",
    "Global Pharmaceuticals", "Popular Pharma", "Macter International",
    "Nabi Qasim", "New Majeed Medicine", "Maple Pharmaceuticals", "Asian Pharma",
    "Prime Pharma", "Atco Laboratories", "Cirin Pharmaceuticals", "Saydon",
    "Helix Pharma", "Surge Laboratories", "Bionmed", "Concept Pharma",
    "Eros Pharma", "Delta Pharma", "Graffini Pharma", "Hamaz Pharma",
    "ICI Pakistan", "Indus Pharma", "Ipca Laboratories", "Jafer Brothers",
    "Karachi Labs", "Mega Pharma", "Noor Pharma", "Opal Laboratories",
    "Pioneer Pharma", "Quality Pharma", "Rhine Pharma", "Siza International",
    "Siza Pharma", "Stragen Pakistan", "Sun Pharmaceuticals", "Torrent Pakistan",
    "Valor Pharma", "Wyeth Pakistan", "Xenon Pakistan", "Yur Pharma",
    "Zexon Pharma", "Zulfiqar Pharma", "Unison Chemical Works", "Vision Pharmaceuticals"
]

# ─────────────────────────────────────────────────────────
# 2. DOSAGE FORMS & UNITS
# ─────────────────────────────────────────────────────────
FORMS = {
    "Tab":    ["250mg", "500mg", "875mg", "1g", "5mg", "10mg", "20mg", "25mg",
               "40mg", "50mg", "75mg", "100mg", "150mg", "200mg", "300mg", "400mg",
               "600mg", "800mg", "1000mg", "2mg", "4mg", "8mg", "12mg", "16mg"],
    "Cap":    ["250mg", "500mg", "100mg", "200mg", "300mg", "400mg", "150mg",
               "75mg", "50mg", "25mg", "450mg", "600mg"],
    "Syp":    ["125mg/5ml", "250mg/5ml", "100mg/5ml", "200mg/5ml", "60ml",
               "120ml", "60mg/5ml", "30mg/5ml", "15mg/5ml"],
    "Inj":    ["500mg", "1g", "2g", "250mg", "100mg/ml", "50mg/ml", "10mg/ml",
               "40mg/2ml", "80mg/2ml", "4mg/ml", "8mg/ml"],
    "Drop":   ["0.5%", "1%", "0.1%", "15ml", "10ml", "5ml"],
    "Cream":  ["1%", "2%", "0.5%", "15g", "30g", "0.025%", "0.1%"],
    "Oint":   ["0.5%", "1%", "2%", "15g", "20g", "30g"],
    "Susp":   ["125mg/5ml", "250mg/5ml", "200mg/5ml", "100mg/5ml"],
    "Inhaler":["100mcg", "200mcg", "250mcg", "50mcg", "25mcg/125mcg"],
    "Patch":  ["5mg/24hr", "10mg/24hr", "2.5mg/24hr"],
    "Sachet": ["2g", "5g", "10g", "ORS Formula"],
    "Gel":    ["1%", "2%", "0.5%", "0.75%", "30g", "50g"],
    "Lotion": ["1%", "2%", "100ml", "200ml"],
}

UNITS = {
    "Tab": "Strip", "Cap": "Strip", "Syp": "Bottle", "Inj": "Vial",
    "Drop": "Bottle", "Cream": "Tube", "Oint": "Tube", "Susp": "Bottle",
    "Inhaler": "Piece", "Patch": "Piece", "Sachet": "Box", "Gel": "Tube",
    "Lotion": "Bottle",
}

# ─────────────────────────────────────────────────────────
# 3. MEDICINE CATEGORIES & THEIR GENERIC DRUG NAMES
# ─────────────────────────────────────────────────────────
MEDICINES_BY_CATEGORY = {
    "Antibiotics": [
        "Amoxicillin", "Ampicillin", "Amoxicillin-Clavulanate", "Azithromycin",
        "Clarithromycin", "Ciprofloxacin", "Levofloxacin", "Moxifloxacin",
        "Ofloxacin", "Cefuroxime", "Ceftriaxone", "Cefixime", "Cefadroxil",
        "Cefazolin", "Cloxacillin", "Doxycycline", "Metronidazole", "Tinidazole",
        "Erythromycin", "Roxithromycin", "Clindamycin", "Linezolid", "Vancomycin",
        "Meropenem", "Imipenem", "Gentamicin", "Tobramycin", "Colistin",
        "Piperacillin-Tazobactam", "Ceftazidime", "Cefoperazone", "Sulbactam",
        "Trimethoprim-Sulfamethoxazole", "Nitrofurantoin", "Fosfomycin",
        "Mupirocin", "Chloramphenicol", "Tetracycline", "Minocycline",
        "Fusidic Acid", "Rifampicin", "Isoniazid", "Pyrazinamide", "Ethambutol",
        "Streptomycin", "Nalidixic Acid", "Norfloxacin", "Neomycin",
        "Bacitracin", "Polymyxin B",
    ],
    "Antifungals": [
        "Fluconazole", "Itraconazole", "Ketoconazole", "Voriconazole",
        "Amphotericin B", "Nystatin", "Clotrimazole", "Miconazole",
        "Terbinafine", "Griseofulvin", "Econazole", "Butoconazole",
        "Caspofungin", "Posaconazole",
    ],
    "Antivirals": [
        "Acyclovir", "Valacyclovir", "Oseltamivir", "Ribavirin", "Sofosbuvir",
        "Daclatasvir", "Lamivudine", "Tenofovir", "Emtricitabine", "Efavirenz",
        "Lopinavir-Ritonavir", "Favipiravir", "Remdesivir",
    ],
    "Antiparasitics": [
        "Mebendazole", "Albendazole", "Ivermectin", "Praziquantel",
        "Pyrantel Pamoate", "Chloroquine", "Hydroxychloroquine",
        "Artemether", "Lumefantrine", "Primaquine", "Quinine",
    ],
    "Cardiology": [
        "Atenolol", "Metoprolol", "Bisoprolol", "Carvedilol", "Propranolol",
        "Amlodipine", "Nifedipine", "Diltiazem", "Verapamil", "Felodipine",
        "Losartan", "Valsartan", "Olmesartan", "Telmisartan", "Candesartan",
        "Enalapril", "Ramipril", "Lisinopril", "Captopril", "Perindopril",
        "Atorvastatin", "Rosuvastatin", "Simvastatin", "Pravastatin",
        "Fenofibrate", "Gemfibrozil", "Ezetimibe", "Aspirin",
        "Clopidogrel", "Ticagrelor", "Warfarin", "Dabigatran", "Rivaroxaban",
        "Apixaban", "Heparin", "Enoxaparin", "Digoxin", "Amiodarone",
        "Furosemide", "Hydrochlorothiazide", "Spironolactone", "Torasemide",
        "Nitroglycerin", "Isosorbide Mononitrate", "Isosorbide Dinitrate",
        "Ivabradine", "Sacubitril-Valsartan", "Dapagliflozin", "Empagliflozin",
    ],
    "Diabetes": [
        "Metformin", "Glibenclamide", "Gliclazide", "Glimepiride", "Glipizide",
        "Sitagliptin", "Vildagliptin", "Saxagliptin", "Alogliptin", "Linagliptin",
        "Empagliflozin", "Dapagliflozin", "Canagliflozin", "Pioglitazone",
        "Acarbose", "Repaglinide", "Nateglinide", "Exenatide", "Liraglutide",
        "Semaglutide", "Dulaglutide", "Insulin Glargine", "Insulin Detemir",
        "Insulin Aspart", "Insulin Lispro", "Insulin Regular", "Insulin NPH",
        "Insulin 70/30", "Insulin Degludec",
    ],
    "Gastroenterology": [
        "Omeprazole", "Esomeprazole", "Lansoprazole", "Pantoprazole", "Rabeprazole",
        "Ranitidine", "Famotidine", "Cimetidine", "Domperidone", "Metoclopramide",
        "Ondansetron", "Granisetron", "Tropisetron", "Hyoscine",
        "Dicyclomine", "Mebeverine", "Trimebutine", "Mesalazine",
        "Sulfasalazine", "Olsalazine", "Balsalazide", "Loperamide",
        "Bismuth Subsalicylate", "Lactulose", "Polyethylene Glycol", "Bisacodyl",
        "Senna", "Psyllium", "Simethicone", "Activated Charcoal", "Sucralfate",
        "Misoprostol", "Ursodeoxycholic Acid", "Rifaximin",
    ],
    "Respiratory": [
        "Salbutamol", "Terbutaline", "Salmeterol", "Formoterol", "Indacaterol",
        "Ipratropium", "Tiotropium", "Umeclidinium", "Glycopyrronium",
        "Beclometasone", "Budesonide", "Fluticasone", "Mometasone",
        "Theophylline", "Aminophylline", "Doxofylline", "Montelukast",
        "Zafirlukast", "Zileuton", "Cetirizine", "Loratadine", "Fexofenadine",
        "Chlorpheniramine", "Diphenhydramine", "Promethazine",
        "Ambroxol", "Bromhexine", "Guaifenesin", "Acetylcysteine",
        "Carbocisteine", "Erdosteine", "Codeine", "Dextromethorphan",
    ],
    "Neurology": [
        "Gabapentin", "Pregabalin", "Carbamazepine", "Valproate", "Phenytoin",
        "Lamotrigine", "Levetiracetam", "Oxcarbazepine", "Topiramate",
        "Phenobarbitone", "Clonazepam", "Diazepam", "Lorazepam", "Midazolam",
        "Haloperidol", "Risperidone", "Olanzapine", "Quetiapine", "Aripiprazole",
        "Clozapine", "Amitriptyline", "Imipramine", "Nortriptyline",
        "Fluoxetine", "Paroxetine", "Sertraline", "Citalopram", "Escitalopram",
        "Venlafaxine", "Duloxetine", "Mirtazapine", "Bupropion",
        "Trihexyphenidyl", "Levodopa-Carbidopa", "Ropinirole", "Pramipexole",
        "Donepezil", "Rivastigmine", "Memantine", "Zolpidem", "Zopiclone",
        "Melatonin", "Tramadol", "Morphine", "Codeine Phosphate",
    ],
    "Pain Management": [
        "Paracetamol", "Ibuprofen", "Diclofenac", "Naproxen", "Indomethacin",
        "Piroxicam", "Meloxicam", "Celecoxib", "Etoricoxib", "Aspirin",
        "Mefenamic Acid", "Ketorolac", "Dexketoprofen", "Aceclofenac",
        "Tenoxicam", "Lornoxicam", "Flurbiprofen", "Ketoprofen",
        "Morphine", "Tramadol", "Oxycodone", "Fentanyl", "Buprenorphine",
        "Pregabalin", "Duloxetine", "Lidocaine", "Benzocaine",
    ],
    "Bone & Joint": [
        "Calcium Carbonate", "Calcium Citrate", "Vitamin D3", "Alfacalcidol",
        "Calcitriol", "Alendronate", "Risedronate", "Ibandronate", "Zoledronic Acid",
        "Strontium Ranelate", "Denosumab", "Teriparatide", "Glucosamine",
        "Chondroitin", "Diacerhein", "Colchicine", "Allopurinol", "Febuxostat",
        "Methotrexate", "Leflunomide", "Hydroxychloroquine", "Sulfasalazine",
        "Etanercept", "Infliximab", "Adalimumab",
    ],
    "Dermatology": [
        "Hydrocortisone", "Betamethasone", "Fluocinolone", "Triamcinolone",
        "Clobetasol", "Mometasone", "Halobetasol", "Desonide",
        "Selenium Sulfide", "Zinc Pyrithione", "Terbinafine", "Clotrimazole",
        "Ketoconazole", "Econazole", "Miconazole", "Azelaic Acid",
        "Benzoyl Peroxide", "Salicylic Acid", "Tretinoin", "Adapalene",
        "Isotretinoin", "Erythromycin", "Clindamycin", "Dapsone",
        "Permethrin", "Lindane", "Ivermectin",
    ],
    "Ophthalmology": [
        "Tropicamide", "Phenylephrine", "Cyclopentolate", "Timolol",
        "Betaxolol", "Brimonidine", "Dorzolamide", "Brinzolamide",
        "Latanoprost", "Travoprost", "Bimatoprost", "Tafluprost",
        "Pilocarpine", "Chloramphenicol", "Ciprofloxacin", "Ofloxacin",
        "Gentamicin", "Tobramycin", "Moxifloxacin", "Gatifloxacin",
        "Dexamethasone", "Prednisolone", "Fluorometholone", "Loteprednol",
        "Diclofenac", "Ketorolac", "Nepafenac", "Bromfenac",
        "Sodium Hyaluronate", "Carboxymethylcellulose",
    ],
    "ENT": [
        "Xylometazoline", "Oxymetazoline", "Naphazoline", "Beclometasone",
        "Fluticasone", "Mometasone", "Flunisolide", "Sodium Cromoglicate",
        "Azelastine", "Levocabastine", "Budesonide", "Triamcinolone",
        "Ciprofloxacin", "Ofloxacin", "Neomycin", "Hydrocortisone",
        "Clotrimazole", "Acetic Acid",
    ],
    "Urology": [
        "Tamsulosin", "Alfuzosin", "Doxazosin", "Terazosin",
        "Finasteride", "Dutasteride", "Sildenafil", "Tadalafil",
        "Vardenafil", "Avanafil", "Oxybutynin", "Tolterodine",
        "Solifenacin", "Darifenacin", "Trospium", "Mirabegron",
        "Furosemide", "Hydrochlorothiazide",
    ],
    "Gynecology": [
        "Progesterone", "Norethisterone", "Medroxyprogesterone", "Levonorgestrel",
        "Desogestrel", "Drospirenone", "Ethinyl Estradiol", "Estradiol",
        "Conjugated Estrogens", "Clomiphene", "Letrozole", "Gonadotropins",
        "Oxytocin", "Misoprostol", "Mifepristone", "Tranexamic Acid",
        "Utrogestan", "Duphaston",
    ],
    "Hormones & Endocrine": [
        "Levothyroxine", "Carbimazole", "Propylthiouracil", "Prednisolone",
        "Dexamethasone", "Methylprednisolone", "Hydrocortisone", "Fludrocortisone",
        "Testosterone", "Danazol", "Tamoxifen", "Anastrozole", "Letrozole",
        "Human Growth Hormone",
    ],
    "Vitamins & Supplements": [
        "Vitamin C", "Vitamin B Complex", "Vitamin B12", "Folic Acid",
        "Ferrous Sulfate", "Ferrous Fumarate", "Iron Polymaltose",
        "Zinc Sulfate", "Zinc Gluconate", "Magnesium Hydroxide",
        "Magnesium Sulfate", "Potassium Chloride", "Calcium Gluconate",
        "Multivitamins", "Omega-3 Fatty Acids", "Coenzyme Q10",
        "Vitamin E", "Vitamin A", "Vitamin D", "Biotin",
        "Thiamine", "Riboflavin", "Niacin", "Pantothenic Acid",
        "Pyridoxine", "Mecobalamin", "Selenious Acid",
    ],
    "Psychiatry": [
        "Lithium Carbonate", "Lamotrigine", "Valproate", "Carbamazepine",
        "Quetiapine", "Olanzapine", "Aripiprazole", "Risperidone", "Clozapine",
        "Haloperidol", "Chlorpromazine", "Fluoxetine", "Sertraline",
        "Paroxetine", "Escitalopram", "Citalopram", "Venlafaxine",
        "Duloxetine", "Mirtazapine", "Trazodone", "Bupropion",
        "Alprazolam", "Diazepam", "Clonazepam", "Buspirone",
    ],
    "Oncology": [
        "Cyclophosphamide", "Methotrexate", "Fluorouracil", "Doxorubicin",
        "Vincristine", "Cisplatin", "Carboplatin", "Paclitaxel",
        "Docetaxel", "Gemcitabine", "Capecitabine", "Imatinib",
        "Erlotinib", "Gefitinib", "Sorafenib", "Sunitinib",
        "Tamoxifen", "Anastrozole", "Letrozole", "Exemestane",
        "Rituximab", "Trastuzumab",
    ],
    "Anesthesia & ICU": [
        "Propofol", "Ketamine", "Thiopental", "Etomidate", "Sevoflurane",
        "Isoflurane", "Halothane", "Nitrous Oxide", "Fentanyl",
        "Morphine", "Vecuronium", "Atracurium", "Succinylcholine",
        "Neostigmine", "Atropine", "Epinephrine", "Norepinephrine",
        "Dopamine", "Dobutamine", "Vasopressin",
    ],
    "Vaccines": [
        "Hepatitis B Vaccine", "Hepatitis A Vaccine", "Measles Mumps Rubella",
        "DPT Vaccine", "Polio Vaccine", "Typhoid Vaccine", "Rabies Vaccine",
        "Meningococcal Vaccine", "Pneumococcal Vaccine", "HPV Vaccine",
        "Influenza Vaccine", "Varicella Vaccine", "Yellow Fever Vaccine",
    ],
    "Contraceptives": [
        "Combined Oral Contraceptive", "Progestin-only Pill", "Emergency Pill",
        "Copper IUD", "Hormonal IUD", "Injectable Contraceptive",
        "Implant Contraceptive",
    ],
    "Hepatology": [
        "Sofosbuvir", "Daclatasvir", "Ledipasvir", "Ribavirin",
        "Ursodeoxycholic Acid", "Silymarin", "Lactulose",
        "Spironolactone", "Furosemide", "Propranolol", "Neomycin",
    ],
}

# ─────────────────────────────────────────────────────────
# 4. BRAND PREFIXES/SUFFIXES used by Pakistani pharma companies
# ─────────────────────────────────────────────────────────
BRAND_PREFIXES = [
    "", "", "",  # Empty = use generic name as brand
    "Ex", "Pro", "Neo", "Ultra", "Max", "Plus", "SR", "XR", "MR",
    "Forte", "DS", "ER", "CR", "LA", "OD",
]

# Brand name suffixes to create unique brand entries per manufacturer
BRAND_SUFFIXES = [
    "", "", "",
    " Forte", " Plus", " DS", " SR", " XR", " OD", " ER",
    " CR", " LA", " 500", " 250", " 1g",
]

# ─────────────────────────────────────────────────────────
# 5. PRICE RANGES BY CATEGORY
# ─────────────────────────────────────────────────────────
PRICE_RANGES = {
    "Antibiotics":         (50, 350),
    "Antifungals":         (80, 500),
    "Antivirals":          (100, 3000),
    "Antiparasitics":      (30, 200),
    "Cardiology":          (40, 600),
    "Diabetes":            (50, 2000),
    "Gastroenterology":    (30, 300),
    "Respiratory":         (50, 800),
    "Neurology":           (40, 700),
    "Pain Management":     (20, 400),
    "Bone & Joint":        (50, 500),
    "Dermatology":         (60, 400),
    "Ophthalmology":       (80, 600),
    "ENT":                 (50, 300),
    "Urology":             (60, 500),
    "Gynecology":          (50, 800),
    "Hormones & Endocrine":(60, 1500),
    "Vitamins & Supplements":(30,400),
    "Psychiatry":          (50, 500),
    "Oncology":            (200, 15000),
    "Anesthesia & ICU":    (100, 5000),
    "Vaccines":            (200, 3000),
    "Contraceptives":      (50, 500),
    "Hepatology":          (100, 5000),
}

def round_price(p):
    """Round price to nearest 5"""
    return int(round(p / 5.0)) * 5

def generate_medicine_name(generic, manufacturer, form, dose):
    """Create a brand name for a medicine."""
    # Company short codes for brand naming
    company_codes = {
        "Getz Pharma": ["Get", "Getz"],
        "Abbott Pakistan": ["Abb", ""],
        "GSK Pakistan": ["GSK", ""],
        "Pfizer Pakistan": ["Pfi", ""],
        "Searle Pakistan": ["Searle", ""],
        "Sami Pharmaceuticals": ["Sami", "Sam"],
        "Ferozsons": ["Feroz", ""],
        "AGP Limited": ["AGP", ""],
        "Highnoon Laboratories": ["HN", "Highnoon"],
        "Hilton Pharma": ["Hilton", "Hilt"],
        "Martin Dow": ["MD", ""],
        "Bosch Pharma": ["Bosch", ""],
        "Zafa Pharmaceutical": ["Zafa", ""],
        "IBL Healthcare": ["IBL", ""],
        "Medipak": ["Medi", ""],
        "PharmEvo": ["Evo", ""],
    }

    prefix = company_codes.get(manufacturer, ["", ""])[random.randint(0, 1)]

    # Create brand name variations
    styles = [
        f"{prefix}{generic}" if prefix else generic,                    # Prefix + generic
        f"{generic} {prefix}" if prefix else generic,                   # Generic + suffix
        f"{generic[:4].capitalize()}{prefix[:2]}",                      # Blend
        f"{prefix}{generic[:5].capitalize()}",                          # Prefix blend
        generic,                                                         # Pure generic
    ]
    name = random.choice(styles)

    # Add form suffix sometimes
    add_suffix = random.random()
    if add_suffix < 0.3 and dose:
        name = f"{name} {dose}"
    elif add_suffix < 0.5:
        suffixes = ["Forte", "Plus", "SR", "XR", "DS", "ER", "OD"]
        name = f"{name} {random.choice(suffixes)}"

    return name.strip()


def generate_all_medicines():
    entries = []
    seen_names = set()

    for category, generics in MEDICINES_BY_CATEGORY.items():
        price_range = PRICE_RANGES.get(category, (40, 400))

        for generic in generics:
            # Assign multiple manufacturers and forms per generic
            num_manufacturers = random.randint(3, min(8, len(MANUFACTURERS)))
            selected_mfrs = random.sample(MANUFACTURERS, num_manufacturers)

            for mfr in selected_mfrs:
                # Pick 1-3 forms per manufacturer per generic
                selected_forms = random.sample(list(FORMS.keys()), random.randint(1, 3))

                for form in selected_forms:
                    doses = FORMS[form]
                    num_doses = random.randint(1, min(3, len(doses)))
                    selected_doses = random.sample(doses, num_doses)

                    for dose in selected_doses:
                        # Generate brand name
                        brand_name = generate_medicine_name(generic, mfr, form, dose)

                        # Ensure uniqueness
                        unique_key = f"{brand_name}|{mfr}"
                        if unique_key in seen_names:
                            brand_name = f"{brand_name} ({mfr[:3]})"
                            unique_key = f"{brand_name}|{mfr}"
                        if unique_key in seen_names:
                            continue

                        seen_names.add(unique_key)

                        # Calculate prices
                        base_pp = random.uniform(price_range[0], price_range[1])
                        purchase_price = round_price(base_pp)
                        margin = random.uniform(1.10, 1.30)
                        sale_price = round_price(base_pp * margin)

                        unit = UNITS.get(form, "Strip")
                        min_stock = random.choice([5, 10, 15, 20])

                        entries.append((
                            brand_name, form, category, mfr, unit,
                            purchase_price, sale_price, min_stock, "Active"
                        ))

    return entries


def seed_database(db_path, entries):
    """Insert all entries into the SQLite database."""
    print(f"\n[+] Connecting to database: {db_path}")
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # Progress tracking
    total = len(entries)
    inserted = 0
    skipped = 0
    batch_size = 500

    print(f"[*] Seeding {total:,} medicine entries...\n")

    for i in range(0, total, batch_size):
        batch = entries[i:i+batch_size]
        for entry in batch:
            try:
                cursor.execute("""
                    INSERT OR IGNORE INTO Medicines
                    (Name, Type, Category, Manufacturer, Unit, PurchasePrice, SalePrice, MinStock, Status)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, entry)
                if cursor.rowcount > 0:
                    inserted += 1
                else:
                    skipped += 1
            except Exception as e:
                skipped += 1

        conn.commit()
        progress = min(i + batch_size, total)
        pct = (progress / total) * 100
        bar = "#" * int(pct / 5) + "-" * (20 - int(pct / 5))
        print(f"\r  [{bar}] {pct:.0f}% | {progress:,}/{total:,} processed | [OK] {inserted:,} inserted | [SKIP] {skipped:,} skipped", end="", flush=True)

    conn.close()
    print(f"\n\n[OK] Done! Inserted: {inserted:,} | Skipped (duplicates): {skipped:,}")
    return inserted


def main():
    # Locate PharmaDB.sqlite
    search_paths = [
        os.path.join(os.path.dirname(os.path.abspath(__file__)), "PharmaDB.sqlite"),
        os.path.join(os.path.dirname(os.path.abspath(__file__)), "bin", "Debug", "PharmaDB.sqlite"),
        os.path.join(os.path.dirname(os.path.abspath(__file__)), "bin", "Release", "PharmaDB.sqlite"),
        "PharmaDB.sqlite",
    ]

    db_path = None
    for p in search_paths:
        if os.path.exists(p):
            db_path = p
            break

    if not db_path:
        print("[!] PharmaDB.sqlite not found! Please make sure it exists.")
        print(f"   Searched in: {search_paths}")
        sys.exit(1)

    print("=" * 60)
    print("  Medixa Pakistan Medicine Database Seeder")
    print("=" * 60)
    print(f"\n[DB] Database: {db_path}")

    # Check current count
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) FROM Medicines")
    existing = cursor.fetchone()[0]
    conn.close()
    print(f"[STAT] Current medicines in DB: {existing:,}")

    # Generate
    print("\n[*] Generating medicine data...")
    entries = generate_all_medicines()
    print(f"[OK] Generated {len(entries):,} unique entries!\n")

    # Seed
    inserted = seed_database(db_path, entries)

    # Final count
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) FROM Medicines")
    final_count = cursor.fetchone()[0]
    cursor.execute("SELECT COUNT(DISTINCT Category) FROM Medicines")
    categories = cursor.fetchone()[0]
    cursor.execute("SELECT COUNT(DISTINCT Manufacturer) FROM Medicines")
    manufacturers_count = cursor.fetchone()[0]
    conn.close()

    print("\n" + "=" * 60)
    print("  🎉 SEEDING COMPLETE!")
    print("=" * 60)
    print(f"  📦 Total Medicines   : {final_count:,}")
    print(f"  🏥 Categories        : {categories}")
    print(f"  🏭 Manufacturers     : {manufacturers_count}")
    print(f"  ✅ Newly Inserted    : {inserted:,}")
    print("=" * 60)
    print("\n💡 Now rebuild your app to use the updated database.")
    print("   The medicine search in Sales/Purchase will now have all medicines!\n")


if __name__ == "__main__":
    main()
