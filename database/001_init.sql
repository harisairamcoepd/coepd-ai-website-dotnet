-- SQL Server schema bootstrap for COEPD MVC4 migration
IF OBJECT_ID('dbo.leads', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.leads (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(255) NULL,
        phone NVARCHAR(50) NULL,
        email NVARCHAR(255) NULL,
        location NVARCHAR(255) NULL,
        interested_domain NVARCHAR(255) NULL,
        whatsapp NVARCHAR(50) NULL,
        source NVARCHAR(50) NULL,
        created_at DATETIME2 NULL CONSTRAINT DF_leads_created_at DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF OBJECT_ID('dbo.staff', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.staff (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(120) NOT NULL,
        email NVARCHAR(120) NOT NULL,
        password_hash NVARCHAR(255) NOT NULL,
        role NVARCHAR(20) NOT NULL CONSTRAINT DF_staff_role DEFAULT 'staff',
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_staff_status DEFAULT 'active',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_staff_created_at DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_staff_email' AND object_id = OBJECT_ID('dbo.staff'))
    CREATE UNIQUE INDEX IX_staff_email ON dbo.staff(email);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_leads_created_at' AND object_id = OBJECT_ID('dbo.leads'))
    CREATE INDEX IX_leads_created_at ON dbo.leads(created_at);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_leads_email' AND object_id = OBJECT_ID('dbo.leads'))
    CREATE INDEX IX_leads_email ON dbo.leads(email);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_leads_phone' AND object_id = OBJECT_ID('dbo.leads'))
    CREATE INDEX IX_leads_phone ON dbo.leads(phone);
GO
