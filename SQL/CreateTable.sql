CREATE SEQUENCE transportseq
	START WITH 1
	INCREMENT BY 1;
	
CREATE TABLE commands (
	command_id VARCHAR(10) PRIMARY KEY DEFAULT ('TC' + RIGHT('00000' + CAST(NEXT VALUE FOR transportseq AS VARCHAR(6)), 6)),
	command_type INT NOT NULL,
	carrier_id VARCHAR(10) NOT NULL,
	eqp_name VARCHAR(10) NOT NULL,
	location VARCHAR(10) NOT NULL,
	reception_at DATETIME NOT NULL,
	send_at DATETIME NULL,
	completion_at DATETIME NULL,
	command_status INT NOT NULL
);

CREATE TABLE shelves (
	shelf_location VARCHAR(10) PRIMARY KEY,
	stored_carrier_id VARCHAR(10) NULL,
	reservation VARCHAR(10) NULL,
	storage_at DATETIME NULL
);

INSERT INTO commands(
    command_type,
    carrier_id,
    eqp_name,
    location,
    reception_at,
    command_status
)
VALUES(
    1,
    'CAR000001',
    'EQP01',
    '10101',
    GETDATE(),
    0
);

INSERT INTO shelves VALUES ('10101', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('10102', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('10103', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('10201', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('10202', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('10203', NULL, NULL, NULL);

INSERT INTO shelves VALUES ('20101', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('20102', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('20103', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('20201', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('20202', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('20203', NULL, NULL, NULL);

INSERT INTO shelves VALUES ('30101', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('30102', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('30103', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('30201', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('30202', NULL, NULL, NULL);
INSERT INTO shelves VALUES ('30203', NULL, NULL, NULL);

UPDATE shelves
SET
    stored_carrier_id = 'CAR000001',
    storage_at = '2026-06-24 09:15:00'
WHERE shelf_location = '10101';

UPDATE shelves
SET
    stored_carrier_id = NULL,
    storage_at = NULL
WHERE shelf_location = '10102';

UPDATE shelves
SET
    stored_carrier_id = 'CAR000003',
    storage_at = '2026-06-24 10:00:00'
WHERE shelf_location = '10103';

UPDATE shelves
SET
    stored_carrier_id = NULL,
    storage_at = NULL
WHERE shelf_location = '10201';

UPDATE shelves
SET
    stored_carrier_id = 'CAR000005',
    storage_at = '2026-06-24 11:00:00'
WHERE shelf_location = '10202';

UPDATE shelves
SET
    stored_carrier_id = 'CAR000006',
    storage_at = '2026-06-24 13:20:00'
WHERE shelf_location = '10203';

SELECT * FROM commands;
SELECT * FROM shelves;

DROP TABLE commands;
DROP TABLE shelves;
DROP SEQUENCE transportseq;