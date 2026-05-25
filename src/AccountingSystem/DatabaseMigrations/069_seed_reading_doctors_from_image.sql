IF OBJECT_ID('tempdb..#ReadingDoctorSeed') IS NOT NULL
BEGIN
    DROP TABLE #ReadingDoctorSeed;
END;
GO

CREATE TABLE #ReadingDoctorSeed
(
    SortOrder INT NOT NULL,
    DoctorName NVARCHAR(200) NOT NULL
);
GO

INSERT INTO #ReadingDoctorSeed (SortOrder, DoctorName)
VALUES
    (1, N'นพ. อภิฉัตร มาคณาธิพย์'),
    (2, N'นพ. ธนภู บุญยะสิทธิรณ'),
    (3, N'นพ. ประพัฒน์ เรืองจุฑารักษ์'),
    (4, N'พญ. ปวีณภา กะรัมย์'),
    (5, N'นพ. ชนุตต์ เต็งศิริอารกุล'),
    (6, N'พญ. ปวีณญา นามเสนา'),
    (7, N'พญ. ชุติมา คังนธีบุญ'),
    (8, N'พญ. ภารดี ศรีสุใส'),
    (9, N'พญ. เรวดี วงศ์อำนาจย์'),
    (10, N'พญ. แพรวา สนจิน'),
    (11, N'พญ. ณภัทร บูรพนาวิบูลย์'),
    (12, N'พญ. วรุณยุภา อุบลลับ'),
    (13, N'นพ. อาทิตย์ มณีกาญจน์'),
    (14, N'พญ. ณัฐิรา วงศ์วิวิศ'),
    (15, N'พญ. ปภิธี เพิ่มศิริวาณิชย์'),
    (16, N'นพ. อุกฤษฎ์ ศิริประเทา'),
    (17, N'นพ. บุรุษ ทองขาว'),
    (18, N'นพ. อรวพล มียาวรกุล'),
    (19, N'พญ. สุภาพิม ปรางค์เจริญ'),
    (20, N'พญ. ชูดา ชินพรเจริญพงศ์'),
    (21, N'นพ. พชร ตั้งไพศาล'),
    (22, N'นพ. เกื้อ ใสแก้ว');
GO

UPDATE target
SET IsActive = 1
FROM dbo.ReadingDoctors target
INNER JOIN #ReadingDoctorSeed seed ON seed.DoctorName = target.DoctorName
WHERE target.IsActive = 0;
GO

DECLARE @NextReadingDoctorSequence INT;

SELECT @NextReadingDoctorSequence = ISNULL(MAX(TRY_CONVERT(INT, RIGHT(DoctorCode, 4))), 0)
FROM dbo.ReadingDoctors
WHERE DoctorCode LIKE N'RDR-[0-9][0-9][0-9][0-9]';

;WITH MissingReadingDoctors AS
(
    SELECT
        seed.DoctorName,
        ROW_NUMBER() OVER (ORDER BY seed.SortOrder) AS RowNumber
    FROM #ReadingDoctorSeed seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.ReadingDoctors existing
        WHERE existing.DoctorName = seed.DoctorName
    )
)
INSERT INTO dbo.ReadingDoctors (DoctorCode, DoctorName, IsActive)
SELECT
    CONCAT(N'RDR-', RIGHT(CONCAT(N'0000', @NextReadingDoctorSequence + RowNumber), 4)),
    DoctorName,
    1
FROM MissingReadingDoctors;
GO

DROP TABLE #ReadingDoctorSeed;
GO
