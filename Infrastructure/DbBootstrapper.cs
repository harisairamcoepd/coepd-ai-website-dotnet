using Coepd.Web.Models;
using System;
using System.Data.Entity;

namespace Coepd.Web.Infrastructure
{
    public static class DbBootstrapper
    {
        public static void EnsureInitialized()
        {
            if (StorageMode.UseRuntimeStore())
            {
                DiagnosticLogger.Info("DbBootstrapper", "Runtime store mode enabled. SQL bootstrap skipped.");
                return;
            }

            try
            {
                using (var db = new CoepdDbContext())
                {
                    db.Database.Connection.Open();
                    DiagnosticLogger.Info("DbBootstrapper", "SQL connection opened successfully.");
                    db.Database.ExecuteSqlCommand(@"
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

IF COL_LENGTH('dbo.leads', 'name') IS NULL ALTER TABLE dbo.leads ADD name NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.leads', 'phone') IS NULL ALTER TABLE dbo.leads ADD phone NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.leads', 'email') IS NULL ALTER TABLE dbo.leads ADD email NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.leads', 'location') IS NULL ALTER TABLE dbo.leads ADD location NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.leads', 'interested_domain') IS NULL ALTER TABLE dbo.leads ADD interested_domain NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.leads', 'whatsapp') IS NULL ALTER TABLE dbo.leads ADD whatsapp NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.leads', 'source') IS NULL ALTER TABLE dbo.leads ADD source NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.leads', 'created_at') IS NULL ALTER TABLE dbo.leads ADD created_at DATETIME2 NULL;

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

IF COL_LENGTH('dbo.staff', 'name') IS NULL ALTER TABLE dbo.staff ADD name NVARCHAR(120) NULL;
IF COL_LENGTH('dbo.staff', 'email') IS NULL ALTER TABLE dbo.staff ADD email NVARCHAR(120) NULL;
IF COL_LENGTH('dbo.staff', 'password_hash') IS NULL ALTER TABLE dbo.staff ADD password_hash NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.staff', 'role') IS NULL ALTER TABLE dbo.staff ADD role NVARCHAR(20) NOT NULL CONSTRAINT DF_staff_role_auto DEFAULT 'staff';
IF COL_LENGTH('dbo.staff', 'status') IS NULL ALTER TABLE dbo.staff ADD status NVARCHAR(20) NOT NULL CONSTRAINT DF_staff_status_auto DEFAULT 'active';
IF COL_LENGTH('dbo.staff', 'created_at') IS NULL ALTER TABLE dbo.staff ADD created_at DATETIME2 NOT NULL CONSTRAINT DF_staff_created_at_auto DEFAULT SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_staff_email' AND object_id = OBJECT_ID('dbo.staff'))
    CREATE UNIQUE INDEX IX_staff_email ON dbo.staff(email);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_leads_created_at' AND object_id = OBJECT_ID('dbo.leads'))
    CREATE INDEX IX_leads_created_at ON dbo.leads(created_at);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_leads_email' AND object_id = OBJECT_ID('dbo.leads'))
    CREATE INDEX IX_leads_email ON dbo.leads(email);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_leads_phone' AND object_id = OBJECT_ID('dbo.leads'))
    CREATE INDEX IX_leads_phone ON dbo.leads(phone);");
                    DiagnosticLogger.Info("DbBootstrapper", "Database bootstrap completed successfully.");
                }
            }
            catch (Exception ex)
            {
                DiagnosticLogger.Error("DbBootstrapper", "Database bootstrap failed.", ex);
            }
        }
    }
}
