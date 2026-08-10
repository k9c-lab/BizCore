-- Migration 114: Insert referring doctors DOC-0217 to DOC-0239
-- Run on production after verifying latest code is DOC-0216

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0217')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0217', N'นพ.ธีรเดช สวธานันท์', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0218')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0218', N'พญ.พีรดา สิยาโน', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0219')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0219', N'นพ.ธนกฤษณ์ คงขำ', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0220')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0220', N'นพ.ศุภวิช จินดาเวช', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0221')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0221', N'พญ.อรอุรา เห็มทอง', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0222')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0222', N'พญ.ณัฐชนน สนหลักสี่', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0223')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0223', N'นพ.ศุภชัย สงสัย', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0224')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0224', N'นพ.ศิรธันย์ อิทธิภูริพัฒน์', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0225')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0225', N'พญ.จริยา จอมพล', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0226')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0226', N'นพ.สุรภัทร คงสวัสดิ์', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0227')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0227', N'พญ.ฐิติมา หิรัญญนิธิวัฒนา', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0228')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0228', N'นพ.สาธิต ตั้งประเสร็จ', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0229')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0229', N'นพ.สุพจน์ เดชอาคม', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0230')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0230', N'นพ.กฤษณพล พิณสุวรรณ์', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0231')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0231', N'นพ.กฤษศักดา วรรณพรม', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0232')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0232', N'นพ.วรท สัตยาวุฒิพงศ์', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0233')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0233', N'นพ.สักกพันธ์ เลาหสุรโยธิน', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0234')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0234', N'พญ.พิมสิริ พึ่งสุข', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0235')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0235', N'นพ.กิตติโชติ พิพัฒ์ดำเกิง', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0236')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0236', N'นพ.ธนบัตร เชิญรุ่งโรจน์', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0237')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0237', N'นพ.ธัชณรงค์ ธัญญศรี', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0238')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0238', N'พญ.ธันย์ชนก เนาวะวาทอง', 1);

IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE DoctorCode = 'DOC-0239')
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive) VALUES ('DOC-0239', N'นพ.ณัฐกุล เกตุพิชัย', 1);
