BEGIN TRANSACTION;
PRAGMA foreign_keys = OFF;

-- Step 1: Dedup mapping (we cluster by EXACT name, keep MIN MedicineID as Master)
CREATE TEMP TABLE Dedup AS
SELECT 
    MedicineID as OldID,
    FIRST_VALUE(MedicineID) OVER(PARTITION BY LOWER(TRIM(Name)) ORDER BY MedicineID ASC) as MasterID
FROM Medicines;

-- Step 2: Now we have a deduplicated Medicines table, but with gaps.
-- Let's create a mapping from current Master MedicineID to a gap-less contiguous sequence.
CREATE TEMP TABLE SeqMapping AS
SELECT 
    MasterID as OldID,
    ROW_NUMBER() OVER(ORDER BY MasterID ASC) as NewID
FROM (SELECT DISTINCT MasterID FROM Dedup);

-- For Stocks: 
UPDATE Stocks 
SET MedicineID = (SELECT NewID FROM SeqMapping WHERE OldID = (SELECT MasterID FROM Dedup WHERE OldID = Stocks.MedicineID));

-- For SaleDetails:
UPDATE SaleDetails 
SET MedicineID = (SELECT NewID FROM SeqMapping WHERE OldID = (SELECT MasterID FROM Dedup WHERE OldID = SaleDetails.MedicineID));

-- For PurchaseDetails:
UPDATE PurchaseDetails 
SET MedicineID = (SELECT NewID FROM SeqMapping WHERE OldID = (SELECT MasterID FROM Dedup WHERE OldID = PurchaseDetails.MedicineID));

-- Delete the non-masters BEFORE updating IDs
DELETE FROM Medicines 
WHERE MedicineID NOT IN (SELECT DISTINCT MasterID FROM Dedup);

-- Temporarily shift IDs to avoid UNIQUE constraint violations during update
UPDATE Medicines
SET MedicineID = -MedicineID;

-- Update Medicines to the NewID
UPDATE Medicines
SET MedicineID = (SELECT NewID FROM SeqMapping WHERE OldID = -Medicines.MedicineID);

-- Reset autoincrement sequence
UPDATE sqlite_sequence SET seq = (SELECT MAX(MedicineID) FROM Medicines) WHERE name = 'Medicines';

DROP TABLE Dedup;
DROP TABLE SeqMapping;

PRAGMA foreign_keys = ON;
COMMIT;
