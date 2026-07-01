-- Seed 10 customers imported from customer.xlsx (ลูกค้า section only)
-- Run this script ONCE on production after verifying data below.

DECLARE @nextSeq INT;
SELECT @nextSeq = ISNULL(MAX(
    TRY_CAST(SUBSTRING(CustomerCode, 5, LEN(CustomerCode)) AS INT)
), 0) + 1
FROM Customers
WHERE CustomerCode LIKE 'CUS-%';

-- 1. บริษัท โรงพยาบาลรวมแพทย์สุโขทัย จำกัด (สำนักงานใหญ่)
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'บริษัท โรงพยาบาลรวมแพทย์สุโขทัย จำกัด (สำนักงานใหญ่)')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 0) AS VARCHAR), 3)), N'บริษัท โรงพยาบาลรวมแพทย์สุโขทัย จำกัด (สำนักงานใหญ่)', N'0645536000106', N'151 หมู่ที่ 1 ถนนจรดวิถีถ่อง ตำบลบ้านกล้วย อำเภอเมืองสุโขทัย จ.สุโขทัย 64000', NULL, NULL, 0, 1);

-- 2. ชเนษฎ์ ศรีสุโข
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'ชเนษฎ์ ศรีสุโข')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 1) AS VARCHAR), 3)), N'ชเนษฎ์ ศรีสุโข', N'1102000809208', N'เลขที 2/21 เอส ซี เอ็ม ซี คลินิกเวชกรรม ถนน สีลม 9 ซอยศึกษาวิทยา ชั้น 2 อาคารมหานครเซ็นเตอร์ แขวงสีลม เขตบางรัก กรุงเทพมหานคร 10500', N'0974282999', NULL, 0, 1);

-- 3. บริษัท คิงเมดออลกู๊ดส์ จำกัด
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'บริษัท คิงเมดออลกู๊ดส์ จำกัด')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 2) AS VARCHAR), 3)), N'บริษัท คิงเมดออลกู๊ดส์ จำกัด', N'0135565006880', N'345/2 หมู่บ้านสิริเพลส ราชพฤกษ์ 345 หมู่ที่1 ตำบลบางคูวัด อำเภอเมืองปทุมธานี จังหวัดปทุมธานี 12000', NULL, NULL, 0, 1);

-- 4. บริษัท ทอมมี่ แอนด์ คอมพาเนี่ยน เฮลธ์แคร์ โซลูชั่น จำกัด
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'บริษัท ทอมมี่ แอนด์ คอมพาเนี่ยน เฮลธ์แคร์ โซลูชั่น จำกัด')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 3) AS VARCHAR), 3)), N'บริษัท ทอมมี่ แอนด์ คอมพาเนี่ยน เฮลธ์แคร์ โซลูชั่น จำกัด', N'0205568066556', N'เลขที่ 88/132 หมู่ที่ 1 ตำบลสุรศักดิ์ อำเภอศรีราชา จังหวัดชลบุรี 20110', NULL, NULL, 0, 1);

-- 5. บริษัท เน็กซ์ วิชั่น เทคโนโลยี จำกัด
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'บริษัท เน็กซ์ วิชั่น เทคโนโลยี จำกัด')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 4) AS VARCHAR), 3)), N'บริษัท เน็กซ์ วิชั่น เทคโนโลยี จำกัด', N'0105551072745', N'50/1056 หมู่2 หมู่บ้านสถาพร ถนนรังสิต นครนายก ตำบลบึงยี่โถ อำเภอธัญบุรี จังหวัดปทุมธานี 12130', NULL, NULL, 0, 1);

-- 6. บริษัท บางปะกอก ฮอสพิทอล กรุ๊ป จำกัด (สาขา3)
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'บริษัท บางปะกอก ฮอสพิทอล กรุ๊ป จำกัด (สาขา3)')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 5) AS VARCHAR), 3)), N'บริษัท บางปะกอก ฮอสพิทอล กรุ๊ป จำกัด (สาขา3)', N'0105534124325', N'757 ซอย รังสิต-นครนายก 53 ตำบลประชาธิปัตย์ อำเภอธัญบุรี ปทุมธานี 12130', NULL, NULL, 0, 1);

-- 7. บริษัท แฟคเตอรี แอนด์ อีควิปเมนท์ กสิกรไทย จำกัด (สำนักงานใหญ่)
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'บริษัท แฟคเตอรี แอนด์ อีควิปเมนท์ กสิกรไทย จำกัด (สำนักงานใหญ่)')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 6) AS VARCHAR), 3)), N'บริษัท แฟคเตอรี แอนด์ อีควิปเมนท์ กสิกรไทย จำกัด (สำนักงานใหญ่)', N'0105533079091', N'400/22 อาคารธนาคารกสิกรไทย ชั้น 7 ถนนพหลโยธิน แขวงสามเสนใน เขตพญาไท กรุงเทพมหานคร 10400', NULL, NULL, 0, 1);

-- 8. บริษัท ภูรินทร์แอนด์เจตน์จิณณ์ จํากัด (สำนักงานใหญ่)
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'บริษัท ภูรินทร์แอนด์เจตน์จิณณ์ จํากัด (สำนักงานใหญ่)')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 7) AS VARCHAR), 3)), N'บริษัท ภูรินทร์แอนด์เจตน์จิณณ์ จํากัด (สำนักงานใหญ่)', N'0205567039598', N'24/61 หมู่ที่ 5 ตำบลหนองปรือ อำเภอบางละมุง จ.ชลบุรี 20150', NULL, NULL, 0, 1);

-- 9. บริษัท เมด ไอ (ไทยแลนด์) จำกัด
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'บริษัท เมด ไอ (ไทยแลนด์) จำกัด')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 8) AS VARCHAR), 3)), N'บริษัท เมด ไอ (ไทยแลนด์) จำกัด', N'0105565105365', N'429/77 หมู่บ้าน พรีเมี่ยมเพลส 9  ถนนสุคนธสวัสดิ์ แขวงลาดพร้าว เขตลาดพร้าว กรุงเทพมหานคร 10230', NULL, NULL, 0, 1);

-- 10. บริษัท อาร์.ที.ไทยเมดิคอล จำกัด (สำนักงานใหญ่)
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerName = N'บริษัท อาร์.ที.ไทยเมดิคอล จำกัด (สำนักงานใหญ่)')
    INSERT INTO Customers (CustomerCode, CustomerName, TaxId, Address, PhoneNumber, Email, CreditLimit, IsActive)
    VALUES (CONCAT('CUS-', RIGHT('000' + CAST((@nextSeq + 9) AS VARCHAR), 3)), N'บริษัท อาร์.ที.ไทยเมดิคอล จำกัด (สำนักงานใหญ่)', N'0205547009821', N'เลขที่ 74/2 หมู่ที่ 6 ตำบลสัตหีบ อำเภอสัตหีบ จังหวัดชลบุรี 20180', NULL, NULL, 0, 1);

