/*
    BizCore branch seed script.

    PURPOSE
    - Add the requested customer branches.
    - Safe to run more than once.
    - Existing branches are matched by BranchCode.
    - Uses the standard branch code pattern `BR-000x`.
    - Keeps the existing `MAIN` branch unchanged because current system scripts
      still reference `MAIN` explicitly.

    BRANCHES
    - ขาณุ
    - คลองขลุง
    - เขาย้อย
    - โพนสวรรค์
    - ท่าเรือ
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @Branches TABLE
    (
        BranchCode NVARCHAR(30) NOT NULL,
        BranchName NVARCHAR(150) NOT NULL,
        Address NVARCHAR(500) NULL,
        PhoneNumber NVARCHAR(50) NULL,
        Email NVARCHAR(256) NULL
    );

    INSERT INTO @Branches
    (
        BranchCode,
        BranchName,
        Address,
        PhoneNumber,
        Email
    )
    VALUES
        (N'BR-0002', N'ขาณุ', NULL, NULL, NULL),
        (N'BR-0003', N'คลองขลุง', NULL, NULL, NULL),
        (N'BR-0004', N'เขาย้อย', NULL, NULL, NULL),
        (N'BR-0005', N'โพนสวรรค์', NULL, NULL, NULL),
        (N'BR-0006', N'ท่าเรือ', NULL, NULL, NULL);

    UPDATE target
    SET
        target.BranchName = source.BranchName,
        target.Address = source.Address,
        target.PhoneNumber = source.PhoneNumber,
        target.Email = source.Email,
        target.IsActive = 1
    FROM dbo.Branches target
    INNER JOIN @Branches source
        ON source.BranchCode = target.BranchCode;

    INSERT INTO dbo.Branches
    (
        BranchCode,
        BranchName,
        Address,
        PhoneNumber,
        Email,
        IsActive,
        CreatedDate
    )
    SELECT
        source.BranchCode,
        source.BranchName,
        source.Address,
        source.PhoneNumber,
        source.Email,
        1,
        GETUTCDATE()
    FROM @Branches source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Branches existing
        WHERE existing.BranchCode = source.BranchCode
    )
    ORDER BY source.BranchCode;

    COMMIT TRANSACTION;

    SELECT
        COUNT(*) AS RequestedCount,
        SUM(CASE WHEN existing.BranchId IS NULL THEN 1 ELSE 0 END) AS InsertedCount,
        SUM(CASE WHEN existing.BranchId IS NOT NULL THEN 1 ELSE 0 END) AS UpdatedCount
    FROM @Branches source
    LEFT JOIN dbo.Branches existing
        ON existing.BranchCode = source.BranchCode;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
