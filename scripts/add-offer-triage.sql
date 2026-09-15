-- Script de migración: priorización y descarte automático de ofertas.
-- Idempotente. Las sentencias que usan columnas recién creadas van en EXEC para que también
-- funcione en un solo lote (sin GO).

-- Quién fijó el estado. Lo que no está en 'new' ya lo decidió el usuario: las reglas no lo tocan.
IF COL_LENGTH(N'dbo.JobOffer', N'StatusSource') IS NULL
BEGIN
    ALTER TABLE [dbo].[JobOffer]
    ADD [StatusSource] NVARCHAR(10) NOT NULL CONSTRAINT [DF_JobOffer_StatusSource] DEFAULT (N'system');

    EXEC(N'UPDATE [dbo].[JobOffer] SET [StatusSource] = N''user'' WHERE [Status] <> N''new'';');
END
GO

-- Fijar al arrastrar. El orden manual anterior abarcaba toda la lista: se reinicia.
IF COL_LENGTH(N'dbo.JobOffer', N'IsPinned') IS NULL
BEGIN
    ALTER TABLE [dbo].[JobOffer]
    ADD [IsPinned] BIT NOT NULL CONSTRAINT [DF_JobOffer_IsPinned] DEFAULT (0);

    EXEC(N'UPDATE [dbo].[JobOffer] SET [SortOrder] = 0;');
END
GO

IF COL_LENGTH(N'dbo.JobOffer', N'DiscardReason') IS NULL
    ALTER TABLE [dbo].[JobOffer] ADD [DiscardReason] NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.JobOffer', N'PriorityScore') IS NULL
    ALTER TABLE [dbo].[JobOffer] ADD [PriorityScore] INT NULL;
IF COL_LENGTH(N'dbo.JobOffer', N'PriorityTier') IS NULL
    ALTER TABLE [dbo].[JobOffer] ADD [PriorityTier] NVARCHAR(1) NULL;
IF COL_LENGTH(N'dbo.JobOffer', N'ScoreBreakdown') IS NULL
    ALTER TABLE [dbo].[JobOffer] ADD [ScoreBreakdown] NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'dbo.JobOffer', N'ScoredAt') IS NULL
    ALTER TABLE [dbo].[JobOffer] ADD [ScoredAt] DATETIME2(3) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobOffer_PriorityScore' AND object_id = OBJECT_ID(N'dbo.JobOffer'))
    EXEC(N'CREATE NONCLUSTERED INDEX [IX_JobOffer_PriorityScore] ON [dbo].[JobOffer] ([PriorityScore]);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobOffer_IsPinned' AND object_id = OBJECT_ID(N'dbo.JobOffer'))
    EXEC(N'CREATE NONCLUSTERED INDEX [IX_JobOffer_IsPinned] ON [dbo].[JobOffer] ([IsPinned]);');
GO

-- Reglas de descarte y umbrales (una sola fila; '{}' = valores por defecto)
IF OBJECT_ID(N'[dbo].[OfferTriageConfig]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OfferTriageConfig] (
        [Id] INT IDENTITY(1, 1) NOT NULL CONSTRAINT [PK_OfferTriageConfig] PRIMARY KEY,
        [SettingsJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_OfferTriageConfig_SettingsJson] DEFAULT (N'{}'),
        [UpdatedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_OfferTriageConfig_UpdatedAt] DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[OfferTriageConfig])
    INSERT INTO [dbo].[OfferTriageConfig] ([SettingsJson]) VALUES (N'{}');
GO
