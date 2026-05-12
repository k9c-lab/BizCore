/*
    BizCore cleanup script: keep only MAIN branch and admin user.

    PURPOSE
    - Keep only dbo.Branches.BranchCode = 'MAIN'
    - Keep only dbo.Users.Username = 'admin'
    - Re-assign admin to MAIN branch and enable all-branch access

    IMPORTANT
    1. Back up the database first.
    2. Run this after transaction/business data has already been cleared.
       Recommended order:
       - 102_clear_business_data_keep_system_masters.sql
       - 103_keep_only_main_branch_and_admin.sql
    3. If other tables still reference non-MAIN branches, this script will stop
       when deleting extra branches to prevent silent data corruption.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @MainBranchId INT;

    SELECT TOP (1)
        @MainBranchId = BranchId
    FROM dbo.Branches
    WHERE BranchCode = N'MAIN'
    ORDER BY BranchId;

    IF @MainBranchId IS NULL
    BEGIN
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
        VALUES
        (
            N'MAIN',
            N'Main Branch',
            NULL,
            NULL,
            NULL,
            1,
            GETUTCDATE()
        );

        SET @MainBranchId = SCOPE_IDENTITY();
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
    BEGIN
        RAISERROR(N'Cleanup aborted: admin user was not found in dbo.Users.', 16, 1);
    END;

    UPDATE dbo.Users
    SET
        BranchId = @MainBranchId,
        CanAccessAllBranches = 1,
        IsActive = 1
    WHERE Username = N'admin';

    DELETE FROM dbo.Users
    WHERE Username <> N'admin';

    DELETE FROM dbo.Branches
    WHERE BranchId <> @MainBranchId;

    COMMIT TRANSACTION;

    SELECT
        RemainingBranchCount = (SELECT COUNT(*) FROM dbo.Branches),
        RemainingUserCount = (SELECT COUNT(*) FROM dbo.Users),
        MainBranchId = @MainBranchId,
        AdminBranchId = (SELECT TOP (1) BranchId FROM dbo.Users WHERE Username = N'admin'),
        AdminCanAccessAllBranches = (SELECT TOP (1) CanAccessAllBranches FROM dbo.Users WHERE Username = N'admin');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
