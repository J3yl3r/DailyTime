-- Script de migración: análisis de ofertas con IA (Gemini).
-- Idempotente. El índice va en EXEC para que también funcione en un solo lote (sin GO).

IF COL_LENGTH(N'dbo.JobOffer', N'AiAnalysis') IS NULL
    ALTER TABLE [dbo].[JobOffer] ADD [AiAnalysis] NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'dbo.JobOffer', N'AiAnalyzedAt') IS NULL
    ALTER TABLE [dbo].[JobOffer] ADD [AiAnalyzedAt] DATETIME2(3) NULL;
IF COL_LENGTH(N'dbo.JobOffer', N'AiModel') IS NULL
    ALTER TABLE [dbo].[JobOffer] ADD [AiModel] NVARCHAR(60) NULL;
IF COL_LENGTH(N'dbo.JobOffer', N'AiError') IS NULL
    ALTER TABLE [dbo].[JobOffer] ADD [AiError] NVARCHAR(300) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobOffer_AiAnalyzedAt' AND object_id = OBJECT_ID(N'dbo.JobOffer'))
    EXEC(N'CREATE NONCLUSTERED INDEX [IX_JobOffer_AiAnalyzedAt] ON [dbo].[JobOffer] ([AiAnalyzedAt]);');
GO
