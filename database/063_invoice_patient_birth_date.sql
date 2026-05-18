IF COL_LENGTH(N'dbo.InvoiceHeaders', N'PatientBirthDate') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ADD PatientBirthDate DATE NULL;
END
