/*
    BizCore business data cleanup script.

    PURPOSE
    - Clear transactional/demo business data before customer handover.
    - Clear business master data that should be re-created per customer:
      Customers, Suppliers, Salespersons, Items, ItemPrices, SerialNumbers.
    - Keep important system master/setup data so the app can still be used:
      Users, Permissions, RolePermissions, Branches, SystemSettings,
      AppliedScripts, PriceLevels, TreatmentRights, ReferringDoctors.

    THIS SCRIPT KEEPS
    - dbo.Users
    - dbo.Permissions
    - dbo.RolePermissions
    - dbo.Branches
    - dbo.SystemSettings
    - dbo.AppliedScripts
    - dbo.PriceLevels
    - dbo.TreatmentRights
    - dbo.ReferringDoctors

    THIS SCRIPT CLEARS
    - Announcements
    - All sales / purchasing / finance / inventory / claim documents
    - Customers, Suppliers, Salespersons
    - Items, ItemPrices, SerialNumbers, StockBalances, StockMovements

    IMPORTANT
    1. Back up the database first.
    2. Run only on the handover/demo database.
    3. After running, re-create customer-specific masters as needed.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
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

    /*
        Inventory state can reference sales/purchasing documents through serials.
        Clear dependent stock/serial tables before removing headers and masters.
    */
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

    /* Non-essential setup/demo content */
    IF OBJECT_ID(N'dbo.Announcements', N'U') IS NOT NULL
        DELETE FROM dbo.Announcements;

    /* Customer-specific master data to be removed */
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
    IF OBJECT_ID(N'dbo.Announcements', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.Announcements', RESEED, 0);

    COMMIT TRANSACTION;

    PRINT N'Business data cleanup completed. System masters were kept.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
