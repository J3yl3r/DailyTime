-- Script de migración: integración bidireccional con Google Calendar.
-- Idempotente. Los índices van en EXEC para que también funcione en un solo lote (sin GO).

-- Los índices filtrados exigen QUOTED_IDENTIFIER ON, y sqlcmd conecta con OFF. Se fija aquí,
-- al principio de la sesión: el EXEC de más abajo abre un lote nuevo y hereda este valor.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.GoogleCalendarAccount', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[GoogleCalendarAccount] (
        [Id]                    INT IDENTITY(1,1) NOT NULL,
        [Email]                 NVARCHAR(320)  NOT NULL,
        [AccessToken]           NVARCHAR(2048) NOT NULL,
        [RefreshToken]          NVARCHAR(512)  NOT NULL,
        [AccessTokenExpiresAt]  DATETIME2(3)   NOT NULL,
        [CalendarId]            NVARCHAR(320)  NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_CalendarId] DEFAULT (N'primary'),
        [PullCutoffAt]          DATETIME2(3)   NULL,
        [TimeZoneId]            NVARCHAR(64)   NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_TimeZoneId] DEFAULT (N'America/Bogota'),
        [SyncEnabled]           BIT            NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_SyncEnabled] DEFAULT (1),
        [SyncTimedTasks]        BIT            NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_SyncTimedTasks] DEFAULT (1),
        [SyncAllDayTasks]       BIT            NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_SyncAllDayTasks] DEFAULT (1),
        [SyncTimedNotes]        BIT            NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_SyncTimedNotes] DEFAULT (1),
        [PastDays]              INT            NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_PastDays] DEFAULT (30),
        [FutureDays]            INT            NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_FutureDays] DEFAULT (180),
        [LastSyncAt]            DATETIME2(3)   NULL,
        [LastSyncStatus]        NVARCHAR(20)   NULL,
        [LastSyncMessage]       NVARCHAR(1000) NULL,
        [LastPushedCount]       INT            NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_LastPushedCount] DEFAULT (0),
        [LastPulledCount]       INT            NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_LastPulledCount] DEFAULT (0),
        [ConnectedAt]           DATETIME2(3)   NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_ConnectedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]             DATETIME2(3)   NOT NULL CONSTRAINT [DF_GoogleCalendarAccount_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_GoogleCalendarAccount] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_GoogleCalendarAccount_Window] CHECK ([PastDays] >= 0 AND [FutureDays] >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.GoogleSyncDeletion', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[GoogleSyncDeletion] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [GoogleEventId] NVARCHAR(300) NOT NULL,
        [CalendarId]    NVARCHAR(320) NOT NULL CONSTRAINT [DF_GoogleSyncDeletion_CalendarId] DEFAULT (N'primary'),
        [DeletedAt]     DATETIME2(3)  NOT NULL CONSTRAINT [DF_GoogleSyncDeletion_DeletedAt] DEFAULT (SYSUTCDATETIME()),
        [Attempts]      INT           NOT NULL CONSTRAINT [DF_GoogleSyncDeletion_Attempts] DEFAULT (0),
        [LastError]     NVARCHAR(500) NULL,
        CONSTRAINT [PK_GoogleSyncDeletion] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GoogleSyncDeletion_GoogleEventId' AND object_id = OBJECT_ID(N'dbo.GoogleSyncDeletion'))
    EXEC(N'CREATE NONCLUSTERED INDEX [IX_GoogleSyncDeletion_GoogleEventId] ON [dbo].[GoogleSyncDeletion] ([GoogleEventId]);');
GO

-- Marcas de correlación en los elementos que se sincronizan.
IF COL_LENGTH(N'dbo.TaskItem', N'GoogleEventId') IS NULL
    ALTER TABLE [dbo].[TaskItem] ADD [GoogleEventId] NVARCHAR(300) NULL;
IF COL_LENGTH(N'dbo.TaskItem', N'GoogleEtag') IS NULL
    ALTER TABLE [dbo].[TaskItem] ADD [GoogleEtag] NVARCHAR(100) NULL;
IF COL_LENGTH(N'dbo.TaskItem', N'GoogleSyncedAt') IS NULL
    ALTER TABLE [dbo].[TaskItem] ADD [GoogleSyncedAt] DATETIME2(3) NULL;
IF COL_LENGTH(N'dbo.TaskItem', N'GoogleUpdatedAt') IS NULL
    ALTER TABLE [dbo].[TaskItem] ADD [GoogleUpdatedAt] DATETIME2(3) NULL;
IF COL_LENGTH(N'dbo.TaskItem', N'SyncSource') IS NULL
    ALTER TABLE [dbo].[TaskItem] ADD [SyncSource] NVARCHAR(20) NULL;
GO

IF COL_LENGTH(N'dbo.Note', N'GoogleEventId') IS NULL
    ALTER TABLE [dbo].[Note] ADD [GoogleEventId] NVARCHAR(300) NULL;
IF COL_LENGTH(N'dbo.Note', N'GoogleEtag') IS NULL
    ALTER TABLE [dbo].[Note] ADD [GoogleEtag] NVARCHAR(100) NULL;
IF COL_LENGTH(N'dbo.Note', N'GoogleSyncedAt') IS NULL
    ALTER TABLE [dbo].[Note] ADD [GoogleSyncedAt] DATETIME2(3) NULL;
IF COL_LENGTH(N'dbo.Note', N'GoogleUpdatedAt') IS NULL
    ALTER TABLE [dbo].[Note] ADD [GoogleUpdatedAt] DATETIME2(3) NULL;
IF COL_LENGTH(N'dbo.Note', N'SyncSource') IS NULL
    ALTER TABLE [dbo].[Note] ADD [SyncSource] NVARCHAR(20) NULL;
GO

-- Un evento de Google no puede estar ligado a dos elementos: índice único filtrado.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TaskItem_GoogleEventId' AND object_id = OBJECT_ID(N'dbo.TaskItem'))
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX [IX_TaskItem_GoogleEventId] ON [dbo].[TaskItem] ([GoogleEventId]) WHERE [GoogleEventId] IS NOT NULL;');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Note_GoogleEventId' AND object_id = OBJECT_ID(N'dbo.Note'))
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX [IX_Note_GoogleEventId] ON [dbo].[Note] ([GoogleEventId]) WHERE [GoogleEventId] IS NOT NULL;');
GO
