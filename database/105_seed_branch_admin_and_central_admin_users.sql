/*
    BizCore user seed script.

    PURPOSE
    - Add/update the requested users.
    - Users mapped to an existing branch by exact branch name become BranchAdmin.
    - Users that cannot be mapped become CentralAdmin.
    - Safe to run more than once.

    DEFAULT PASSWORD
    - This script uses the same default password hash as the admin reset script.
    - Plain-text password: Admin@12345
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @DefaultPasswordHash NVARCHAR(300) =
        N'PBKDF2-SHA256$100000$Qml6Q29yZUFkbWluU2FsdDEyMzQ1Ng==$q5v2GZ/ztGXmJmXF0qB/yRR3hvZIWG+Yh2oxi9GMapI=';

    DECLARE @MainBranchId INT = NULL;

    SELECT TOP (1)
        @MainBranchId = BranchId
    FROM dbo.Branches
    WHERE BranchCode = N'MAIN'
    ORDER BY BranchId;

    DECLARE @Users TABLE
    (
        Username NVARCHAR(50) NOT NULL,
        DisplayName NVARCHAR(150) NOT NULL,
        Email NVARCHAR(256) NULL,
        RequestedBranchName NVARCHAR(150) NULL
    );

    INSERT INTO @Users
    (
        Username,
        DisplayName,
        Email,
        RequestedBranchName
    )
    VALUES
        (N'chanpen', N'chanpen', N'chanpen@bizcore.local', NULL),
        (N'GAN', N'การัน', N'GAN@bizcore.local', NULL),
        (N'jira', N'จิรา', N'cadmin@bizcore.local', NULL),
        (N'KAN', N'ขาณุ', N'KAN@bizcore.local', N'ขาณุ'),
        (N'KKL', N'คลองลึก', N'KKL@bizcore.local', N'คลองลึก'),
        (N'KYO', N'เขาย้อย', N'KYO@bizcore.local', N'เขาย้อย'),
        (N'Phawaran', N'Phawaran', N'Phawaran@bizcore.local', NULL),
        (N'PSW', N'โพนสวรรค์', N'PSW@bizcore.local', N'โพนสวรรค์'),
        (N'TRU', N'ท่าเรือ', N'TRU@bizcore.local', N'ท่าเรือ'),
        (N'tu', N'ตู่', N'cadmin2@bizcore.local', NULL);

    DECLARE @ResolvedUsers TABLE
    (
        Username NVARCHAR(50) NOT NULL PRIMARY KEY,
        DisplayName NVARCHAR(150) NOT NULL,
        Email NVARCHAR(256) NULL,
        RoleName NVARCHAR(30) NOT NULL,
        BranchId INT NULL,
        CanAccessAllBranches BIT NOT NULL
    );

    INSERT INTO @ResolvedUsers
    (
        Username,
        DisplayName,
        Email,
        RoleName,
        BranchId,
        CanAccessAllBranches
    )
    SELECT
        source.Username,
        source.DisplayName,
        source.Email,
        CASE
            WHEN branch.BranchId IS NOT NULL THEN N'BranchAdmin'
            ELSE N'CentralAdmin'
        END AS RoleName,
        CASE
            WHEN branch.BranchId IS NOT NULL THEN branch.BranchId
            ELSE @MainBranchId
        END AS BranchId,
        CASE
            WHEN branch.BranchId IS NOT NULL THEN CAST(0 AS BIT)
            ELSE CAST(1 AS BIT)
        END AS CanAccessAllBranches
    FROM @Users source
    LEFT JOIN dbo.Branches branch
        ON branch.BranchName = source.RequestedBranchName;

    UPDATE target
    SET
        target.DisplayName = source.DisplayName,
        target.Email = source.Email,
        target.Role = source.RoleName,
        target.IsActive = 1,
        target.PasswordHash = COALESCE(NULLIF(target.PasswordHash, N''), @DefaultPasswordHash),
        target.BranchId = source.BranchId,
        target.CanAccessAllBranches = source.CanAccessAllBranches
    FROM dbo.Users target
    INNER JOIN @ResolvedUsers source
        ON source.Username = target.Username;

    INSERT INTO dbo.Users
    (
        Username,
        DisplayName,
        Email,
        PasswordHash,
        Role,
        BranchId,
        CanAccessAllBranches,
        IsActive
    )
    SELECT
        source.Username,
        source.DisplayName,
        source.Email,
        @DefaultPasswordHash,
        source.RoleName,
        source.BranchId,
        source.CanAccessAllBranches,
        1
    FROM @ResolvedUsers source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users existing
        WHERE existing.Username = source.Username
    )
    ORDER BY source.Username;

    COMMIT TRANSACTION;

    SELECT
        u.Username,
        u.DisplayName,
        u.Email,
        u.Role,
        u.CanAccessAllBranches,
        b.BranchCode,
        b.BranchName
    FROM dbo.Users u
    LEFT JOIN dbo.Branches b
        ON b.BranchId = u.BranchId
    WHERE u.Username IN
    (
        N'chanpen',
        N'GAN',
        N'jira',
        N'KAN',
        N'KKL',
        N'KYO',
        N'Phawaran',
        N'PSW',
        N'TRU',
        N'tu'
    )
    ORDER BY u.Username;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
