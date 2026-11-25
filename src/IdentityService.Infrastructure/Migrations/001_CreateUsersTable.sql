CREATE TABLE IF NOT EXISTS users (
    aggregateid VARCHAR(128) NOT NULL,
    createdtime TIMESTAMPTZ NOT NULL,
    updatedtime TIMESTAMPTZ NOT NULL,
    createdby VARCHAR(128) NOT NULL DEFAULT 'system',
    updatedby VARCHAR(128) NOT NULL DEFAULT 'system',
    lastaggregatesequencenumber INT NOT NULL DEFAULT 1,
    username VARCHAR(128) NOT NULL,
    name VARCHAR(256) NOT NULL,
    email VARCHAR(256) NOT NULL,
    phone VARCHAR(128) NOT NULL,
    lastpasswordreset VARCHAR(128) NOT NULL DEFAULT '',
    twofactorenabled BOOLEAN NOT NULL DEFAULT FALSE,
    lastlogin TIMESTAMPTZ NULL,
    lastloginip VARCHAR(128) NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Active',
    CONSTRAINT pk_users PRIMARY KEY (aggregateid)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_users_username ON users (username);
