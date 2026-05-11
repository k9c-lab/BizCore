DECLARE @RolePermissionSeed TABLE
(
    RoleName NVARCHAR(30) NOT NULL,
    PermissionCode NVARCHAR(80) NOT NULL
);

INSERT INTO @RolePermissionSeed (RoleName, PermissionCode)
VALUES
    (N'Accounting', N'Sales.Invoices.Menu'),
    (N'Accounting', N'Sales.BillingNotes.Menu'),
    (N'Accounting', N'Sales.Payments.Menu'),
    (N'Accounting', N'Sales.Receipts.Menu'),
    (N'Accounting', N'Purchasing.PR.Menu'),
    (N'Accounting', N'Purchasing.PO.Menu'),
    (N'Accounting', N'PR.View'),
    (N'Accounting', N'PO.View'),
    (N'Accounting', N'PO.Create'),
    (N'Accounting', N'PO.Edit'),
    (N'Accounting', N'PO.Submit'),
    (N'Accounting', N'PO.Cancel');

INSERT INTO dbo.RolePermissions (RoleName, PermissionId)
SELECT seed.RoleName, permissions.PermissionId
FROM @RolePermissionSeed seed
INNER JOIN dbo.Permissions permissions ON permissions.Code = seed.PermissionCode
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.RolePermissions existing
    WHERE existing.RoleName = seed.RoleName
      AND existing.PermissionId = permissions.PermissionId
);
GO
