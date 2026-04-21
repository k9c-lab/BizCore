/*
    BizCore demo/customer trial cleanup script.

    WARNING:
    - This script deletes business/test data.
    - It keeps dbo.Users so the system can still be logged in after cleanup.
    - It deletes master data too:
      Customers, Suppliers, Salespersons, Items, SerialNumbers.
    - Run only on a demo/trial database, not production.

    Recommended use:
    1. Back up the database first.
    2. Run this script.
    3. Re-seed only the master data you want the customer to try.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    /* Warranty / claim documents */
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

    /* Sales documents */
    IF OBJECT_ID(N'dbo.InvoiceSerials', N'U') IS NOT NULL
        DELETE FROM dbo.InvoiceSerials;

    IF OBJECT_ID(N'dbo.InvoiceDetails', N'U') IS NOT NULL
        DELETE FROM dbo.InvoiceDetails;

    /*
        SerialNumbers can reference InvoiceHeaders.
        Delete serial stock before invoice headers and before item master cleanup.
    */
    IF OBJECT_ID(N'dbo.SerialNumbers', N'U') IS NOT NULL
        DELETE FROM dbo.SerialNumbers;

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

    IF OBJECT_ID(N'dbo.PurchaseOrderDetails', N'U') IS NOT NULL
        DELETE FROM dbo.PurchaseOrderDetails;

    IF OBJECT_ID(N'dbo.PurchaseOrderHeaders', N'U') IS NOT NULL
        DELETE FROM dbo.PurchaseOrderHeaders;

    /* Master data requested to be cleared */
    IF OBJECT_ID(N'dbo.Items', N'U') IS NOT NULL
        DELETE FROM dbo.Items;

    IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL
        DELETE FROM dbo.Customers;

    IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NOT NULL
        DELETE FROM dbo.Suppliers;

    IF OBJECT_ID(N'dbo.Salespersons', N'U') IS NOT NULL
        DELETE FROM dbo.Salespersons;

    /*
        Keep dbo.Users for login.
        Reset identities so customer trial documents start from clean IDs.
    */
    IF OBJECT_ID(N'dbo.CustomerClaimDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CustomerClaimDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.CustomerClaimHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CustomerClaimHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.SerialClaimLogs', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SerialClaimLogs', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceiptHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceiptHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.PaymentAllocations', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PaymentAllocations', RESEED, 0);
    IF OBJECT_ID(N'dbo.PaymentHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PaymentHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.InvoiceSerials', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.InvoiceSerials', RESEED, 0);
    IF OBJECT_ID(N'dbo.InvoiceDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.InvoiceDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.InvoiceHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.InvoiceHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.QuotationDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.QuotationDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.QuotationHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.QuotationHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceivingSerials', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceivingSerials', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceivingDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceivingDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.ReceivingHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.ReceivingHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.PurchaseOrderDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PurchaseOrderDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.PurchaseOrderHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.PurchaseOrderHeaders', RESEED, 0);
    IF OBJECT_ID(N'dbo.SerialNumbers', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SerialNumbers', RESEED, 0);
    IF OBJECT_ID(N'dbo.Items', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Items', RESEED, 0);
    IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Customers', RESEED, 0);
    IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Suppliers', RESEED, 0);
    IF OBJECT_ID(N'dbo.Salespersons', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Salespersons', RESEED, 0);

    COMMIT TRANSACTION;

    PRINT N'Demo data cleanup completed. dbo.Users was kept.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
