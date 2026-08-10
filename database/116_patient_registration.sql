-- Migration 116: Patient Registration module
-- Creates Patients, PatientVisits, PatientVisitItems tables
-- No impact on existing tables

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Patients')
BEGIN
    CREATE TABLE Patients (
        PatientId       INT IDENTITY(1,1) PRIMARY KEY,
        HN              NVARCHAR(30)   NOT NULL,
        NationalId      NVARCHAR(20)   NULL,
        FirstName       NVARCHAR(100)  NOT NULL,
        LastName        NVARCHAR(100)  NOT NULL,
        Gender          NVARCHAR(10)   NULL,
        DateOfBirth     DATE           NULL,
        Phone           NVARCHAR(20)   NULL,
        [Address]       NVARCHAR(500)  NULL,
        IsActive        BIT            NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX UX_Patients_HN ON Patients(HN);
END;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PatientVisits')
BEGIN
    CREATE TABLE PatientVisits (
        PatientVisitId      INT IDENTITY(1,1) PRIMARY KEY,
        VN                  NVARCHAR(30)   NOT NULL,
        VisitDate           DATE           NOT NULL,
        PatientId           INT            NOT NULL REFERENCES Patients(PatientId),
        CustomerId          INT            NULL     REFERENCES Customers(CustomerId),
        [Weight]            DECIMAL(6,2)   NULL,
        Height              DECIMAL(6,2)   NULL,
        Ward                NVARCHAR(100)  NULL,
        TreatmentRightId    INT            NULL     REFERENCES TreatmentRights(TreatmentRightId),
        ReferringDoctorId   INT            NULL     REFERENCES ReferringDoctors(ReferringDoctorId),
        ReferringHospital   NVARCHAR(200)  NULL,
        Remark              NVARCHAR(2000) NULL,
        [Status]            NVARCHAR(20)   NOT NULL DEFAULT 'Registered',
        InvoiceId           INT            NULL     REFERENCES InvoiceHeaders(InvoiceId),
        BranchId            INT            NULL     REFERENCES Branches(BranchId),
        CreatedByUserId     INT            NULL     REFERENCES Users(UserId),
        CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX UX_PatientVisits_VN ON PatientVisits(VN);
END;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PatientVisitItems')
BEGIN
    CREATE TABLE PatientVisitItems (
        PatientVisitItemId  INT IDENTITY(1,1) PRIMARY KEY,
        PatientVisitId      INT             NOT NULL REFERENCES PatientVisits(PatientVisitId) ON DELETE CASCADE,
        ItemId              INT             NOT NULL REFERENCES Items(ItemId),
        Quantity            DECIMAL(18,4)   NOT NULL DEFAULT 1
    );
END;
