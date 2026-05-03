BEGIN TRANSACTION;

-- Step 1: Create a mapping from duplicate MedicineID to the Master MedicineID
-- We choose the one with the lowest MedicineID as the Master for each Name
CREATE TEMP TABLE MedMapping AS
SELECT 
    m.MedicineID as OldID,
    (SELECT MIN(MedicineID) FROM Medicines sub WHERE LOWER(sub.Name) = LOWER(m.Name)) as MasterID
FROM Medicines m
WHERE m.MedicineID != (SELECT MIN(MedicineID) FROM Medicines sub WHERE LOWER(sub.Name) = LOWER(m.Name));

-- Step 2: Update foreign keys in referencing tables to point to the MasterID
UPDATE Stocks 
SET MedicineID = (SELECT MasterID FROM MedMapping WHERE OldID = Stocks.MedicineID)
WHERE MedicineID IN (SELECT OldID FROM MedMapping);

UPDATE SaleDetails 
SET MedicineID = (SELECT MasterID FROM MedMapping WHERE OldID = SaleDetails.MedicineID)
WHERE MedicineID IN (SELECT OldID FROM MedMapping);

UPDATE PurchaseDetails 
SET MedicineID = (SELECT MasterID FROM MedMapping WHERE OldID = PurchaseDetails.MedicineID)
WHERE MedicineID IN (SELECT OldID FROM MedMapping);

-- Step 3: Delete the duplicates from the Medicines table
DELETE FROM Medicines 
WHERE MedicineID IN (SELECT OldID FROM MedMapping);

-- Step 4: Drop the temp table
DROP TABLE MedMapping;

COMMIT;
