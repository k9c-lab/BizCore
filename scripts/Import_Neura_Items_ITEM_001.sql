/*
    Item import script for BizCore.
    - Safe to run more than once.
    - Existing rows are updated by ItemCode.
    - New rows are inserted when ItemCode does not exist.
    - Base selling price is imported as 0 because this file does not contain price data.
      Update `dbo.Items.UnitPrice` and `dbo.ItemPrices` separately after import if needed.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Items TABLE
    (
        ItemCode NVARCHAR(30) NOT NULL,
        ItemName NVARCHAR(200) NOT NULL,
        PartNumber NVARCHAR(80) NOT NULL
    );

    DECLARE @ExistingItems TABLE
    (
        ItemCode NVARCHAR(30) NOT NULL PRIMARY KEY
    );

    INSERT INTO @Items (ItemCode, ItemName, PartNumber)
    VALUES
        (N'ITEM-001', N'Syringe (Nipro) 3 ml', N'ITEM-001'),
        (N'ITEM-002', N'Syringe (Nipro) 5 ml', N'ITEM-002'),
        (N'ITEM-003', N'Syringe (Nipro) 10 ml', N'ITEM-003'),
        (N'ITEM-004', N'Syringe (Nipro) 20 ml', N'ITEM-004'),
        (N'ITEM-005', N'Syringe (Nipro) 50 ml (Catheter Tip)', N'ITEM-005'),
        (N'ITEM-006', N'Medicut IV Catheter (Nipro) 18Gx1 ¼"', N'ITEM-006'),
        (N'ITEM-007', N'Medicut IV Catheter (Nipro) 20Gx1¼"', N'ITEM-007'),
        (N'ITEM-008', N'Medicut IV Catheter (Nipro) 22Gx1¼"', N'ITEM-008'),
        (N'ITEM-009', N'Medicut IV Catheter (Nipro) 24Gx1"', N'ITEM-009'),
        (N'ITEM-010', N'EXTENSION TUBE (Nipro) 18"', N'ITEM-010'),
        (N'ITEM-011', N'EXTENSION TUBE (Nipro) 36"', N'ITEM-011'),
        (N'ITEM-012', N'เข็มฉีดยา (Nipro) 18Gx1"', N'ITEM-012'),
        (N'ITEM-013', N'เข็มฉีดยา (Nipro) 20Gx1"', N'ITEM-013'),
        (N'ITEM-014', N'เข็มฉีดยา (Nipro) 21Gx1"', N'ITEM-014'),
        (N'ITEM-015', N'เข็มฉีดยา (Nipro) 24Gx1"', N'ITEM-015'),
        (N'ITEM-016', N'HEPARIN-CAP', N'ITEM-016'),
        (N'ITEM-017', N'3 WAY STOPCOCK (NIPRO) 50 PCS', N'ITEM-017'),
        (N'ITEM-018', N'Syringe (Nipro) 50 ml (Cecentric Tip)', N'ITEM-018'),
        (N'ITEM-019', N'เทปใส Transpore ขนาด 1”', N'ITEM-019'),
        (N'ITEM-020', N'Hydrogen 450 CC', N'ITEM-020'),
        (N'ITEM-021', N'Normal Saline (NSS) 0.9% ขนาด 100 ml', N'ITEM-021'),
        (N'ITEM-022', N'แอลกอฮอล์ ALCOHOL 70% ขนาด 450 ml', N'ITEM-022'),
        (N'ITEM-023', N'ถุงมือ ชนิดไม่มีแป้ง Size S (ศรีตรัง)', N'ITEM-023'),
        (N'ITEM-024', N'ถุงมือ ชนิดไม่มีแป้ง Size M (ศรีตรัง)', N'ITEM-024'),
        (N'ITEM-025', N'ถุงมือ ชนิดไม่มีแป้ง Size L (ศรีตรัง)', N'ITEM-025'),
        (N'ITEM-026', N'Cleansing Enema Set', N'ITEM-026'),
        (N'ITEM-027', N'หน้ากากอนามัย', N'ITEM-027'),
        (N'ITEM-028', N'สำลีก้อน( 0.35g )  450 กรัม', N'ITEM-028'),
        (N'ITEM-029', N'สำลีชุบแอลกอฮอล์', N'ITEM-029'),
        (N'ITEM-030', N'หน้ากากอนามัย N 95', N'ITEM-030'),
        (N'ITEM-031', N'ชุดเกจ์ออกซิเจน', N'ITEM-031'),
        (N'ITEM-032', N'ถังออกซิเจน 6 คิว', N'ITEM-032'),
        (N'ITEM-033', N'รถเข็นถังออกซิเจน', N'ITEM-033'),
        (N'ITEM-034', N'กล่องสำลี พร้อมฝา 4*2.5', N'ITEM-034'),
        (N'ITEM-035', N'กล่องสำลี พร้อมฝา 5*6.5', N'ITEM-035'),
        (N'ITEM-036', N'หมวกตัวหนอนใยสังเคราะห์', N'ITEM-036'),
        (N'ITEM-037', N'เครื่องวัดความดัน', N'ITEM-037'),
        (N'ITEM-038', N'ชุดทำแผลสแตนเลส ปากคีบ 2 อัน', N'ITEM-038'),
        (N'ITEM-039', N'สายทูนิเก้', N'ITEM-039'),
        (N'ITEM-040', N'ผ้าปูเตียง ขนาด กว้าง 60 × 250 ซม. แบบไม่รัดมุม', N'ITEM-040'),
        (N'ITEM-041', N'ผ้ายาง', N'ITEM-041'),
        (N'ITEM-042', N'ผ้าห่ม', N'ITEM-042'),
        (N'ITEM-043', N'แผ่น CD', N'ITEM-043'),
        (N'ITEM-044', N'แผ่น DVD', N'ITEM-044'),
        (N'ITEM-045', N'ซองใส่แผ่น CD', N'ITEM-045'),
        (N'ITEM-046', N'Paracetamol 500mg (100เม็ด/กระปุก)', N'ITEM-046'),
        (N'ITEM-047', N'Chlorpheniramine maleate 4 mg', N'ITEM-047'),
        (N'ITEM-048', N'แอลกอฮอล์เจลล้างมือ', N'ITEM-048'),
        (N'ITEM-049', N'น้ำยาล้างมือ 3M ขนาด 1000 ML.', N'ITEM-049'),
        (N'ITEM-050', N'ถุงขยะสีดำ ขนาด 18×20 ไซส์ Xs', N'ITEM-050'),
        (N'ITEM-051', N'ถุงขยะสีดำ ขนาด 22×30 ไซส์ Ss', N'ITEM-051'),
        (N'ITEM-052', N'Sterile water Irrigate 1000 ml.', N'ITEM-052'),
        (N'ITEM-053', N'MAIWEI High Pressure Syringe 200ml.', N'ITEM-053'),
        (N'ITEM-054', N'hawkmed Disposable High-Pressure Angiographic Syringes 200ml.', N'ITEM-054'),
        (N'ITEM-055', N'Yushou CT High-pressure Angiographic Syringe 200ml', N'ITEM-055'),
        (N'ITEM-056', N'350psi, 150cm coil tube with valve', N'ITEM-056');

    INSERT INTO @ExistingItems (ItemCode)
    SELECT i.ItemCode
    FROM dbo.Items i
    INNER JOIN @Items source
        ON source.ItemCode = i.ItemCode;

    IF EXISTS
    (
        SELECT 1
        FROM @Items source
        INNER JOIN dbo.Items existing
            ON existing.PartNumber = source.PartNumber
           AND existing.ItemCode <> source.ItemCode
    )
    BEGIN
        THROW 50001, 'Import aborted because one or more PartNumber values already belong to a different ItemCode in dbo.Items.', 1;
    END;

    UPDATE target
    SET
        target.ItemName = source.ItemName,
        target.PartNumber = source.PartNumber,
        target.ItemType = N'Product',
        target.Unit = N'EA',
        target.TrackStock = 1,
        target.IsSerialControlled = 0,
        target.IsActive = 1
    FROM dbo.Items target
    INNER JOIN @Items source
        ON source.ItemCode = target.ItemCode;

    INSERT INTO dbo.Items
    (
        ItemCode,
        ItemName,
        PartNumber,
        ItemType,
        Unit,
        TrackStock,
        IsSerialControlled,
        UnitPrice,
        CurrentStock,
        IsActive
    )
    SELECT
        i.ItemCode,
        i.ItemName,
        i.PartNumber,
        N'Product',
        N'EA',
        1,
        0,
        0,
        0,
        1
    FROM @Items i
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Items existing
        WHERE existing.ItemCode = i.ItemCode
    )
    ORDER BY i.ItemCode;

    COMMIT TRANSACTION;

    SELECT
        SUM(CASE WHEN existing.ItemCode IS NULL THEN 1 ELSE 0 END) AS InsertedCount,
        SUM(CASE WHEN existing.ItemCode IS NOT NULL THEN 1 ELSE 0 END) AS UpdatedCount,
        MIN(source.ItemCode) AS FirstItemCode,
        MAX(source.ItemCode) AS LastItemCode
    FROM @Items source
    LEFT JOIN @ExistingItems existing
        ON existing.ItemCode = source.ItemCode;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
