-- Seed 50 new referring doctors (DOC-0145 to DOC-0194)
-- Duplicates already in DB were excluded. Safe to re-run (IF NOT EXISTS guard).

-- 1. นพ.พันยุติ วัชรากร
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.พันยุติ วัชรากร', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0145', N'นพ.พันยุติ วัชรากร', 1);

-- 2. นพ.ปฏิภาณ วงค์คำจันทร์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ปฏิภาณ วงค์คำจันทร์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0146', N'นพ.ปฏิภาณ วงค์คำจันทร์', 1);

-- 3. นพ.ทิวัตถ์ อิทธสมบัติ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ทิวัตถ์ อิทธสมบัติ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0147', N'นพ.ทิวัตถ์ อิทธสมบัติ', 1);

-- 4. พญ.สุทธิดา ศิริพันธ์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.สุทธิดา ศิริพันธ์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0148', N'พญ.สุทธิดา ศิริพันธ์', 1);

-- 5. พญ.โชติกาญจน์ ทองยัง
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.โชติกาญจน์ ทองยัง', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0149', N'พญ.โชติกาญจน์ ทองยัง', 1);

-- 6. นพ.ณภัทร หัสมินทร์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ณภัทร หัสมินทร์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0150', N'นพ.ณภัทร หัสมินทร์', 1);

-- 7. พญ.สุพิชชา ฤทธิไกรรณการ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.สุพิชชา ฤทธิไกรรณการ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0151', N'พญ.สุพิชชา ฤทธิไกรรณการ', 1);

-- 8. พญ.วรณัน บุญสมสุข
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.วรณัน บุญสมสุข', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0152', N'พญ.วรณัน บุญสมสุข', 1);

-- 9. พญ.สิริธนันท์ เจริญวิกกัย
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.สิริธนันท์ เจริญวิกกัย', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0153', N'พญ.สิริธนันท์ เจริญวิกกัย', 1);

-- 10. พญ.อารยา เจษฎ์ปิยะวงศ์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.อารยา เจษฎ์ปิยะวงศ์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0154', N'พญ.อารยา เจษฎ์ปิยะวงศ์', 1);

-- 11. พญ.ณิชา พิริยะพันธ์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ณิชา พิริยะพันธ์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0155', N'พญ.ณิชา พิริยะพันธ์', 1);

-- 12. นพ.สรวิศ เกรียงตันวงศ์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.สรวิศ เกรียงตันวงศ์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0156', N'นพ.สรวิศ เกรียงตันวงศ์', 1);

-- 13. นพ.ธีรพงศ์ ตัญเจริญสุขจิต
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ธีรพงศ์ ตัญเจริญสุขจิต', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0157', N'นพ.ธีรพงศ์ ตัญเจริญสุขจิต', 1);

-- 14. พญ.จรรยาภรณ์ ทองทิน
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.จรรยาภรณ์ ทองทิน', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0158', N'พญ.จรรยาภรณ์ ทองทิน', 1);

-- 15. พญ.อัญชลี ภู่สงค์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.อัญชลี ภู่สงค์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0159', N'พญ.อัญชลี ภู่สงค์', 1);

-- 16. นพ.ประมณฑ์ โอประเสริฐสวัสดิ์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ประมณฑ์ โอประเสริฐสวัสดิ์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0160', N'นพ.ประมณฑ์ โอประเสริฐสวัสดิ์', 1);

-- 17. พญ.พัชริดา มหัสฉริยพงษ์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.พัชริดา มหัสฉริยพงษ์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0161', N'พญ.พัชริดา มหัสฉริยพงษ์', 1);

-- 18. พญ.พิชาดา กิตติวงศธร
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.พิชาดา กิตติวงศธร', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0162', N'พญ.พิชาดา กิตติวงศธร', 1);

-- 19. นพ.วิศรุต ถนอมพฤฒิกุล
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.วิศรุต ถนอมพฤฒิกุล', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0163', N'นพ.วิศรุต ถนอมพฤฒิกุล', 1);

-- 20. นพ.วิชยุตม์ รัชตะ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.วิชยุตม์ รัชตะ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0164', N'นพ.วิชยุตม์ รัชตะ', 1);

-- 21. นพ.พัสกร ชื่นอยู่
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.พัสกร ชื่นอยู่', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0165', N'นพ.พัสกร ชื่นอยู่', 1);

-- 22. นพ.นิติภูมิ นันทสุวรรณ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.นิติภูมิ นันทสุวรรณ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0166', N'นพ.นิติภูมิ นันทสุวรรณ', 1);

-- 23. พญ.ลักษิกา สุขสวัสดิ์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ลักษิกา สุขสวัสดิ์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0167', N'พญ.ลักษิกา สุขสวัสดิ์', 1);

-- 24. พญ.ศรัญนา ประภาสัย
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ศรัญนา ประภาสัย', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0168', N'พญ.ศรัญนา ประภาสัย', 1);

-- 25. ทพญ.ภรณิดา ภู่ประเสริฐ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'ทพญ.ภรณิดา ภู่ประเสริฐ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0169', N'ทพญ.ภรณิดา ภู่ประเสริฐ', 1);

-- 26. นพ.ภูริณัฐ โตวาร์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ภูริณัฐ โตวาร์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0170', N'นพ.ภูริณัฐ โตวาร์', 1);

-- 27. นพ.สมัฒย์ ภาวะโสภณ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.สมัฒย์ ภาวะโสภณ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0171', N'นพ.สมัฒย์ ภาวะโสภณ', 1);

-- 28. พญ.ธันยธร กังพิศดาร
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ธันยธร กังพิศดาร', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0172', N'พญ.ธันยธร กังพิศดาร', 1);

-- 29. นพ.ภูวดล คำนึงธรรม
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ภูวดล คำนึงธรรม', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0173', N'นพ.ภูวดล คำนึงธรรม', 1);

-- 30. นพ.พีรพล เกตุกิจ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.พีรพล เกตุกิจ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0174', N'นพ.พีรพล เกตุกิจ', 1);

-- 31. นพ.คณิน รุ่งคณาวุฒิ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.คณิน รุ่งคณาวุฒิ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0175', N'นพ.คณิน รุ่งคณาวุฒิ', 1);

-- 32. พญ.วิวรรณ พงศ์พัฒนานนท์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.วิวรรณ พงศ์พัฒนานนท์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0176', N'พญ.วิวรรณ พงศ์พัฒนานนท์', 1);

-- 33. พญ.ดลรัสม์ เหลืองแสงชัย
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ดลรัสม์ เหลืองแสงชัย', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0177', N'พญ.ดลรัสม์ เหลืองแสงชัย', 1);

-- 34. พญ.นภฝัน มณีโชติ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.นภฝัน มณีโชติ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0178', N'พญ.นภฝัน มณีโชติ', 1);

-- 35. พญ.ณัฐวดี อินทแสน
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ณัฐวดี อินทแสน', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0179', N'พญ.ณัฐวดี อินทแสน', 1);

-- 36. นพ.ธนกฤต กนกวัฒนกุล
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ธนกฤต กนกวัฒนกุล', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0180', N'นพ.ธนกฤต กนกวัฒนกุล', 1);

-- 37. พญ.ณัฐวรา เจนวิชชุ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ณัฐวรา เจนวิชชุ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0181', N'พญ.ณัฐวรา เจนวิชชุ', 1);

-- 38. พญ.วัลลภา ตั้งทรัพย์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.วัลลภา ตั้งทรัพย์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0182', N'พญ.วัลลภา ตั้งทรัพย์', 1);

-- 39. นพ.ตฤณปรัชญา โอริส
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ตฤณปรัชญา โอริส', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0183', N'นพ.ตฤณปรัชญา โอริส', 1);

-- 40. พญ.รชยา โพธิอาศน์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.รชยา โพธิอาศน์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0184', N'พญ.รชยา โพธิอาศน์', 1);

-- 41. นพ.ปฐวี ศรีนวกุล
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.ปฐวี ศรีนวกุล', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0185', N'นพ.ปฐวี ศรีนวกุล', 1);

-- 42. พญ.จารวี เกียรติชูพิพัฒน์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.จารวี เกียรติชูพิพัฒน์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0186', N'พญ.จารวี เกียรติชูพิพัฒน์', 1);

-- 43. พญ.อัซฮานี รักชุมคง
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.อัซฮานี รักชุมคง', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0187', N'พญ.อัซฮานี รักชุมคง', 1);

-- 44. พญ.ปานเนตร อินทรเรือง
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ปานเนตร อินทรเรือง', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0188', N'พญ.ปานเนตร อินทรเรือง', 1);

-- 45. พญ.อรวรรณ ตะเวทิพงศ์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.อรวรรณ ตะเวทิพงศ์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0189', N'พญ.อรวรรณ ตะเวทิพงศ์', 1);

-- 46. นพ.รชต ระหว่างบ้าน
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'นพ.รชต ระหว่างบ้าน', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0190', N'นพ.รชต ระหว่างบ้าน', 1);

-- 47. พญ.ธัญชนก ปลื้มสุทธิ์
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ธัญชนก ปลื้มสุทธิ์', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0191', N'พญ.ธัญชนก ปลื้มสุทธิ์', 1);

-- 48. พญ.สุประวีณ์ คชเกตุ
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.สุประวีณ์ คชเกตุ', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0192', N'พญ.สุประวีณ์ คชเกตุ', 1);

-- 49. พญ.ปองขวัญ ม่วงเสม
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.ปองขวัญ ม่วงเสม', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0193', N'พญ.ปองขวัญ ม่วงเสม', 1);

-- 50. พญ.มนรดา จงจิตรไพศาล
IF NOT EXISTS (SELECT 1 FROM ReferringDoctors WHERE REPLACE(REPLACE(REPLACE(DoctorName, N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N'') = REPLACE(REPLACE(REPLACE(N'พญ.มนรดา จงจิตรไพศาล', N'นพ.', N''), N'พญ.', N''), N'พ.ญ.', N''))
    INSERT INTO ReferringDoctors (DoctorCode, DoctorName, IsActive)
    VALUES (N'DOC-0194', N'พญ.มนรดา จงจิตรไพศาล', 1);

