-- init.sql: Runs on first container boot

CREATE TABLE IF NOT EXISTS staff (
    aggregateid VARCHAR(128) NOT NULL,
    createtime TIMESTAMPTZ NOT NULL,
    updatedtime TIMESTAMPTZ NOT NULL,
    lastaggregatesequencenumber INT NOT NULL DEFAULT 1,
    employeeid VARCHAR(128) NOT NULL,
    username VARCHAR(128) NOT NULL,
    name VARCHAR(256) NOT NULL,
    email VARCHAR(256) NOT NULL,
    role VARCHAR(256) NOT NULL,
    phone VARCHAR(128) NOT NULL,
    dept VARCHAR(256) NOT NULL,
    lastpasswordreset VARCHAR(128) NOT NULL DEFAULT '',
    twofactorenabled BOOLEAN NOT NULL DEFAULT FALSE,
    lastlogin TIMESTAMPTZ NULL,
    lastloginip VARCHAR(128) NULL,
    emailalerts BOOLEAN NOT NULL DEFAULT TRUE,
    systemmessages BOOLEAN NOT NULL DEFAULT TRUE,
    weeklysummary BOOLEAN NOT NULL DEFAULT TRUE,
    professionalbio TEXT NULL,
    photourl TEXT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Active',
    onboardedat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    mustresetpassword BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_staff PRIMARY KEY (aggregateid)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_staff_username ON staff (username);
CREATE UNIQUE INDEX IF NOT EXISTS ix_staff_employeeid ON staff (employeeid);

-- Seed default user
INSERT INTO staff (
    aggregateid, createtime, updatedtime, lastaggregatesequencenumber,
    employeeid, username, name, email, role, phone, dept,
    lastpasswordreset, twofactorenabled, emailalerts, systemmessages,
    weeklysummary, status, onboardedat, mustresetpassword
) VALUES (
    'staff-default-001', NOW(), NOW(), 1,
    'EMP-2024-001', 'test@rest.com', 'System Admin', 'test@rest.com',
    'Admin', '+1 (555) 000-0000', 'IT',
    'Jun 15, 2026', false, true, true,
    true, 'Active', NOW(), false
) ON CONFLICT (aggregateid) DO NOTHING;
