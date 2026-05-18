IF OBJECT_ID('tempdb..#ServiceItemSeed') IS NOT NULL
BEGIN
    DROP TABLE #ServiceItemSeed;
END;
GO

CREATE TABLE #ServiceItemSeed
(
    SortOrder INT NOT NULL,
    PartNumber NVARCHAR(80) NOT NULL,
    ItemName NVARCHAR(200) NOT NULL,
    Unit NVARCHAR(20) NOT NULL
);
GO

INSERT INTO #ServiceItemSeed (SortOrder, PartNumber, ItemName, Unit)
VALUES
    (1, N'44020', N'CT Fistulography', N'ครั้ง'),
    (2, N'44101', N'CT Brain without contrast study', N'ครั้ง'),
    (3, N'44102', N'CT Brain with contrast study', N'ครั้ง'),
    (4, N'44103', N'CTA: Brain', N'ครั้ง'),
    (5, N'44105', N'CTV: Brain', N'ครั้ง'),
    (6, N'44143', N'CT Spine: Cervical', N'ครั้ง'),
    (7, N'44144', N'CT Spine: Thoracic', N'ครั้ง'),
    (8, N'44146', N'CT Spine: Lumbosacral', N'ครั้ง'),
    (9, N'44201', N'CT Facial bone', N'ครั้ง'),
    (10, N'44210', N'CT Orbits', N'ครั้ง'),
    (11, N'44220', N'CT Temporal bone (including internal acoustic canals)', N'ครั้ง'),
    (12, N'44232', N'CT PNS screening', N'ครั้ง'),
    (13, N'44233', N'CT Paranasal sinuses without contrast', N'ครั้ง'),
    (14, N'44234', N'CT Paranasal sinuses with contrast', N'ครั้ง'),
    (15, N'44241', N'CT Dental scan - maxilla', N'ครั้ง'),
    (16, N'44242', N'CT Dental scan - mandible', N'ครั้ง'),
    (17, N'44250', N'CT Neck', N'ครั้ง'),
    (18, N'44251', N'CTA: Neck', N'ครั้ง'),
    (19, N'44253', N'CTV: Neck', N'ครั้ง'),
    (20, N'44260', N'CT Larynx (or CT Vocal cord paralysis)', N'ครั้ง'),
    (21, N'44301', N'CT Chest with contrast', N'ครั้ง'),
    (22, N'44302', N'High resolution CT chest (HRCT)', N'ครั้ง'),
    (23, N'44303', N'CT Chest without contrast', N'ครั้ง'),
    (24, N'44310', N'CTA: Chest', N'ครั้ง'),
    (25, N'44311', N'CTA: Pulmonary artery', N'ครั้ง'),
    (26, N'44312', N'CTV: Chest', N'ครั้ง'),
    (27, N'44402', N'CTA Coronary arteries', N'ครั้ง'),
    (28, N'44404', N'CT Cardiac function', N'ครั้ง'),
    (29, N'44405', N'CT Coronary calcium score', N'ครั้ง'),
    (30, N'44422', N'CTA: Thoracic aorta', N'ครั้ง'),
    (31, N'44423', N'CTA: Abdominal aorta', N'ครั้ง'),
    (32, N'44501', N'CT Upper abdomen', N'ครั้ง'),
    (33, N'44502', N'CT Lower abdomen', N'ครั้ง'),
    (34, N'44503', N'CT Whole abdomen', N'ครั้ง'),
    (35, N'44505', N'CTV: Abdomen', N'ครั้ง'),
    (36, N'44508', N'CT Peritoneography', N'ครั้ง'),
    (37, N'44510', N'CTA: Liver donor', N'ครั้ง'),
    (38, N'44531', N'CT Enterography', N'ครั้ง'),
    (39, N'44532', N'CT Colonography', N'ครั้ง'),
    (40, N'44602', N'CT Urinary tract (or KUB)', N'ครั้ง'),
    (41, N'44603', N'CTA: Pelvis', N'ครั้ง'),
    (42, N'44611', N'CTA: Renal arteries', N'ครั้ง'),
    (43, N'44620', N'CT Cystography', N'ครั้ง'),
    (44, N'44720', N'CT Shoulder joint (1side = 1 part)', N'ครั้ง'),
    (45, N'44721', N'CT Arm (1 side = 1 part)', N'ครั้ง'),
    (46, N'44722', N'CT Elbow joint (1 side = 1 part)', N'ครั้ง'),
    (47, N'44723', N'CT Forearm (1 side = 1 part)', N'ครั้ง'),
    (48, N'44724', N'CT Wrist joint (1 side = 1 part)', N'ครั้ง'),
    (49, N'44725', N'CT Hand (1 side = 1 part)', N'ครั้ง'),
    (50, N'44726', N'CT Arthrography: Soulder Joint (1 side = 1 part)', N'ครั้ง'),
    (51, N'44727', N'CT Arthrography: Eobow joint (1 side = 1 part)', N'ครั้ง'),
    (52, N'44728', N'CT Arthrography: Wrist joint (1 side = 1 part)', N'ครั้ง'),
    (53, N'44750', N'CTA: Upper extremities (peripheral runoff)', N'ครั้ง'),
    (54, N'44751', N'CTV: Upper extremities', N'ครั้ง'),
    (55, N'44760', N'CTA Lower extremities (peripheral runoff)', N'ครั้ง'),
    (56, N'44761', N'CTV: Lower extremities', N'ครั้ง'),
    (57, N'44780', N'CT Hip joint (1 side = 1 part)', N'ครั้ง'),
    (58, N'44781', N'CT Thigh (1 side = 1 part)', N'ครั้ง'),
    (59, N'44782', N'CT Knee joint (1 side = 1 part)', N'ครั้ง'),
    (60, N'44783', N'CT Leg (1 side = 1 part)', N'ครั้ง'),
    (61, N'44784', N'CT Ankle joint (1 side = 1 part)', N'ครั้ง'),
    (62, N'44785', N'CT Foot (1 side = 1 part)', N'ครั้ง'),
    (63, N'44786', N'CT Arthrography: Hip joint (1 side = 1 part)', N'ครั้ง'),
    (64, N'44787', N'CT Arthrography: Knee joint (1 side = 1 part)', N'ครั้ง'),
    (65, N'44788', N'CT Arthrography: Ankle joint (1 side = 1 part)', N'ครั้ง'),
    (66, N'44901', N'Using non-ionic contrast media', N'50 ml'),
    (67, N'44910', N'Biopsy under CT guidance', N'ครั้ง');
GO

UPDATE target
SET
    target.ItemName = seed.ItemName,
    target.Unit = seed.Unit,
    target.ItemType = N'Service',
    target.TrackStock = 0,
    target.IsSerialControlled = 0,
    target.CurrentStock = 0,
    target.IsActive = 1
FROM dbo.Items target
INNER JOIN #ServiceItemSeed seed ON seed.PartNumber = target.PartNumber;
GO

DECLARE @NextServiceSequence INT;

SELECT @NextServiceSequence = ISNULL(MAX(TRY_CONVERT(INT, RIGHT(ItemCode, 4))), 0)
FROM dbo.Items
WHERE ItemCode LIKE N'SRV-[0-9][0-9][0-9][0-9]';

;WITH MissingServiceItems AS
(
    SELECT
        seed.PartNumber,
        seed.ItemName,
        seed.Unit,
        ROW_NUMBER() OVER (ORDER BY seed.SortOrder) AS RowNumber
    FROM #ServiceItemSeed seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Items existing
        WHERE existing.PartNumber = seed.PartNumber
    )
)
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
    CONCAT(N'SRV-', RIGHT(CONCAT(N'0000', @NextServiceSequence + RowNumber), 4)),
    ItemName,
    PartNumber,
    N'Service',
    Unit,
    0,
    0,
    0,
    0,
    1
FROM MissingServiceItems;
GO

DROP TABLE #ServiceItemSeed;
GO
