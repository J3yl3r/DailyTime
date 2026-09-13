-- Script de migración: horario global de captura automática.
-- Idempotente: se puede ejecutar varias veces.

-- Check "Automática" por portal (antes vivía en localStorage del navegador)
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[JobPortal]')
      AND name = 'AutoScrapeEnabled'
)
BEGIN
    ALTER TABLE [dbo].[JobPortal]
    ADD [AutoScrapeEnabled] BIT NOT NULL CONSTRAINT [DF_JobPortal_AutoScrapeEnabled] DEFAULT (0);
END
GO

-- Horario global (una sola fila): mismas horas para todos los portales marcados
IF OBJECT_ID(N'[dbo].[ScrapeSchedule]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ScrapeSchedule] (
        [Id] INT IDENTITY(1, 1) NOT NULL CONSTRAINT [PK_ScrapeSchedule] PRIMARY KEY,
        [Enabled] BIT NOT NULL CONSTRAINT [DF_ScrapeSchedule_Enabled] DEFAULT (0),
        [Times] NVARCHAR(200) NOT NULL CONSTRAINT [DF_ScrapeSchedule_Times] DEFAULT (N''),
        [Days] NVARCHAR(20) NULL,
        [TimeZoneId] NVARCHAR(64) NOT NULL CONSTRAINT [DF_ScrapeSchedule_TimeZoneId] DEFAULT (N'America/Bogota'),
        [ConfigUpdatedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ScrapeSchedule_ConfigUpdatedAt] DEFAULT (SYSUTCDATETIME()),
        [LastSlotAt] DATETIME2(3) NULL,
        [LastSlotStatus] NVARCHAR(20) NULL,
        [LastSlotMessage] NVARCHAR(1000) NULL,
        [LastSlotFinishedAt] DATETIME2(3) NULL,
        [UpdatedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_ScrapeSchedule_UpdatedAt] DEFAULT (SYSUTCDATETIME())
    );
END
GO
