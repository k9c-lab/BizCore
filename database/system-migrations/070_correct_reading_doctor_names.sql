IF OBJECT_ID('tempdb..#ReadingDoctorRenameMap') IS NOT NULL
BEGIN
    DROP TABLE #ReadingDoctorRenameMap;
END;
GO

IF OBJECT_ID('tempdb..#ReadingDoctorFinalSeed') IS NOT NULL
BEGIN
    DROP TABLE #ReadingDoctorFinalSeed;
END;
GO

CREATE TABLE #ReadingDoctorRenameMap
(
    OldDoctorName NVARCHAR(200) NOT NULL,
    NewDoctorName NVARCHAR(200) NOT NULL
);
GO

INSERT INTO #ReadingDoctorRenameMap (OldDoctorName, NewDoctorName)
VALUES
    (N'นพ. อภฉัตร มาศเมธาทิพย์', N'นพ. อภิฉัตร มาศเมธาทิพย์'),
    (N'นพ. ธนภู บุญยะสิทธิรณ', N'นพ. ธนุส บุญยะลีพรรณ'),
    (N'นพ. ประพัฒน์ เรืองจุฑารักษ์', N'นพ. ประพัฒน์ เรืองฤทธิ์กุล'),
    (N'พญ. ปวีนภา คะรัมย์', N'พญ. ปวีณกร คะรัมย์'),
    (N'นพ. ชนุตต์ เต็งศิริอารกุล', N'นพ. ชนัตถ์  เต็งศิริอรกุล'),
    (N'พญ. ปวีนญา นามเสนา', N'พญ. ปวิชญา นามเสนา'),
    (N'พญ. ชุติมา คังนธีบุญ', N'พญ.ชุติมา ตั้งนิธิบุญ'),
    (N'พญ. ภารดี ศรีสุใส', N'พญ. ภารณี ศรีสุภะ'),
    (N'พญ. เรวดี วงศ์อำนาจย์', N'พญ. เรวดี วงศ์อามาตย์'),
    (N'พญ. ณภัทร บูรพนาวิบุลย์', N'พญ. ณภัทร บูรพนาวิบูลย์'),
    (N'พญ. วรุณยุภา อุบลลับ', N'พญ. วรุณยุภา อู่ขลิบ'),
    (N'นพ. อาทิตย์ มณีกาญจน์', N'นพ.อาจิณ มณีกาญจน์'),
    (N'พญ. ณัฐธิรา วงศ์วิวิศ', N'พญ ณัฎฐา วงศ์วิชิต'),
    (N'พญ. ปภิธิ เพิ่มศิริวาณิชย์', N'พญ.ปณิธิ เพิ่มศิริวาณิชย์'),
    (N'นพ. อุกฤษณ์ ศิริประเทา', N'นพ. อุกฤษฏ์ ศรีบรรเทา'),
    (N'นพ. บุรุษ ทองขาว', N'นพ นรุตม์ ทองขาว'),
    (N'นพ. อรวพล มียาวรกุล', N'นพ.อรรถพล มีถาวรกุล'),
    (N'พญ. สุภาพิม ปรางค์เจริญ', N'พญ. สุดาพิม ปรางค์เจริญ'),
    (N'พญ. ชูดา ชินพรเจริญพงศ์', N'พญ.ชยุดา  ชินพรเจริญพงศ์'),
    (N'นพ. พชร ตั้งไพศาล', N'นพ.พชร ทั้งไพศาล'),
    (N'นพ. เกื้อ ใสแก้ว', N'นพ.เกื้อ ใสแก้ว');
GO

UPDATE target
SET target.DoctorName = renameMap.NewDoctorName,
    target.IsActive = 1
FROM dbo.ReadingDoctors target
INNER JOIN #ReadingDoctorRenameMap renameMap ON renameMap.OldDoctorName = target.DoctorName
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.ReadingDoctors existing
    WHERE existing.DoctorName = renameMap.NewDoctorName
      AND existing.ReadingDoctorId <> target.ReadingDoctorId
);
GO

UPDATE target
SET target.IsActive = 1
FROM dbo.ReadingDoctors target
INNER JOIN #ReadingDoctorRenameMap renameMap ON renameMap.NewDoctorName = target.DoctorName
WHERE target.IsActive = 0;
GO

CREATE TABLE #ReadingDoctorFinalSeed
(
    SortOrder INT NOT NULL,
    DoctorName NVARCHAR(200) NOT NULL
);
GO

INSERT INTO #ReadingDoctorFinalSeed (SortOrder, DoctorName)
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

DECLARE @NextReadingDoctorSequence INT;

SELECT @NextReadingDoctorSequence = ISNULL(MAX(TRY_CONVERT(INT, RIGHT(DoctorCode, 4))), 0)
FROM dbo.ReadingDoctors
WHERE DoctorCode LIKE N'RDR-[0-9][0-9][0-9][0-9]';

;WITH MissingReadingDoctors AS
(
    SELECT
        seed.DoctorName,
        ROW_NUMBER() OVER (ORDER BY seed.SortOrder) AS RowNumber
    FROM #ReadingDoctorFinalSeed seed
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

DROP TABLE #ReadingDoctorRenameMap;
DROP TABLE #ReadingDoctorFinalSeed;
GO
