USE [StudentManagementDB]
GO

PRINT 'Migrating Exams.ExamTime to StartTime/EndTime...'
GO

IF COL_LENGTH('dbo.Exams', 'StartTime') IS NULL
BEGIN
    ALTER TABLE [dbo].[Exams] ADD [StartTime] [datetime] NULL;
END
GO

IF COL_LENGTH('dbo.Exams', 'EndTime') IS NULL
BEGIN
    ALTER TABLE [dbo].[Exams] ADD [EndTime] [datetime] NULL;
END
GO

IF COL_LENGTH('dbo.Exams', 'ExamTime') IS NOT NULL
BEGIN
    UPDATE [dbo].[Exams]
    SET
        [StartTime] = ISNULL([StartTime], [ExamTime]),
        [EndTime] = ISNULL([EndTime], DATEADD(hour, 2, [ExamTime]))
    WHERE [StartTime] IS NULL OR [EndTime] IS NULL;
END
GO

IF EXISTS (SELECT 1 FROM [dbo].[Exams] WHERE [StartTime] IS NULL OR [EndTime] IS NULL)
BEGIN
    THROW 51000, 'Cannot migrate Exams: StartTime or EndTime still contains NULL values.', 1;
END
GO

IF EXISTS (SELECT 1 FROM [dbo].[Exams] WHERE [EndTime] <= [StartTime])
BEGIN
    THROW 51001, 'Cannot migrate Exams: EndTime must be later than StartTime.', 1;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'[dbo].[Exams]')
      AND [name] = N'StartTime'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE [dbo].[Exams] ALTER COLUMN [StartTime] [datetime] NOT NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'[dbo].[Exams]')
      AND [name] = N'EndTime'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE [dbo].[Exams] ALTER COLUMN [EndTime] [datetime] NOT NULL;
END
GO

IF COL_LENGTH('dbo.Exams', 'ExamTime') IS NOT NULL
BEGIN
    DECLARE @dropDefaultSql nvarchar(max) = N'';

    SELECT @dropDefaultSql = @dropDefaultSql + N'ALTER TABLE [dbo].[Exams] DROP CONSTRAINT [' + dc.[name] + N'];'
    FROM sys.default_constraints AS dc
    INNER JOIN sys.columns AS c
        ON c.[default_object_id] = dc.[object_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[Exams]')
      AND c.[name] = N'ExamTime';

    IF @dropDefaultSql <> N''
    BEGIN
        EXEC sp_executesql @dropDefaultSql;
    END

    ALTER TABLE [dbo].[Exams] DROP COLUMN [ExamTime];
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_Exams_TimeRange'
      AND parent_object_id = OBJECT_ID(N'[dbo].[Exams]')
)
BEGIN
    ALTER TABLE [dbo].[Exams] WITH CHECK
    ADD CONSTRAINT [CK_Exams_TimeRange] CHECK ([EndTime] > [StartTime]);
END
GO

PRINT 'Exam time range migration completed.'
GO
