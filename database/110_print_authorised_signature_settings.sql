-- Add authorised signature settings for formal invoice print (สำหรับส่วนกลาง)
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = N'Print.AuthorisedName')
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description, UpdatedAtUtc)
    VALUES (N'Print.AuthorisedName', N'', N'ชื่อผู้มีอำนาจลงนาม สำหรับพิมพ์ใบแจ้งหนี้แบบส่วนกลาง', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = N'Print.AuthorisedTitle')
    INSERT INTO SystemSettings (SettingKey, SettingValue, Description, UpdatedAtUtc)
    VALUES (N'Print.AuthorisedTitle', N'', N'ตำแหน่งผู้มีอำนาจลงนาม สำหรับพิมพ์ใบแจ้งหนี้แบบส่วนกลาง', SYSUTCDATETIME());
