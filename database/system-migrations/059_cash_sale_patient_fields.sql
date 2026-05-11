IF COL_LENGTH('dbo.CashSaleHeaders', 'PatientFullName') IS NULL
BEGIN
    ALTER TABLE dbo.CashSaleHeaders ADD PatientFullName NVARCHAR(200) NULL;
END;

IF COL_LENGTH('dbo.CashSaleHeaders', 'PatientAge') IS NULL
BEGIN
    ALTER TABLE dbo.CashSaleHeaders ADD PatientAge INT NULL;
END;

IF COL_LENGTH('dbo.CashSaleHeaders', 'PatientGender') IS NULL
BEGIN
    ALTER TABLE dbo.CashSaleHeaders ADD PatientGender NVARCHAR(20) NULL;
END;

IF COL_LENGTH('dbo.CashSaleHeaders', 'PatientHn') IS NULL
BEGIN
    ALTER TABLE dbo.CashSaleHeaders ADD PatientHn NVARCHAR(50) NULL;
END;

IF COL_LENGTH('dbo.CashSaleHeaders', 'TreatmentRightId') IS NULL
BEGIN
    ALTER TABLE dbo.CashSaleHeaders ADD TreatmentRightId INT NULL;
END;

IF COL_LENGTH('dbo.CashSaleHeaders', 'PatientWard') IS NULL
BEGIN
    ALTER TABLE dbo.CashSaleHeaders ADD PatientWard NVARCHAR(100) NULL;
END;

IF COL_LENGTH('dbo.CashSaleHeaders', 'ReferringDoctorId') IS NULL
BEGIN
    ALTER TABLE dbo.CashSaleHeaders ADD ReferringDoctorId INT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CashSaleHeaders_TreatmentRights')
BEGIN
    ALTER TABLE dbo.CashSaleHeaders WITH CHECK
    ADD CONSTRAINT FK_CashSaleHeaders_TreatmentRights
    FOREIGN KEY (TreatmentRightId) REFERENCES dbo.TreatmentRights(TreatmentRightId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CashSaleHeaders_ReferringDoctors')
BEGIN
    ALTER TABLE dbo.CashSaleHeaders WITH CHECK
    ADD CONSTRAINT FK_CashSaleHeaders_ReferringDoctors
    FOREIGN KEY (ReferringDoctorId) REFERENCES dbo.ReferringDoctors(ReferringDoctorId);
END;
