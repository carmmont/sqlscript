IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Gathering')
EXEC sys.sp_executesql N'CREATE SCHEMA [Gathering]'

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IDX_HISTORY]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[IDX_HISTORY](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL,
	[Table] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Index] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Fragmentation] [decimal](18, 2) NOT NULL,
	[Pages] [decimal](18, 2) NOT NULL,
	[TimeTakenMs] [int] NOT NULL,
	[command] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Start] [int] NOT NULL,
	[IDX] [int] NOT NULL,
 CONSTRAINT [PK_IDX_HISTORY] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
IF NOT EXISTS(SELECT value FROM fn_listextendedproperty('FILE VERSION', 'schema', 'dbo', 'TABLE', 'IDX_HISTORY', NULL, NULL))
BEGIN
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.4.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'IDX_HISTORY';

EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'IDX_HISTORY';


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
IF NOT EXISTS(SELECT value FROM fn_listextendedproperty('FILE VERSION', 'schema', 'dbo', 'TABLE', 'TABLE1', NULL, NULL))
BEGIN
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.4.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TABLE1';

EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TABLE1';


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
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Gathering].[DF__Exec_Quer__times__37A5467C]') AND type = 'D')
BEGIN
ALTER TABLE [Gathering].[Exec_Query_Plans_1] ADD CONSTRAINT DF__Exec_Quer__times__37A5467C DEFAULT (getdate()) FOR [timestamp]
END

GO
IF NOT EXISTS(SELECT value FROM fn_listextendedproperty('FILE VERSION', 'schema', 'Gathering', 'TABLE', 'Exec_Query_Plans_1', NULL, NULL))
BEGIN
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.4.0' , @level0type=N'SCHEMA',@level0name=N'Gathering', @level1type=N'TABLE',@level1name=N'Exec_Query_Plans_1';

EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'Gathering', @level1type=N'TABLE',@level1name=N'Exec_Query_Plans_1';


END
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[refresh]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[refresh]
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[refresh]
AS
BEGIN
    --- MMM 6
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	exec sp_refreshview 'view_1'
    exec dbmaint.Gathering.collect_all
END





GO
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.9.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'refresh';
GO
EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'refresh';
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DBA_REORGANIZE_INDEXES]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[DBA_REORGANIZE_INDEXES]
GO

/*
EXEC DBA_REORGANIZE_INDEXES 1, 2
EXEC DBA_REORGANIZE_INDEXES 2, 2
*/
CREATE PROCEDURE DBA_REORGANIZE_INDEXES
  @STARTIDX  INT = 1
, @STEP INT = 1
AS
/**/

/*
DECLARE @STARTIDX  INT = 3
DECLARE @STEP      INT = 3
*/



SET NOCOUNT ON;

DECLARE @MINPAGES  INT = 200
DECLARE @INDEXFRAG INT = 10
DECLARE @MAXSEC    INT = 7200


DECLARE @SQL NVARCHAR(MAX)

IF not exists(select * from sys.objects where [type] = 'U' and [Name] = 'IDX_HISTORY')
BEGIN


CREATE TABLE [IDX_HISTORY](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL,
	[Table] [nvarchar](400) NOT NULL,
	[Index] [nvarchar](400) NOT NULL,
	[Fragmentation] [decimal](18, 2) NOT NULL,
	[Pages] [decimal](18, 2) NOT NULL,
	[TimeTakenMs] [int] NOT NULL,
	[command] [nvarchar](max) NOT NULL,
    [Start] INT NOT NULL,
    [IDX]  INT NOT NULL,
 CONSTRAINT [PK_IDX_HISTORY] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
) )


END



DECLARE @TABLES TABLE (
    [ID] [INT] NOT NULL IDENTITY(1,1),
	[object_id] [int] NOT NULL,
	[TableName] [nvarchar](400) NOT NULL
) 



DECLARE @DTB DATETIME = GETUTCDATE();

INSERT @TABLES
(
      [object_id]
     ,[TableName]
   

)
SELECT [object_id], [Name] from sys.objects where type = 'U'


DECLARE @IDX INT = @STARTIDX
DECLARE @ROW INT
DECLARE @IDX2 INT 
DECLARE @ROW2 INT
DECLARE @TableName NVARCHAR(400)
DECLARE @TN NVARCHAR(400)
DECLARE @Index NVARCHAR(400)
DECLARE @MS INT = 0
DECLARE @OBJID INT
DECLARE @Fragmentation [decimal](18, 2) 
DECLARE @Pages decimal(18, 2)

DECLARE @IDX_IDX INT
DECLARE @ROW_IDX INT

DECLARE @INDEXES TABLE (
    [ID] [INT] NOT NULL IDENTITY(1,1),
	
	[TableName] [nvarchar](400) NOT NULL,
    [IndexName] [nvarchar](400)  NULL, 
    [avg_fragmentation] float  NULL,
    page_count bigint  NULL
    )

 

SELECT @ROW = COUNT(*) FROM @TABLES

WHILE @IDX <= @ROW AND @MAXSEC > DATEDIFF(SS, @DTB, GETUTCDATE())
BEGIN

     SELECT @OBJID = [object_id]
            , @TN = [TableName]
     FROM @TABLES WHERE ID = @IDX

     
       
            SET @INDEX = NULL
    
    DELETE @INDEXES

    INSERT @INDEXES ( [TableName] , [IndexName] , [avg_fragmentation] , page_count)
    SELECT '[' + SNAME + '].[' + TNAME + ']'
                  , Ind.name
                  , indexstats.avg_fragmentation_in_percent
                  ,  page_count
    FROM sys.dm_db_index_physical_stats(DB_ID(), @OBJID, NULL, NULL, NULL) indexstats
    INNER JOIN sys.indexes ind 
    ON ind.object_id = indexstats.object_id
    AND ind.index_id = indexstats.index_id
    INNER JOIN
    (select S.NAME AS SNAME, O.NAME AS TNAME
     from sys.objects O 
    inner join sys.schemas S ON O.schema_id = S.schema_id 
    where type = 'U') K ON K.TNAME = OBJECT_NAME(ind.OBJECT_ID)
    LEFT JOIN msdb.sys.partitions p ON ind.object_id = p.OBJECT_ID  
                                                AND ind.index_id = p.index_id  
            LEFT JOIN msdb.sys.allocation_units a ON p.partition_id = a.container_id
    WHERE page_count > @MINPAGES  and indexstats.avg_fragmentation_in_percent >= @INDEXFRAG
    and ind.name is not null
    and ind.object_id = @OBJID

    SELECT @ROW_IDX = COUNT(*) FROM @INDEXES

    SET @IDX_IDX = 1

    WHILE(@IDX_IDX <= @ROW_IDX)
    BEGIN


            SELECT @TableName = [TableName] 
                          , @INDEX =  [IndexName]
                          , @Fragmentation = [avg_fragmentation]
                          , @Pages  =  page_count
            FROM @INDEXES WHERE ID = @IDX_IDX
            

            IF @INDEX IS NOT NULL
            BEGIN

                    SET @SQL = 'ALTER INDEX [' + @INDEX + '] ON ' + @TableName + ' REORGANIZE;'

                    SET @MS = 0
           
                           --PRINT @SQL + '   -- ' +  @TN + ' ' + CONVERT(NVARCHAR(255), ISNULL(@OBJID, 0)) + ' ... ' + CONVERT(NVARCHAR(255), ISNULL(@IDX2, 0)) + ' ... ' + CONVERT(NVARCHAR(255), ISNULL(@IDX, 0))+ ' ... ' + CONVERT(NVARCHAR(255), ISNULL(@ROW2, 0))
       
                            DECLARE @DT DATETIME = GETUTCDATE()

                            --PRINT @SQL

                            EXEC (@SQL)

                            SET @MS = DATEDIFF(MS, @DT, GETUTCDATE())

                            --PRINT @MS
        
                            INSERT INTO [dbo].[IDX_HISTORY]
                                ( [Date]  ,
	                              [Table] ,
	                              [Index] ,
	                              [Fragmentation] ,
	                              [Pages] ,
	                              [TimeTakenMs] ,
	                              [command],
                                  [Start],
                                  [IDX])
                            SELECT GETUTCDATE()
                            , @TABLENAME
                            , @INDEX
                            , @Fragmentation
                            , @Pages
                            , @ms
                            , @SQL
                            , @STARTIDX
                            , @IDX

   
                END
			 SET @IDX_IDX = @IDX_IDX + 1
        END

       
           
    

    SET @IDX = @IDX + @STEP

END


     
    

        
        







GO
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.7.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'DBA_REORGANIZE_INDEXES';
GO
EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'DBA_REORGANIZE_INDEXES';
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GET_ITEMS]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[GET_ITEMS]
GO
create procedure GET_ITEMS
AS
    SELECT ID, NAME FROM TABLE1 








GO
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.7.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'GET_ITEMS';
GO
EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'GET_ITEMS';
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NEW_ITEM]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[NEW_ITEM]
GO
create procedure NEW_ITEM(@NAME NVARCHAR(255))
AS
    INSERT INTO TABLE1 (NAME) VALUES (@NAME)







GO
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.7.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'NEW_ITEM';
GO
EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'NEW_ITEM';
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[num_echo]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[num_echo]
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION num_echo 
(
	@n int
)
RETURNS int
AS
BEGIN
	RETURN @N

END





GO
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.4.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'FUNCTION',@level1name=N'num_echo';
GO
EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'FUNCTION',@level1name=N'num_echo';
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FFF]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[FFF]
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
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.4.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'FUNCTION',@level1name=N'FFF';
GO
EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'FUNCTION',@level1name=N'FFF';
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[test_coverage]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[test_coverage]
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[test_coverage]
	-- Add the parameters for the stored procedure here
	@full int = 0
AS
BEGIN
    /*COMMENT
    *
    *
    *
    */exec GET_ITEMS
    if @full > 0
    BEGIN
        select * from FFF()
    END

    PRINT @full

    if @full > 1
    BEGIN
    -------------------------
        SELECT dbo.num_echo(@full)
    END
END





GO
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.7.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'test_coverage';
GO
EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'PROCEDURE',@level1name=N'test_coverage';
GO
IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[view_1]'))
DROP VIEW [dbo].[view_1]
GO
create view view_1
as
SELECT        TABLE1.*
FROM            TABLE1





GO
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.4.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'view_1';
GO
EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'view_1';
GO
IF  EXISTS (SELECT * FROM sys.synonyms WHERE name = N'TABLE2' AND schema_id = SCHEMA_ID(N'dbo'))
DROP SYNONYM [dbo].[TABLE2]
GO
CREATE SYNONYM [dbo].[TABLE2] FOR [TESTDB].[dbo].[TABLE1]
GO
EXEC sys.sp_addextendedproperty @name=N'FILE VERSION', @value=N'0.0.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'SYNONYM',@level1name=N'TABLE2';
GO
EXEC sys.sp_addextendedproperty @name=N'DATABASE VERSION', @value=N'0.9.0.0' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'SYNONYM',@level1name=N'TABLE2';
GO
