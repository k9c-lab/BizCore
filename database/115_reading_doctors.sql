-- Migration 115: Insert reading doctors RDR-0038 to RDR-0056
-- Run on production after verifying latest code is RDR-0037

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0038')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0038', N'นพ.ชนัตถ์ เต็งสิริอรกุล', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0039')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0039', N'พญ.แพรวา สนจีน', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0040')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0040', N'พญ.ณภัทร บูรพนาวิบูลย์', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0041')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0041', N'พญ.ปณิธิ เพิ่มศิริวาณิชย์', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0042')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0042', N'พญ.วรุณยุภา อู่ขลิบ', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0043')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0043', N'นพ.อาจิณ มรีกาญจน์', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0044')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0044', N'พญ.ชยุดา ชินพรเจริญพงศ์', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0045')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0045', N'นพ.ประพัฒน์ เรืองฤทธิ์กุล', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0046')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0046', N'พญ.ชุติมา ตั้งนิธิบุญ', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0047')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0047', N'พญ.ภารณี ศรีสุภะ', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0048')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0048', N'พญ.ปวีณกร คะรัมย์', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0049')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0049', N'พญ.เรวดี วงศ์อามาตย์', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0050')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0050', N'นพ.อภิฉัตร มาศเมธาทิพย์', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0051')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0051', N'พญ.สุดาพิม ปรางค์เจริญ', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0052')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0052', N'นพ.อุกฤษฎ์ ศรีบรรเทา', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0053')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0053', N'พญ.วัชชิรา ดวงแก้ว', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0054')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0054', N'พญ.วริษฐา เจริญเนตร', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0055')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0055', N'นพ.อัครชัย ลาภธนัญชัยวงค์', 1);

IF NOT EXISTS (SELECT 1 FROM ReadingDoctors WHERE DoctorCode = 'RDR-0056')
    INSERT INTO ReadingDoctors (DoctorCode, DoctorName, IsActive) VALUES ('RDR-0056', N'นพ.อภิรัฐ ยอดแก้ว', 1);
