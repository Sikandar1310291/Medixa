import sqlite3
import os

db_paths = [
    r'C:\ProgramData\Medixa\PharmaDB.sqlite',
    r'C:\Users\ma516\OneDrive\Desktop\Pharma\bin\Debug\PharmaDB.sqlite'
]

sami_data = """
2Sum – 1g, 2g, 500mg
Abacus – 100mg, 200mg, 40mg/5ml, 100mg/5ml
Actim – 2.5mg, 5mg, 10mg
Afixba – 2.5mg, 5mg
Alkeris – 100mg, 200mg
Antial – 10mg
Arceva – 20/120mg, 40/240mg, 80/480mg
Arilio – 0.5mg, 1mg
Azitma – 250mg, 500mg, 200mg/5ml
BECEPTOR – 10mg, 20mg
Bisleri – 100mg
Bistavo – 20mg
Breeky – 200mcg
Brino – 250mg, 500mg
Caricef – 200mg, 400mg, 100mg/5ml
DELANZO – 30mg, 60mg
Dicloran – 50mg, 75mg, 100mg
Diclorep – 50mg
Doctile – 3g
DULTIX – 20mg, 30mg, 60mg
Ecasil – 400mg, 600mg, 100mg/5ml
EFFIFLOX – 250mg, 500mg
ELEZO – 100mg/5ml, 150mg
EMPOLI – 10mg, 25mg
Empoli Plus – 5mg+850mg, 5mg+1000mg, 12.5mg+500mg, 12.5mg+850mg, 12.5mg+1000mg
Enier – 8mg, 16mg, 24mg
Favuza – 200mg
Fungone – 150mg
FYLOD – 400mg, 100mg/5ml
Gpride – 1mg, 2mg, 4mg
Gpride-M – 1mg/500mg, 2mg/500mg
Grasil – 25mg, 50mg, 100mg, 250mg, 500mg
ICOFATE-M – 500mg
IDZO – 5mg/5ml
Intig-D – (no clear potency)
ITAGLIP – 50mg
ITAGLIP Plus – 50/500mg, 50/850mg, 50/1000mg
ITP – 50mg, 150mg
Izato – 500mg, 100mg/5ml
JAKNIB – 5mg
Kinz – 10mg, 20mg
LACASIL – 10g
Levijon – (no clear potency)
Mabil – 500mcg
Magura – 0.5mg, 2mg
Melor – 7.5mg, 15mg
Mofest – 400mg
Montika – 4mg, 5mg, 10mg
Movax – 2mg, 4mg
Moveryl – 100mg, 200mg
Neege – 20mg, 40mg
Neo-Antial – 5mg
Neofil – 300mcg
Neo-Sedil – 5mg
Neucef – 500mg, 125mg/5ml
Nims – 100mg
Nivador – 250mg, 500mg, 1g
Nixaf – 200mg, 550mg
Novidat – 250mg, 500mg
NovoTEpH – 20mg, 40mg
OCVIR-V – 400/100mg
Onato – 5mg, 10mg
Onato-V – 5/80mg, 5/160mg, 10/160mg
Onato-V HCT – 5/160/12.5mg, 10/160/12.5mg, 10/160/25mg
Orno – 8mg
Osiris – 20mg
Oxidil – 250mg, 500mg, 1g, 2g
Pencital – 2.25g, 4.5g
Peridone – 10mg
PLATRID – 75mg
PLATRID-AP – 75/75mg
Pralzo – 0.5mg
Pregy – 25mg, 50mg, 75mg, 100mg, 150mg
Provas – 300mg
PROVAS DUO – 500+200mg
QUADRO – 300mcg, 600mcg
Recada – 10mg, 30mg, 100mg
Rhytab – 5mg, 7.5mg
Ritban – 2.5mg, 10mg, 15mg, 20mg
Rithmo – 250mg, 500mg
Rolac – 100mg
Romero – 500mg, 1g
Ropo – 2000IU, 4000IU
Rosera – 5mg, 10mg, 20mg
Sambro – 3mg
SAMCLOM – 50mg
Sedil – 10mg
SILDAT – 4mg, 8mg
Slate – 250mg, 500mg
Solfy – 5mg
Tamiso MR – 0.4mg
Tapento – 50mg, 75mg
TEFOD – 25mg
Telarb – 20mg, 40mg, 80mg
Telarb-H – 40/12.5mg, 80/12.5mg
TELARB-Plus – 40/5mg, 40/10mg, 80/5mg, 80/10mg
TEpH – 20mg, 40mg
Tercica – 200mg, 300mg, 400mg
Timequin – 15/120mg, 40/320mg
Tonoflex – 50mg, 100mg
TONOFLEX-P – 75/650mg
TRUVA – 10mg, 20mg, 40mg
Urigo – 40mg, 80mg
VALSATRIL – 24/26mg, 49/51mg
VIPTIN-MET – 50/500mg, 50/850mg, 50/1000mg
Xyquil DR – 10/10mg
Yorker – 250mg, 500mg, 1g
ZOLEN – (no potency clear)
"""

getz_data = """
Amtas – 5/40mg, 5/80mg, 10/40mg
Zurig – 40mg, 80mg
Cipesta – 250mg, 500mg
Cytopan – 50mg, 75mg
Cova – 80mg, 160mg
Cova-H – 160/12.5mg
Trevia – 100mg
Treviamet – 50/500mg
Fenoget – 67mg, 200mg
Mascol – 400mg, 800mg
Fexet – 60mg, 120mg, 180mg
Fexet D – 60/120mg
Tamsolin – 0.4mg
Tamsolin-S – 0.4/6mg
Larinex – 5mg
Claritek – 250mg, 500mg
Zoliget – 15/2mg, 30/4mg
Zetro – 250mg, 500mg
Diampa – 10mg, 25mg
Diampa M – 5/1000mg
Zolid – 15mg, 30mg
Zolid Plus – 15/500mg, 15/850mg
Sivab – 5mg, 7.5mg
Tasmi – 20mg, 40mg, 80mg
Getryl – 1mg, 2mg, 3mg, 4mg
Lenwin – 500mg
Olrlifit – 120mg
Covam – 5/80mg, 5/160mg
Xeticam – 250mg, 500mg
Gabix – 100mg, 300mg
Zavaget – 10mg
Cefiget – 200mg, 400mg
Zafon fast – 8mg
Rivaxo – 10mg, 20mg
Ezita – 10mg
Montiget – 5mg, 10mg
Nebil – 2.5mg, 5mg
Vilget M – 50/850mg
Mebever MR – 200mg
Rovista – 5mg, 10mg, 20mg
Regasta – 50mg
Lipiget – 10mg, 20mg, 40mg
Lipiget EZ – 10/10mg
Celbex – 100mg, 200mg
Advant – 8mg, 16mg
Nervon – 500mcg
Amclav – 625mg
Leflox – 250mg, 500mg, 750mg
Co tasmi – 40/12.5mg, 80/12.5mg
Solifen – 5mg, 10mg
Nimixa – 200mg, 550mg
Gabica – 75mg, 100mg, 150mg, 300mg
Vilget – 50/500mg
"""

def parse_medicines(data, manufacturer):
    records = []
    for line in data.strip().split('\n'):
        line = line.strip()
        if not line:
            continue
        if '–' in line:
            name_part, potencies_part = line.split('–', 1)
        elif '-' in line:
            name_part, potencies_part = line.split('-', 1)
        else:
            continue
            
        name = name_part.strip()
        potencies_part = potencies_part.strip()
        
        if "no clear potency" in potencies_part.lower() or "no potency clear" in potencies_part.lower():
            records.append((name, manufacturer))
        else:
            potencies = [p.strip() for p in potencies_part.split(',')]
            for p in potencies:
                records.append((f"{name} {p}", manufacturer))
    return records

all_records = parse_medicines(sami_data, "SAMI Pharma") + parse_medicines(getz_data, "Getz Pharma")

for db_path in db_paths:
    if os.path.exists(db_path):
        print(f"Connecting to {db_path}...")
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # We already wiped medicines, but just to be sure
        cursor.execute("DELETE FROM Medicines")
        
        # Insert new ones
        count = 0
        for med_name, manufacturer in all_records:
            cursor.execute(
                "INSERT INTO Medicines (Name, Type, Category, Manufacturer, Unit, PurchasePrice, SalePrice, MinStock, Status) "
                "VALUES (?, 'Tablet', 'General', ?, 'Box', 0, 0, 10, 'Active')",
                (med_name, manufacturer)
            )
            count += 1
            
        conn.commit()
        conn.close()
        print(f"Successfully inserted {count} medicines into {db_path}!")

print("All done!")
