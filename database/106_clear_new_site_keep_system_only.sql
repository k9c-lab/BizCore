/*
    BizCore new-site cleanup script: keep only core system setup.

    PURPOSE
    - Prepare a database for a brand-new site/customer installation.
    - Remove all business transactions and customer-specific masters.
    - Keep only minimal system/setup data so the app can boot and be configured.
    - Keep only dbo.Branches.BranchCode = 'MAIN'
    - Keep only dbo.Users.Username = 'admin'

    THIS SCRIPT KEEPS
    - dbo.Users: only Username = 'admin'
    - dbo.Permissions
    - dbo.RolePermissions
    - dbo.Branches: only BranchCode = 'MAIN'
    - dbo.SystemSettings
    - dbo.AppliedScripts
    - dbo.PriceLevels

    THIS SCRIPT CLEARS
    - Announcements
    - All sales / purchasing / finance / inventory / claim documents
    - Customers, Suppliers, Salespersons
    - Items, ItemPrices, SerialNumbers, StockBalances, StockMovements
    - TreatmentRights
    - ReferringDoctors
    - ReadingDoctors
    - All users except `admin`
    - All branches except `MAIN`

    IMPORTANT
    1. Back up the database first.
    2. Run only on the handover/new-site database.
    3. After running, set up customer-specific masters again as needed.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @MainBranchId INT;
    DECLARE @AdminUserId INT;

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

    SELECT TOP (1)
        @AdminUserId = UserId
    FROM dbo.Users
    WHERE Username = N'admin'
    ORDER BY UserId;

    IF @AdminUserId IS NULL
    BEGIN
        RAISERROR(N'Cleanup aborted: admin user was not found in dbo.Users.', 16, 1);
    END;

    /* Warranty / claim documents */
    IF OBJECT_ID(N'dbo.SupplierClaimDetails', N'U') IS NOT NULL
        DELETE FROM dbo.SupplierClaimDetails;

    IF OBJECT_ID(N'dbo.SupplierClaimHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.SupplierClaimHeaders;

    IF OBJECT_ID(N'dbo.CustomerClaimDetails', N'U') IS NOT NULL
        DELETE FROM dbo.CustomerClaimDetails;

    IF OBJECT_ID(N'dbo.SerialClaimLogs', N'U') IS NOT NULL
        DELETE FROM dbo.SerialClaimLogs;

    IF OBJECT_ID(N'dbo.CustomerClaimHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.CustomerClaimHeaders;

    /* Finance documents */
    IF OBJECT_ID(N'dbo.ReceiptPrintLines', N'U') IS NOT NULL
        DELETE FROM dbo.ReceiptPrintLines;

    IF OBJECT_ID(N'dbo.ReceiptHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.ReceiptHeaders;

    IF OBJECT_ID(N'dbo.PaymentAllocations', N'U') IS NOT NULL
        DELETE FROM dbo.PaymentAllocations;

    IF OBJECT_ID(N'dbo.PaymentHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.PaymentHeaders;

    IF OBJECT_ID(N'dbo.SupplierPaymentHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.SupplierPaymentHeaders;

    /* Billing note documents */
    IF OBJECT_ID(N'dbo.BillingNoteLines', N'U') IS NOT NULL
        DELETE FROM dbo.BillingNoteLines;

    IF OBJECT_ID(N'dbo.BillingNoteInvoices', N'U') IS NOT NULL
        DELETE FROM dbo.BillingNoteInvoices;

    IF OBJECT_ID(N'dbo.BillingNoteHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.BillingNoteHeaders;

    /* Sales documents */
    IF OBJECT_ID(N'dbo.InvoiceSerials', N'U') IS NOT NULL
        DELETE FROM dbo.InvoiceSerials;

    IF OBJECT_ID(N'dbo.CashSaleSerials', N'U') IS NOT NULL
        DELETE FROM dbo.CashSaleSerials;

    IF OBJECT_ID(N'dbo.CashSaleDetails', N'U') IS NOT NULL
        DELETE FROM dbo.CashSaleDetails;

    IF OBJECT_ID(N'dbo.CashSaleHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.CashSaleHeaders;

    IF OBJECT_ID(N'dbo.InvoiceDetails', N'U') IS NOT NULL
        DELETE FROM dbo.InvoiceDetails;

    IF OBJECT_ID(N'dbo.InvoiceHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.InvoiceHeaders;

    IF OBJECT_ID(N'dbo.QuotationDetails', N'U') IS NOT NULL
        DELETE FROM dbo.QuotationDetails;

    IF OBJECT_ID(N'dbo.QuotationHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.QuotationHeaders;

    /* Purchasing / receiving documents */
    IF OBJECT_ID(N'dbo.ReceivingSerials', N'U') IS NOT NULL
        DELETE FROM dbo.ReceivingSerials;

    IF OBJECT_ID(N'dbo.ReceivingDetails', N'U') IS NOT NULL
        DELETE FROM dbo.ReceivingDetails;

    IF OBJECT_ID(N'dbo.ReceivingHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.ReceivingHeaders;

    IF OBJECT_ID(N'dbo.PurchaseOrderAllocationSources', N'U') IS NOT NULL
        DELETE FROM dbo.PurchaseOrderAllocationSources;

    IF OBJECT_ID(N'dbo.PurchaseOrderAllocations', N'U') IS NOT NULL
        DELETE FROM dbo.PurchaseOrderAllocations;

    IF OBJECT_ID(N'dbo.PurchaseOrderDetails', N'U') IS NOT NULL
        DELETE FROM dbo.PurchaseOrderDetails;

    IF OBJECT_ID(N'dbo.PurchaseOrderHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.PurchaseOrderHeaders;

    IF OBJECT_ID(N'dbo.PurchaseRequestDetails', N'U') IS NOT NULL
        DELETE FROM dbo.PurchaseRequestDetails;

    IF OBJECT_ID(N'dbo.PurchaseRequestHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.PurchaseRequestHeaders;

    /* Inventory state */
    IF OBJECT_ID(N'dbo.StockTransferSerials', N'U') IS NOT NULL
        DELETE FROM dbo.StockTransferSerials;

    IF OBJECT_ID(N'dbo.StockTransferDetails', N'U') IS NOT NULL
        DELETE FROM dbo.StockTransferDetails;

    IF OBJECT_ID(N'dbo.StockTransferHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.StockTransferHeaders;

    IF OBJECT_ID(N'dbo.StockIssueSerials', N'U') IS NOT NULL
        DELETE FROM dbo.StockIssueSerials;

    IF OBJECT_ID(N'dbo.StockIssueDetails', N'U') IS NOT NULL
        DELETE FROM dbo.StockIssueDetails;

    IF OBJECT_ID(N'dbo.StockIssueHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.StockIssueHeaders;

    IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NOT NULL
        DELETE FROM dbo.StockMovements;

    IF OBJECT_ID(N'dbo.StockBalances', N'U') IS NOT NULL
        DELETE FROM dbo.StockBalances;

    IF OBJECT_ID(N'dbo.SerialNumbers', N'U') IS NOT NULL
        DELETE FROM dbo.SerialNumbers;

    /* Non-essential setup/demo content */
    IF OBJECT_ID(N'dbo.Announcements', N'U') IS NOT NULL
        DELETE FROM dbo.Announcements;

    /* Re-point kept setup rows to admin before user cleanup */
    IF COL_LENGTH(N'dbo.SystemSettings', N'UpdatedByUserId') IS NOT NULL
        UPDATE dbo.SystemSettings
        SET UpdatedByUserId = @AdminUserId
        WHERE UpdatedByUserId IS NOT NULL
          AND UpdatedByUserId <> @AdminUserId;

    /* Customer-specific / healthcare-specific master data */
    IF OBJECT_ID(N'dbo.ItemPrices', N'U') IS NOT NULL
        DELETE FROM dbo.ItemPrices;

    IF OBJECT_ID(N'dbo.Items', N'U') IS NOT NULL
        DELETE FROM dbo.Items;

    IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL
        DELETE FROM dbo.Customers;

    IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NOT NULL
        DELETE FROM dbo.Suppliers;

    IF OBJECT_ID(N'dbo.Salespersons', N'U') IS NOT NULL
        DELETE FROM dbo.Salespersons;

    IF OBJECT_ID(N'dbo.ReferringDoctors', N'U') IS NOT NULL
        DELETE FROM dbo.ReferringDoctors;

    IF OBJECT_ID(N'dbo.ReadingDoctors', N'U') IS NOT NULL
        DELETE FROM dbo.ReadingDoctors;

    IF OBJECT_ID(N'dbo.TreatmentRights', N'U') IS NOT NULL
        DELETE FROM dbo.TreatmentRights;

    /* Keep only admin and MAIN */
    UPDATE dbo.Users
    SET
        BranchId = @MainBranchId,
        CanAccessAllBranches = 1,
        IsActive = 1
    WHERE UserId = @AdminUserId;

    DELETE FROM dbo.Users
    WHERE UserId <> @AdminUserId;

    DELETE FROM dbo.Branches
    WHERE BranchId <> @MainBranchId;

    /* Reset identities for cleared tables */
    IF OBJECT_ID(N'dbo.SupplierClaimDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SupplierClaimDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.SupplierClaimHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SupplierClaimHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.CustomerClaimDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CustomerClaimDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.CustomerClaimHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CustomerClaimHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.SerialClaimLogs', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SerialClaimLogs', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceiptPrintLines', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceiptPrintLines', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceiptHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceiptHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.PaymentAllocations', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PaymentAllocations', RESEED, 0);
    IF OBJECT_ID(N'dbo.PaymentHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PaymentHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.SupplierPaymentHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SupplierPaymentHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.BillingNoteLines', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.BillingNoteLines', RESEED, 0);
    IF OBJECT_ID(N'dbo.BillingNoteInvoices', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.BillingNoteInvoices', RESEED, 0);
    IF OBJECT_ID(N'dbo.BillingNoteHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.BillingNoteHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.InvoiceSerials', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.InvoiceSerials', RESEED, 0);
    IF OBJECT_ID(N'dbo.InvoiceDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.InvoiceDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.InvoiceHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.InvoiceHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.CashSaleSerials', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CashSaleSerials', RESEED, 0);
    IF OBJECT_ID(N'dbo.CashSaleDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CashSaleDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.CashSaleHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CashSaleHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.QuotationDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.QuotationDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.QuotationHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.QuotationHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceivingSerials', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceivingSerials', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceivingDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceivingDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceivingHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceivingHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.PurchaseOrderAllocationSources', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PurchaseOrderAllocationSources', RESEED, 0);
    IF OBJECT_ID(N'dbo.PurchaseOrderAllocations', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PurchaseOrderAllocations', RESEED, 0);
    IF OBJECT_ID(N'dbo.PurchaseOrderDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PurchaseOrderDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.PurchaseOrderHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PurchaseOrderHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.PurchaseRequestDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PurchaseRequestDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.PurchaseRequestHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PurchaseRequestHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.StockTransferSerials', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.StockTransferSerials', RESEED, 0);
    IF OBJECT_ID(N'dbo.StockTransferDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.StockTransferDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.StockTransferHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.StockTransferHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.StockIssueSerials', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.StockIssueSerials', RESEED, 0);
    IF OBJECT_ID(N'dbo.StockIssueDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.StockIssueDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.StockIssueHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.StockIssueHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.StockMovements', RESEED, 0);
    IF OBJECT_ID(N'dbo.StockBalances', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.StockBalances', RESEED, 0);
    IF OBJECT_ID(N'dbo.SerialNumbers', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SerialNumbers', RESEED, 0);
    IF OBJECT_ID(N'dbo.ItemPrices', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ItemPrices', RESEED, 0);
    IF OBJECT_ID(N'dbo.Items', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Items', RESEED, 0);
    IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Customers', RESEED, 0);
    IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Suppliers', RESEED, 0);
    IF OBJECT_ID(N'dbo.Salespersons', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Salespersons', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReferringDoctors', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReferringDoctors', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReadingDoctors', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReadingDoctors', RESEED, 0);
    IF OBJECT_ID(N'dbo.TreatmentRights', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.TreatmentRights', RESEED, 0);
    IF OBJECT_ID(N'dbo.Announcements', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Announcements', RESEED, 0);

    COMMIT TRANSACTION;

    SELECT
        RemainingBranchCount = (SELECT COUNT(*) FROM dbo.Branches),
        RemainingUserCount = (SELECT COUNT(*) FROM dbo.Users),
        RemainingPriceLevelCount = CASE WHEN OBJECT_ID(N'dbo.PriceLevels', N'U') IS NOT NULL THEN (SELECT COUNT(*) FROM dbo.PriceLevels) ELSE 0 END,
        RemainingSystemSettingCount = CASE WHEN OBJECT_ID(N'dbo.SystemSettings', N'U') IS NOT NULL THEN (SELECT COUNT(*) FROM dbo.SystemSettings) ELSE 0 END,
        RemainingTreatmentRightCount = CASE WHEN OBJECT_ID(N'dbo.TreatmentRights', N'U') IS NOT NULL THEN (SELECT COUNT(*) FROM dbo.TreatmentRights) ELSE 0 END,
        RemainingReferringDoctorCount = CASE WHEN OBJECT_ID(N'dbo.ReferringDoctors', N'U') IS NOT NULL THEN (SELECT COUNT(*) FROM dbo.ReferringDoctors) ELSE 0 END,
        RemainingReadingDoctorCount = CASE WHEN OBJECT_ID(N'dbo.ReadingDoctors', N'U') IS NOT NULL THEN (SELECT COUNT(*) FROM dbo.ReadingDoctors) ELSE 0 END,
        MainBranchId = @MainBranchId,
        AdminUserId = @AdminUserId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
