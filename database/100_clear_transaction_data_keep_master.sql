/*
    BizCore transaction/test data cleanup script.

    WARNING:
    - This script deletes business documents, stock, serial, and claim history.
    - It keeps master/setup data:
      Users, Branches, Customers, Suppliers, Salespersons, Items.
    - Run only on a demo/test database, not production.

    Recommended use:
    1. Back up the database first.
    2. Run this script.
    3. Login with existing users and start testing PR -> PO allocation -> Receiving again.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    /* Warranty / claim documents */
    /*
        Legacy supplier claim tables may still exist from early schema versions.
        The current app uses SerialClaimLogs, but these legacy tables can still
        reference SerialNumbers and must be cleared before SerialNumbers.
    */
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

    /* Sales documents */
    IF OBJECT_ID(N'dbo.InvoiceSerials', N'U') IS NOT NULL
        DELETE FROM dbo.InvoiceSerials;

    /*
        Inventory transaction state.
        SerialNumbers can reference InvoiceHeaders, and InvoiceSerials can reference SerialNumbers.
        So the order is InvoiceSerials -> SerialNumbers -> InvoiceHeaders.
        Claim documents were already removed above.
    */
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

    /*
        Keep master data:
        dbo.Users, dbo.Branches, dbo.Customers, dbo.Suppliers, dbo.Salespersons, dbo.Items.
        Reset item current stock because all stock/serial transactions were cleared.
    */
    IF COL_LENGTH('dbo.Items', 'CurrentStock') IS NOT NULL
        UPDATE dbo.Items SET CurrentStock = 0;

    /* Reset identities for document/transaction tables */
    IF OBJECT_ID(N'dbo.CustomerClaimDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.CustomerClaimDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.SupplierClaimDetails', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SupplierClaimDetails', RESEED, 0);
    IF OBJECT_ID(N'dbo.SupplierClaimHeaders', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.SupplierClaimHeaders', RESEED, 0);
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

    COMMIT TRANSACTION;

    PRINT N'Transaction cleanup completed. Users, branches, customers, suppliers, salespersons, and items were kept.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
