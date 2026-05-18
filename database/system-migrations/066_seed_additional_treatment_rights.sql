IF OBJECT_ID('tempdb..#TreatmentRightSeed') IS NOT NULL
BEGIN
    DROP TABLE #TreatmentRightSeed;
END;
GO

CREATE TABLE #TreatmentRightSeed
(
    SortOrder INT NOT NULL,
    TreatmentRightName NVARCHAR(200) NOT NULL
);
GO

INSERT INTO #TreatmentRightSeed (SortOrder, TreatmentRightName)
VALUES
    (1, N'เบิกได้ DRG'),
    (2, N'พรบ.รถ'),
    (3, N'ชำระเงินเอง'),
    (4, N'ประกันสังคม'),
    (5, N'บัตรทองต่างด้าว'),
    (6, N'ผู้มีปัญหาสถานะทางสิทธิ์');

UPDATE target
SET IsActive = 1
FROM dbo.TreatmentRights target
INNER JOIN #TreatmentRightSeed seed ON seed.TreatmentRightName = target.TreatmentRightName
WHERE target.IsActive = 0;
GO

DECLARE @NextTreatmentRightSequence INT;

SELECT @NextTreatmentRightSequence = ISNULL(MAX(TRY_CONVERT(INT, RIGHT(TreatmentRightCode, 4))), 0)
FROM dbo.TreatmentRights
WHERE TreatmentRightCode LIKE N'TRT-[0-9][0-9][0-9][0-9]';

;WITH MissingTreatmentRights AS
(
    SELECT
        seed.TreatmentRightName,
        ROW_NUMBER() OVER (ORDER BY seed.SortOrder) AS RowNumber
    FROM #TreatmentRightSeed seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.TreatmentRights existing
        WHERE existing.TreatmentRightName = seed.TreatmentRightName
    )
)
INSERT INTO dbo.TreatmentRights (TreatmentRightCode, TreatmentRightName, IsActive)
SELECT
    CONCAT(N'TRT-', RIGHT(CONCAT(N'0000', @NextTreatmentRightSequence + RowNumber), 4)),
    TreatmentRightName,
    1
FROM MissingTreatmentRights;
GO

DROP TABLE #TreatmentRightSeed;
GO
