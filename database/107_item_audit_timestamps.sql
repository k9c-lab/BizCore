-- Add audit timestamp columns to Items table
ALTER TABLE Items
    ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedDate DATETIME2 NULL;
