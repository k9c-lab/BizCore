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
    (1, N'นพ. อภิฉัตร มาศเมธาทิพย์'),
    (2, N'นพ. ธนุส บุญยะลีพรรณ'),
    (3, N'นพ. ประพัฒน์ เรืองฤทธิ์กุล'),
    (4, N'พญ. ปวีณกร คะรัมย์'),
    (5, N'นพ. ชนัตถ์  เต็งศิริอรกุล'),
    (6, N'พญ. ปวิชญา นามเสนา'),
    (7, N'พญ.ชุติมา ตั้งนิธิบุญ'),
    (8, N'พญ. ภารณี ศรีสุภะ'),
    (9, N'พญ. เรวดี วงศ์อามาตย์'),
    (10, N'พญ. แพรวา สนจีน'),
    (11, N'พญ. ณภัทร บูรพนาวิบูลย์'),
    (12, N'พญ. วรุณยุภา อู่ขลิบ'),
    (13, N'นพ.อาจิณ มณีกาญจน์'),
    (14, N'พญ ณัฎฐา วงศ์วิชิต'),
    (15, N'พญ.ปณิธิ เพิ่มศิริวาณิชย์'),
    (16, N'นพ. อุกฤษฏ์ ศรีบรรเทา'),
    (17, N'นพ นรุตม์ ทองขาว'),
    (18, N'นพ.อรรถพล มีถาวรกุล'),
    (19, N'พญ. สุดาพิม ปรางค์เจริญ'),
    (20, N'พญ.ชยุดา  ชินพรเจริญพงศ์'),
    (21, N'นพ.พชร ทั้งไพศาล'),
    (22, N'นพ.เกื้อ ใสแก้ว'),
    (23, N'พญ. วชิรา ดวงแก้ว');
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
