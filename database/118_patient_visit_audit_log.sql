IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PatientVisitAuditLogs')
BEGIN
    CREATE TABLE PatientVisitAuditLogs (
        AuditLogId      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PatientVisitId  INT NOT NULL REFERENCES PatientVisits(PatientVisitId) ON DELETE CASCADE,
        UserId          INT NULL,
        UserName        NVARCHAR(100) NOT NULL DEFAULT '',
        OccurredAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Description     NVARCHAR(2000) NOT NULL
    );
    CREATE INDEX IX_PatientVisitAuditLogs_PatientVisitId ON PatientVisitAuditLogs(PatientVisitId);
END;
