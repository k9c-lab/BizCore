SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Customers TABLE
    (
        CustomerCode NVARCHAR(30) NOT NULL,
        CustomerName NVARCHAR(200) NOT NULL,
        TaxId NVARCHAR(30) NULL,
        Address NVARCHAR(500) NULL,
        PhoneNumber NVARCHAR(50) NULL,
        Email NVARCHAR(256) NULL
    );

    INSERT INTO @Customers
    (
        CustomerCode,
        CustomerName,
        TaxId,
        Address,
        PhoneNumber,
        Email
    )
    VALUES
        (N'CUS-0010', N'โรงพยาบาลคลองขลุง', N'0994000494513', N'315 ม.10 ถ.พหลโยธิน ต.คลองขลุง  อ.คลองขลุง  จ. กำแพงเพชร   62120', N'055-781006', NULL),
        (N'CUS-0011', N'โรงพยาบาลโกสัมพีนคร', N'0994000916951', N'458  หมู่ 3   ต.โกสัมพี อ.โกสัมพีนคร จ.กำแพงเพชร  62000', N'055-714081', NULL),
        (N'CUS-0012', N'โรงพยาบาลปางศิลาทอง', N'0994000496095', N'250  หมู่ 4  ต.หินดาต อ.ปางศิลาทอง จ.กำแพงเพชร  62120', N'055-741950', NULL),
        (N'CUS-0013', N'โรงพยาบาลคลองลาน', N'0994000494505', N'9 หมู่ 9 ต. คลองน้ำไหล  อ.คลองลาน  จ.กำแพงเพชร 62180', N'055-786262', NULL),
        (N'CUS-0014', N'โรงพยาบาลทรายทองวัฒนา', N'0994000156022', N'305  หมู่ 1  ต.ทุ่งทราย อ.ทรายทองวัฒนา จ.กำแพงเพชร  62190', N'055-862208', NULL),
        (N'CUS-0015', N'โรงพยาบาลลานกระบือ', N'0994000494483', N'62 หมู่ 6 ต.ลานกระบือ  อ.ลานกระบือ จ.กำแพงเพชร  62170', N'055-769226', NULL),
        (N'CUS-0016', N'โรงพยาบาลไทรงาม', N'0994000137061', N'406 หมู่ 4 ต.ไทรงาม อ.ไทรงาม จ.กำแพงเพชร 62150', N'055-791006', NULL),
        (N'CUS-0017', N'โรงพยาบาลทุ่งโพธิ์ทะเล', N'0994002438858', N'80 หมู่ 15 ต.นิคมทุ่งโพธิ์ทะเล อ. เมืองกำแพงเพชร จ.กำแพงเพชร 62000', N'055-741788', NULL),
        (N'CUS-0018', N'โรงพยาบาลท่าเรือ', N'09940008281101', N'440/1 ถนนเทศบาล 2 ต. ท่าเรือ อ.ท่าเรือ  จ.พระนครศรีอยุธยา 13130', N'035-341300', NULL),
        (N'CUS-0019', N'โรงพยาบาลบึงสามัคคี', N'0994000497539', N'200  หมู่ 7 ต.ระหาน อ.บึงสามัคคี จ.กำแพงเพชร  62210', N'055-871672', NULL),
        (N'CUS-0020', N'โรงพยาบาลขาณุวรลักษบุรี', N'0994000113544', N'340 หมู่ 2 ต. แสนตอ อ.ขาณุวรลักษบุรี   จังหวัดกำแพงเพชร 62130', N'055-779427', NULL),
        (N'CUS-0021', N'โรงพยาบาลกำแพงเพชร', N'0994000494424', N'428 ถนนราชดำเนิน 1 ต. ในเมือง อ.เมืองกำแพงเพชร จ.กำแพงเพชร 62000', N'055-022000', NULL),
        (N'CUS-0022', N'โรงพยาบาลเขาย้อย', N'0994000537841', N'136/2 หมู่ 5 ต.เขาย้อย อ.เขาย้อย จ.เพชรบุรี 76140', N'032-562200', NULL),
        (N'CUS-0023', N'โรงพยาบาลหนองหญ้าปล้อง', N'0994000539215', N'192 หมู่ 11 ต.หนองหญ้าปล้อง  อ.หนองหญ้าปล้อง จังหวัดเพชรบุรี  76160', N'032-494353', NULL),
        (N'CUS-0024', N'โรงพยาบาลบ้านลาด', N'0994000537212', N'131 หมู่ 8 ต.ท่าช้าง  อ.บ้านลาด จ.เพชรบุรี 76150', N'032-491051', NULL),
        (N'CUS-0025', N'โรงพยาบาลบ้านแหลม', N'0994000537387', N'238 หมู่ 3 ต.บ้านแหลม  อ.บ้านแหลม จ.เพชรบุรี 76110', N'032-481144-6', NULL),
        (N'CUS-0026', N'โรงพยาบาลโพนสวรรค์', N'0994000159491', N'276 หมู่ 12 ต.โพนสวรรค์ อ.โพนสวรรค์ จ.นครพนม 48190', N'042-595064', NULL),
        (N'CUS-0027', N'โรงพยาบาลท่าอุเทน', N'0994000882891', N'23/23 หมู่ 6 ต.โนนตาล อ.ท่าอุเทน จ.นครพนม 48120', N'042-503022', NULL),
        (N'CUS-0028', N'โรงพยาบาลนาแก', N'0994000354712', N'75 หมู่ 7 ถนนสกล-นาแก ต.บ้านแก้ง อ.นาแก จ.นครพนม 48130', N'042-571205 , 042-571235', NULL);

    IF EXISTS
    (
        SELECT 1
        FROM @Customers c
        INNER JOIN dbo.Customers d
            ON d.CustomerCode = c.CustomerCode
    )
    BEGIN
        THROW 50021, 'Import aborted because one or more CustomerCode values already exist in dbo.Customers.', 1;
    END;

    INSERT INTO dbo.Customers
    (
        CustomerCode,
        CustomerName,
        TaxId,
        Address,
        PhoneNumber,
        Email,
        CreditLimit,
        IsActive
    )
    SELECT
        c.CustomerCode,
        c.CustomerName,
        c.TaxId,
        c.Address,
        c.PhoneNumber,
        c.Email,
        0,
        1
    FROM @Customers c
    ORDER BY c.CustomerCode;

    COMMIT TRANSACTION;

    SELECT
        COUNT(*) AS InsertedCount,
        MIN(CustomerCode) AS FirstCustomerCode,
        MAX(CustomerCode) AS LastCustomerCode
    FROM @Customers;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
