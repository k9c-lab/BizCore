/*
    BizCore customer handover cleanup script.

    PURPOSE
    - Clear demo/test transactions and most demo master data before customer installation.
    - Keep only the `admin` login user.
    - Keep system setup tables such as permissions/menu-role mappings so the app can still be configured.

    THIS SCRIPT KEEPS
    - dbo.Users: only Username = 'admin'
    - dbo.Permissions
    - dbo.RolePermissions
    - dbo.Branches

    THIS SCRIPT CLEARS
    - All business transactions
    - Customers, Suppliers, Salespersons, Items, SerialNumbers, stock tables
    - All users except `admin`

    IMPORTANT
    1. Back up the database first.
    2. Run only on the customer handover/demo database.
    3. After running, login with admin and set up master data again.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
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
    IF OBJECT_ID(N'dbo.ReceiptHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.ReceiptHeaders;

    IF OBJECT_ID(N'dbo.PaymentAllocations', N'U') IS NOT NULL
        DELETE FROM dbo.PaymentAllocations;

    IF OBJECT_ID(N'dbo.PaymentHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.PaymentHeaders;

    IF OBJECT_ID(N'dbo.SupplierPaymentHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.SupplierPaymentHeaders;

    /* Sales documents */
    IF OBJECT_ID(N'dbo.InvoiceSerials', N'U') IS NOT NULL
        DELETE FROM dbo.InvoiceSerials;

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

    /* Inventory transactions/state */
    IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NOT NULL
        DELETE FROM dbo.StockMovements;

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

    IF OBJECT_ID(N'dbo.StockBalances', N'U') IS NOT NULL
        DELETE FROM dbo.StockBalances;

    IF OBJECT_ID(N'dbo.SerialNumbers', N'U') IS NOT NULL
        DELETE FROM dbo.SerialNumbers;

    /* Master/demo data to be removed */
    IF OBJECT_ID(N'dbo.Items', N'U') IS NOT NULL
        DELETE FROM dbo.Items;

    IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL
        DELETE FROM dbo.Customers;

    IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NOT NULL
        DELETE FROM dbo.Suppliers;

    IF OBJECT_ID(N'dbo.Salespersons', N'U') IS NOT NULL
        DELETE FROM dbo.Salespersons;

    /* Keep only admin login */
    IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
        DELETE FROM dbo.Users WHERE Username <> N'admin';

    /* Reset item stock in case the table was not cleared for any reason */
    IF COL_LENGTH('dbo.Items', 'CurrentStock') IS NOT NULL
        UPDATE dbo.Items SET CurrentStock = 0;

    /* Reset identities */
    IF OBJECT_ID(N'dbo.CustomerClaimDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CustomerClaimDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.SupplierClaimDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SupplierClaimDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.SupplierClaimHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SupplierClaimHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.CustomerClaimHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CustomerClaimHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.SerialClaimLogs', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SerialClaimLogs', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceiptHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceiptHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.PaymentAllocations', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PaymentAllocations', RESEED, 0);
    IF OBJECT_ID(N'dbo.PaymentHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PaymentHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.SupplierPaymentHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SupplierPaymentHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.InvoiceSerials', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.InvoiceSerials', RESEED, 0);
    IF OBJECT_ID(N'dbo.InvoiceDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.InvoiceDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.InvoiceHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.InvoiceHeaders', RESEED, 0);
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
    IF OBJECT_ID(N'dbo.Items', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Items', RESEED, 0);
    IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Customers', RESEED, 0);
    IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Suppliers', RESEED, 0);
    IF OBJECT_ID(N'dbo.Salespersons', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Salespersons', RESEED, 0);

    COMMIT TRANSACTION;

    PRINT N'Customer handover cleanup completed. Only admin user was kept.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
