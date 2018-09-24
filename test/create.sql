
IF NOT EXISTS(SELECT * FROM sys.schemas WHERE [name] = 'Gathering')
BEGIN
	EXEC('CREATE SCHEMA [Gathering] AUTHORIZATION [dbo]')
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Gathering].[Exec_Query_Plans_1]') AND type in (N'U'))
BEGIN
CREATE TABLE [Gathering].[Exec_Query_Plans_1](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[plan_handle] [varbinary](64) NOT NULL,
	[plan_generation_num] [bigint] NOT NULL,
	[statement_start_offset] [int] NOT NULL,
	[statement_end_offset] [int] NOT NULL,
	[dbid] [smallint] NULL,
	[objectId] [int] NULL,
	[query_plan] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[creation_time] [datetime] NULL,
	[correlation_id] [uniqueidentifier] NOT NULL,
	[timestamp] [datetime] NOT NULL,
 CONSTRAINT [PK_Exec_Query_Plans_11] PRIMARY KEY NONCLUSTERED 
(
	[plan_handle] ASC,
	[plan_generation_num] ASC,
	[statement_start_offset] ASC,
	[statement_end_offset] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Gathering].[Exec_Query_Plans_1]') AND name = N'IX_Exec_Query_Plans_11')
CREATE UNIQUE CLUSTERED INDEX [IX_Exec_Query_Plans_11] ON [Gathering].[Exec_Query_Plans_1]
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Gathering].[Exec_Query_Plans_1]') AND name = N'IX_Exec_Query_Plans_1')
CREATE NONCLUSTERED INDEX [IX_Exec_Query_Plans_1] ON [Gathering].[Exec_Query_Plans_1]
(
	[plan_handle] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Gathering].[TIMESTAMP_DEFAULT]') AND type = 'D')
BEGIN
ALTER TABLE [Gathering].[Exec_Query_Plans_1] ADD DEFAULT (getdate()) FOR [timestamp]
END


GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TABLE1]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[TABLE1](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[NAME] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
) ON [PRIMARY]
END
GO
DROP PROCEDURE IF EXISTS [dbo].[GET_ITEMS]
GO
create procedure GET_ITEMS
AS
    SELECT ID, NAME FROM TABLE1 


GO
DROP PROCEDURE IF EXISTS [dbo].[NEW_ITEM]
GO
create procedure NEW_ITEM(@NAME NVARCHAR(255))
AS
    INSERT INTO TABLE1 (NAME) VALUES (@NAME)


GO

create view view_1
as
SELECT        TABLE1.*
FROM            TABLE1


GO

CREATE FUNCTION FFF
(	
	
)
RETURNS TABLE 
RETURN 
(
	-- Add the SELECT statement with parameter references here
	SELECT 0 AS S
)
GO

