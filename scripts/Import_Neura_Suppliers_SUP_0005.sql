/*
    Supplier import script for BizCore.
    - Safe to run more than once.
    - Existing rows are updated by SupplierCode.
    - New rows are inserted when SupplierCode does not exist.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Suppliers TABLE
    (
        SupplierCode NVARCHAR(30) NOT NULL,
        SupplierName NVARCHAR(200) NOT NULL,
        TaxId NVARCHAR(30) NULL,
        Address NVARCHAR(500) NULL,
        PhoneNumber NVARCHAR(50) NULL,
        Email NVARCHAR(256) NULL
    );

    DECLARE @ExistingSuppliers TABLE
    (
        SupplierCode NVARCHAR(30) NOT NULL PRIMARY KEY
    );

    INSERT INTO @Suppliers
    (
        SupplierCode,
        SupplierName,
        TaxId,
        Address,
        PhoneNumber,
        Email
    )
    VALUES
        (N'SUP-0001', N'บริษัท สเวนนอร่า เมด จำกัด', N'0105560131210', N'144/41 ถนนสุขุมวิท 71 แขวงพระโขนงเหนือ เขตวัฒนา กรุงเทพมหานคร 10110', N'02-7113945', NULL),
        (N'SUP-0002', N'บริษัท เวลท์ โปรเกรสชั่น จำกัด', N'0105564025503', N'1/94 ถนนสุดบรรทัด ตำบลแก่งคอย อำเภแก่งคอย จังหวัดสระบุรี 18110', N'036-298299', NULL),
        (N'SUP-0003', N'บริษัท ฮอสพิทอล รีโนเวชั่น จำกัด', N'0105555154771', N'18 ถนนสวนผัก แขวงฉิมพลี เขตตลิ่งชัน กรุงเทพมหานคร 10170', N'02-4484398-99', NULL),
        (N'SUP-0004', N'บริษัท กรีน ฟาร์มา 2017 จำกัด', N'0105560219931', N'16 ซอยลาดพร้าว 124 (สวัสดิการ) แขวงพลับพลา เขตวังทองหลาง กรุงเทพมหานคร 10310', N'085-5429955', NULL),
        (N'SUP-0005', N'บริษัท มหาจักร อินเตอร์เนชั่นแนล จำกัด', N'0105537144378', N'120/5 หมู่ที่ 7 ถนนนครอินทร์ ตำบลบางคูเวียง อำเภอบางกรวย จังหวัดนนทบุรี 11130', N'02-4237633', NULL),
        (N'SUP-0006', N'บริษัท เอ็มบี มิสเตอร์แบ็กส์ (ประเทศไทย) จำกัด', N'0735564002028', N'24/29 หมู่ 3 ถนนพุทธมณฑลสาย 7 ตำบลหอมเกร็ด อำเภอสามพราน จังหวัดนครปฐม 73110', N'091-7961524', NULL),
        (N'SUP-0007', N'บริษัท แอดไวซ์ ไอที อินฟินิท จำกัด (มหาชน)', N'0107565000620', N'74/1 หมู่ที่ 1 ตำบลท่าอิฐ อำเภอปากเกร็ด จังหวัดนนทบุรี 11120', N'02-9088888', NULL),
        (N'SUP-0008', N'บริษัท ซี.ดี.เอ็ม.ที. (ประเทศไทย) จำกัด', N'0105563028312', N'99/162 ถนนกาญจนาภิเษก แขวงประเวศ  เขตประเวศ กรุงเทพมหานคร 10250', NULL, NULL);

    INSERT INTO @ExistingSuppliers (SupplierCode)
    SELECT s.SupplierCode
    FROM dbo.Suppliers s
    INNER JOIN @Suppliers source
        ON source.SupplierCode = s.SupplierCode;

    UPDATE target
    SET
        target.SupplierName = source.SupplierName,
        target.TaxId = source.TaxId,
        target.Address = source.Address,
        target.PhoneNumber = source.PhoneNumber,
        target.Email = source.Email,
        target.IsActive = 1
    FROM dbo.Suppliers target
    INNER JOIN @Suppliers source
        ON source.SupplierCode = target.SupplierCode;

    INSERT INTO dbo.Suppliers
    (
        SupplierCode,
        SupplierName,
        TaxId,
        Address,
        PhoneNumber,
        Email,
        CreditLimit,
        IsActive
    )
    SELECT
        s.SupplierCode,
        s.SupplierName,
        s.TaxId,
        s.Address,
        s.PhoneNumber,
        s.Email,
        0,
        1
    FROM @Suppliers s
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Suppliers existing
        WHERE existing.SupplierCode = s.SupplierCode
    )
    ORDER BY s.SupplierCode;

    COMMIT TRANSACTION;

    SELECT
        SUM(CASE WHEN existing.SupplierCode IS NULL THEN 1 ELSE 0 END) AS InsertedCount,
        SUM(CASE WHEN existing.SupplierCode IS NOT NULL THEN 1 ELSE 0 END) AS UpdatedCount,
        MIN(source.SupplierCode) AS FirstSupplierCode,
        MAX(source.SupplierCode) AS LastSupplierCode
    FROM @Suppliers source
    LEFT JOIN @ExistingSuppliers existing
        ON existing.SupplierCode = source.SupplierCode;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
