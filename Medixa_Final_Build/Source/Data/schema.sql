-- Pharma Billing System - SQLite Schema

-- 1. Medicines Table
CREATE TABLE IF NOT EXISTS Medicines (
    MedicineID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Type TEXT, -- Tab, Syp, Cap, etc.
    Category TEXT,
    Manufacturer TEXT,
    Unit TEXT, -- e.g., Box, Strip, Piece
    PurchasePrice REAL DEFAULT 0,
    SalePrice REAL DEFAULT 0,
    MinStock INTEGER DEFAULT 10,
    Status TEXT DEFAULT 'Active'
);

-- 2. Stocks Table (Batch-wise management)
CREATE TABLE IF NOT EXISTS Stocks (
    StockID INTEGER PRIMARY KEY AUTOINCREMENT,
    MedicineID INTEGER,
    BatchNo TEXT NOT NULL,
    RackNo TEXT DEFAULT 'Rack-1',
    ExpiryDate TEXT, -- YYYY-MM-DD
    Quantity INTEGER DEFAULT 0,
    SupplierID INTEGER,
    DateAdded TEXT DEFAULT (date('now')),
    FOREIGN KEY (MedicineID) REFERENCES Medicines(MedicineID)
);

-- 3. Suppliers Table
CREATE TABLE IF NOT EXISTS Suppliers (
    SupplierID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Contact TEXT,
    Email TEXT,
    Address TEXT,
    LedgerID INTEGER
);

-- 4. Customers Table
CREATE TABLE IF NOT EXISTS Customers (
    CustomerID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Contact TEXT,
    Email TEXT,
    Address TEXT,
    Balance REAL DEFAULT 0,
    LedgerID INTEGER
);

-- 5. Sales Table
CREATE TABLE IF NOT EXISTS Sales (
    SaleID INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerID INTEGER,
    SaleDate TEXT DEFAULT (datetime('now')),
    TotalAmount REAL DEFAULT 0,
    Discount REAL DEFAULT 0,
    Tax REAL DEFAULT 0,
    NetPaid REAL DEFAULT 0,
    Status TEXT DEFAULT 'Paid', -- Paid, Partial, Credit
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
);

-- 6. SaleDetails Table
CREATE TABLE IF NOT EXISTS SaleDetails (
    DetailID INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleID INTEGER,
    MedicineID INTEGER,
    BatchNo TEXT,
    RackNo TEXT,
    Quantity INTEGER,
    UnitPrice REAL,
    TotalPrice REAL,
    FOREIGN KEY (SaleID) REFERENCES Sales(SaleID),
    FOREIGN KEY (MedicineID) REFERENCES Medicines(MedicineID)
);

-- 7. Purchases Table
CREATE TABLE IF NOT EXISTS Purchases (
    PurchaseID INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierID INTEGER,
    InvoiceNo TEXT,
    PurchaseDate TEXT DEFAULT (datetime('now')),
    TotalAmount REAL DEFAULT 0,
    Tax REAL DEFAULT 0,
    Status TEXT DEFAULT 'Received',
    FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID)
);

-- 8. PurchaseDetails Table
CREATE TABLE IF NOT EXISTS PurchaseDetails (
    DetailID INTEGER PRIMARY KEY AUTOINCREMENT,
    PurchaseID INTEGER,
    MedicineID INTEGER,
    BatchNo TEXT,
    ExpiryDate TEXT,
    Quantity INTEGER,
    PurchasePrice REAL,
    TotalPrice REAL,
    FOREIGN KEY (PurchaseID) REFERENCES Purchases(PurchaseID),
    FOREIGN KEY (MedicineID) REFERENCES Medicines(MedicineID)
);

-- 9. Ledgers Table
CREATE TABLE IF NOT EXISTS Ledgers (
    LedgerID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT UNIQUE NOT NULL,
    Type TEXT, -- Customer, Supplier, Cash, Bank, Expense, Income
    Balance REAL DEFAULT 0
);

-- 10. LedgerTransactions Table
CREATE TABLE IF NOT EXISTS LedgerTransactions (
    TransactionID INTEGER PRIMARY KEY AUTOINCREMENT,
    LedgerID INTEGER,
    TransactionDate TEXT DEFAULT (datetime('now')),
    Description TEXT,
    Debit REAL DEFAULT 0,
    Credit REAL DEFAULT 0,
    Balance REAL DEFAULT 0,
    FOREIGN KEY (LedgerID) REFERENCES Ledgers(LedgerID)
);

-- 11. Users Table
CREATE TABLE IF NOT EXISTS Users (
    UserID INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT UNIQUE NOT NULL,
    Password TEXT NOT NULL,
    Role TEXT DEFAULT 'Admin'
);

-- 12. Initial Data
INSERT OR IGNORE INTO Users (Username, Password, Role) VALUES ('admin', 'admin123', 'Admin');
INSERT OR IGNORE INTO Ledgers (Name, Type, Balance) VALUES ('Cash Account', 'Cash', 0);
INSERT OR IGNORE INTO Ledgers (Name, Type, Balance) VALUES ('Sales Income', 'Income', 0);
INSERT OR IGNORE INTO Ledgers (Name, Type, Balance) VALUES ('Purchase Expense', 'Expense', 0);
-- Bulk Seed Medicines
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Advant', 'Getz Pharma', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Advantec', 'Getz Pharma', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Cadwin', 'Getz Pharma', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Nebil', 'Getz Pharma', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Rovista', 'Getz Pharma', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Rovista EZ', 'Getz Pharma', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Getformin', 'Getz Pharma', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Diampa', 'Getz Pharma', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Diampa-M', 'Getz Pharma', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Treviamet', 'Getz Pharma', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Basagine', 'Getz Pharma', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Insuget', 'Getz Pharma', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Amclav', 'Getz Pharma', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Amclav DS', 'Getz Pharma', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Cefiget', 'Getz Pharma', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Cipesta', 'Getz Pharma', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Claritek', 'Getz Pharma', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Leflox', 'Getz Pharma', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Zolid', 'Getz Pharma', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Fexet', 'Getz Pharma', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Fexet-D', 'Getz Pharma', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Larinex', 'Getz Pharma', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Rincit', 'Getz Pharma', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Bekson', 'Getz Pharma', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Bekson Forte', 'Getz Pharma', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Salbo', 'Getz Pharma', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Xaltide', 'Getz Pharma', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Gabica', 'Getz Pharma', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Gabix', 'Getz Pharma', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Celbexx', 'Getz Pharma', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Starcox', 'Getz Pharma', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Zafon Fast', 'Getz Pharma', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Zafon Rapid', 'Getz Pharma', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Osam-D', 'Getz Pharma', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Agnar', 'Getz Pharma', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Cartigen', 'Getz Pharma', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Cartigen Plus', 'Getz Pharma', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Core 24', 'Getz Pharma', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Risek', 'Getz Pharma', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Risek Insta', 'Getz Pharma', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Acogest', 'Getz Pharma', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Cytopan', 'Getz Pharma', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Pantra', 'Getz Pharma', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Tirzee', 'Getz Pharma', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Getfil', 'Getz Pharma', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Tamsolin', 'Getz Pharma', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Tamsolin Plus', 'Getz Pharma', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Zurig', 'Getz Pharma', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Mibega', 'Getz Pharma', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Artheget', 'Getz Pharma', 'Extra', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Asacol', 'Getz Pharma', 'Extra', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Cenova', 'Getz Pharma', 'Extra', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Montiget', 'Getz Pharma', 'Extra', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Orlift', 'Getz Pharma', 'Extra', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Piravir', 'Getz Pharma', 'Extra', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Norvasc', 'Abbott', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Tricor', 'Abbott', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Hyzaar', 'Abbott', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Lipitor', 'Abbott', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Crestor', 'Abbott', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Glucophage', 'Abbott', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Glucovance', 'Abbott', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Januvia', 'Abbott', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Victoza', 'Abbott', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Augmentin', 'Abbott', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Zinnat', 'Abbott', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Flagyl', 'Abbott', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Ciproxin', 'Abbott', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Claritin', 'Abbott', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Allegra', 'Abbott', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Flonase', 'Abbott', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Ventolin', 'Abbott', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Symbicort', 'Abbott', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Pulmicort', 'Abbott', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Depakote', 'Abbott', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Neurontin', 'Abbott', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Topamax', 'Abbott', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Ensure', 'Abbott', 'Bone/Supp', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Caltrate', 'Abbott', 'Bone/Supp', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Glucerna', 'Abbott', 'Bone/Supp', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Nexium', 'Abbott', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Creon', 'Abbott', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Humira', 'Abbott', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Similac', 'Abbott', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Pediasure', 'Abbott', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Synthroid', 'Abbott', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Coreg', 'GSK', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Plavix', 'GSK', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Avlocardyl', 'GSK', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Avandia', 'GSK', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Amoxil', 'GSK', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Bactroban', 'GSK', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Flixonase', 'GSK', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Rhinocort', 'GSK', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Seretide', 'GSK', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Advair', 'GSK', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Flovent', 'GSK', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Panadol', 'GSK', 'Pain/Neuro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Ibupral', 'GSK', 'Pain/Neuro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Caltra', 'GSK', 'Supplements', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Cervarix', 'GSK', 'Vaccines', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Infanrix', 'GSK', 'Vaccines', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Synflorix', 'GSK', 'Vaccines', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Boostrix', 'GSK', 'Vaccines', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Zithromax', 'Pfizer', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Bactrim', 'Pfizer', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Septrim', 'Pfizer', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Tazocin', 'Pfizer', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Xyzal', 'Pfizer', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Spiriva', 'Pfizer', 'Inhaler', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Lyrica', 'Pfizer', 'Pain', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Celebrex', 'Pfizer', 'Pain', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Fosamax', 'Pfizer', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Prevnar 13', 'Pfizer', 'Vaccine', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Atenolol', 'Ferozsons', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Amlodipine', 'Ferozsons', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz ACE', 'Ferozsons', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Card', 'Ferozsons', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Met', 'Ferozsons', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Glucovance', 'Ferozsons', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Victoza', 'Ferozsons', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Amoxil', 'Ferozsons', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Augmentin', 'Ferozsons', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Zinnat', 'Ferozsons', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Ciproflox', 'Ferozsons', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Loratadine', 'Ferozsons', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Cetirizine', 'Ferozsons', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Ventolin', 'Ferozsons', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Seretide', 'Ferozsons', 'Respiratory', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Gabapentin', 'Ferozsons', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Tramadol', 'Ferozsons', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Caltrate', 'Ferozsons', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Osteo', 'Ferozsons', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Multivitamins', 'Ferozsons', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Nexium', 'Ferozsons', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Pancreatin', 'Ferozsons', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Ranitidine', 'Ferozsons', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Feroz Insulin', 'Ferozsons', 'Specialized', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Cardace', 'Searle', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Searle Losartan', 'Searle', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Tramadol Searle', 'Searle', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Gabapentin Searle', 'Searle', 'Neurology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Osteo-Bone', 'Searle', 'Bone', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Atenolol', 'Sami', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Amlodipine', 'Sami', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Losartan', 'Sami', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Carvedilol', 'Sami', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Metformin', 'Sami', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Sitagliptin', 'Sami', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Amoxicillin', 'Sami', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Augmentin', 'Sami', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Ciprofloxacin', 'Sami', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Cefuroxime', 'Sami', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Loratadine', 'Sami', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Cetirizine', 'Sami', 'Allergy', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Esomeprazole', 'Sami', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Sami Ranitidine', 'Sami', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('AGP Metformin', 'AGP', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('AGP Amoxicillin', 'AGP', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Hilton Metformin', 'Hilton', 'Diabetes', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Hilton Augmentin', 'Hilton', 'Antibiotics', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Highnoon Losartan', 'Highnoon', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Highnoon Esomeprazole', 'Highnoon', 'Gastro', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Bosch Amlodipine', 'Bosch', 'Cardiology', 'Tab', 'Active');
INSERT OR IGNORE INTO Medicines (Name, Manufacturer, Category, Type, Status) VALUES ('Zafa Metformin', 'Zafa', 'Diabetes', 'Tab', 'Active');
