-- Script de migración: color del evento de Google en tareas y notas.
-- Guarda el hexadecimal ya resuelto (el que el usuario eligió en Google, o el del calendario).
-- Idempotente.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF COL_LENGTH(N'dbo.TaskItem', N'GoogleColor') IS NULL
    ALTER TABLE [dbo].[TaskItem] ADD [GoogleColor] NVARCHAR(9) NULL;
GO

IF COL_LENGTH(N'dbo.Note', N'GoogleColor') IS NULL
    ALTER TABLE [dbo].[Note] ADD [GoogleColor] NVARCHAR(9) NULL;
GO

-- Los elementos ya traídos no tienen color hasta que su evento vuelva a cambiar en Google.
-- Vaciar el corte incremental obliga a una pasada completa de la ventana, que los rellena.
UPDATE [dbo].[GoogleCalendarAccount] SET [PullCutoffAt] = NULL;
GO
