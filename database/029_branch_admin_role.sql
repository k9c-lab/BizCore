-- BranchAdmin role guard.
-- No schema change: this only normalizes existing users if BranchAdmin was assigned manually.

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.Users
    SET CanAccessAllBranches = 0
    WHERE Role = N'BranchAdmin'
      AND CanAccessAllBranches = 1;
END;
GO
