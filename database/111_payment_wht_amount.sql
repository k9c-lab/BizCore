-- Add withholding tax amount field to PaymentHeaders
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'PaymentHeaders') AND name = N'WhtAmount')
    ALTER TABLE PaymentHeaders ADD WhtAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
