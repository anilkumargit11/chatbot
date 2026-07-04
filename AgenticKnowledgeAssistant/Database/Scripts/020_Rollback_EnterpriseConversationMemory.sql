SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tblAI_ConversationMessages', N'U') IS NOT NULL
    DROP TABLE dbo.tblAI_ConversationMessages;
GO

IF OBJECT_ID(N'dbo.tblAI_ConversationSessions', N'U') IS NOT NULL
    DROP TABLE dbo.tblAI_ConversationSessions;
GO
