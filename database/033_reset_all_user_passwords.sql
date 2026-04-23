-- Reset all user passwords to: 12345678
-- This keeps existing users, roles, branches, active status, and permissions unchanged.

UPDATE dbo.Users
SET PasswordHash = N'PBKDF2-SHA256$100000$SafDBCAiMLag9hhOytdK3Q==$+nNFa1Yvc3gyb3bbIq2bscIIYGI1uDPNvA9Q1xKtSio=';

SELECT
    Username,
    DisplayName,
    Role,
    IsActive
FROM dbo.Users
ORDER BY Username;
