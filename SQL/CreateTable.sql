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
    2,
    'CAR000001',
    'EQP01',
    '10101',
    GETDATE(),
    0
);

INSERT INTO shelves
VALUES('10101', 1, 0, 0);

SELECT * FROM commands;
SELECT * FROM shelves;

DROP TABLE commands;
DROP TABLE shelves;
DROP SEQUENCE transportseq;