-- Migration 120: Add NamePrefix column to Patients table

IF COL_LENGTH(N'dbo.Patients', N'NamePrefix') IS NULL
BEGIN
    ALTER TABLE dbo.Patients ADD NamePrefix NVARCHAR(20) NULL;
END;
GO
