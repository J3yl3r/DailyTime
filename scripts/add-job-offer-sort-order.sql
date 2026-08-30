-- Script de migración: Agrega SortOrder a la tabla JobOffer si no existe
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[JobOffer]') 
      AND name = 'SortOrder'
)
BEGIN
    ALTER TABLE [dbo].[JobOffer] 
    ADD [SortOrder] INT NOT NULL CONSTRAINT [DF_JobOffer_SortOrder] DEFAULT (0);

    CREATE NONCLUSTERED INDEX [IX_JobOffer_SortOrder] 
    ON [dbo].[JobOffer] ([SortOrder] ASC);
END
GO
