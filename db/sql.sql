USE [master]
GO

PRINT '========================================='
PRINT '1. Rebuilding [StudentManagementDB]...'
PRINT '========================================='
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'StudentManagementDB')
BEGIN
    ALTER DATABASE [StudentManagementDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [StudentManagementDB];
END
GO

CREATE DATABASE [StudentManagementDB]
GO
ALTER DATABASE [StudentManagementDB] SET COMPATIBILITY_LEVEL = 160
GO

USE [StudentManagementDB]
GO

PRINT '========================================='
PRINT '2. Creating tables and constraints...'
PRINT '========================================='

CREATE TABLE [dbo].[Classes](
    [ClassID] [int] IDENTITY(1,1) NOT NULL,
    [ClassName] [nvarchar](100) NOT NULL,
    [Major] [nvarchar](100) NULL,
    [AcademicYear] [int] NULL,
    [ClassNumber] [int] NULL,
PRIMARY KEY CLUSTERED ([ClassID] ASC),
UNIQUE NONCLUSTERED ([ClassName] ASC)
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Users](
    [UserID] [int] IDENTITY(1,1) NOT NULL,
    [Username] [nvarchar](50) NOT NULL,
    [Password] [nvarchar](100) NOT NULL,
    [Role] [int] NOT NULL,
    [PasskeyCredentialId] [varbinary](max) NULL,
    [PasskeyPublicKey] [nvarchar](max) NULL,
    [PasskeySignCount] [int] NULL,
    [PasskeyEnabled] [bit] NOT NULL CONSTRAINT [DF_Users_PasskeyEnabled] DEFAULT ((0)),
PRIMARY KEY CLUSTERED ([UserID] ASC),
UNIQUE NONCLUSTERED ([Username] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Teachers](
    [TeacherID] [nvarchar](20) NOT NULL,
    [TeacherName] [nvarchar](50) NOT NULL,
    [Title] [nvarchar](50) NULL,
    [UserID] [int] NOT NULL,
PRIMARY KEY CLUSTERED ([TeacherID] ASC)
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Students](
    [StudentID] [nvarchar](20) NOT NULL,
    [StudentName] [nvarchar](50) NOT NULL,
    [Gender] [nvarchar](2) NULL,
    [ClassID] [int] NULL,
    [UserID] [int] NOT NULL,
PRIMARY KEY CLUSTERED ([StudentID] ASC)
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Courses](
    [CourseID] [int] IDENTITY(1,1) NOT NULL,
    [CourseName] [nvarchar](100) NOT NULL,
    [Credits] [float] NOT NULL,
    [TeacherID] [nvarchar](20) NULL,
    [CourseType] [int] NOT NULL CONSTRAINT [DF_Courses_CourseType] DEFAULT ((1)),
PRIMARY KEY CLUSTERED ([CourseID] ASC)
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ClassSessions](
    [SessionID] [int] IDENTITY(1,1) NOT NULL,
    [CourseID] [int] NOT NULL,
    [DayOfWeek] [int] NOT NULL,
    [StartPeriod] [int] NOT NULL,
    [EndPeriod] [int] NOT NULL,
    [Classroom] [nvarchar](100) NULL,
    [StartWeek] [int] NOT NULL CONSTRAINT [DF_ClassSessions_StartWeek] DEFAULT ((1)),
    [EndWeek] [int] NOT NULL CONSTRAINT [DF_ClassSessions_EndWeek] DEFAULT ((21)),
PRIMARY KEY CLUSTERED ([SessionID] ASC)
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Exams](
    [ExamID] [int] IDENTITY(1,1) NOT NULL,
    [CourseID] [int] NOT NULL,
    [StartTime] [datetime] NOT NULL,
    [EndTime] [datetime] NOT NULL,
    [Location] [nvarchar](100) NULL,
    [Details] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED ([ExamID] ASC),
CONSTRAINT [CK_Exams_TimeRange] CHECK ([EndTime] > [StartTime])
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[StudentCourses](
    [SC_ID] [int] IDENTITY(1,1) NOT NULL,
    [StudentID] [nvarchar](20) NOT NULL,
    [CourseID] [int] NOT NULL,
    [Grade] [float] NULL,
PRIMARY KEY CLUSTERED ([SC_ID] ASC),
CONSTRAINT [UQ_Student_Course] UNIQUE NONCLUSTERED ([StudentID] ASC, [CourseID] ASC)
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Passkeys](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [UserId] [int] NOT NULL,
    [CredentialId] [varbinary](max) NOT NULL,
    [PublicKey] [varbinary](max) NOT NULL,
    [UserHandle] [varbinary](max) NOT NULL,
    [SignatureCounter] [bigint] NOT NULL,
    [CredType] [nvarchar](50) NOT NULL,
    [RegDate] [datetime] NOT NULL CONSTRAINT [DF_Passkeys_RegDate] DEFAULT (getdate()),
    [AaGuid] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[ClassSessions] WITH CHECK ADD CONSTRAINT [FK_ClassSessions_Courses] FOREIGN KEY([CourseID]) REFERENCES [dbo].[Courses] ([CourseID])
ALTER TABLE [dbo].[Courses] WITH CHECK ADD CONSTRAINT [FK_Courses_Teachers] FOREIGN KEY([TeacherID]) REFERENCES [dbo].[Teachers] ([TeacherID])
ALTER TABLE [dbo].[Exams] WITH CHECK ADD CONSTRAINT [FK_Exams_Courses] FOREIGN KEY([CourseID]) REFERENCES [dbo].[Courses] ([CourseID])
ALTER TABLE [dbo].[Passkeys] WITH CHECK ADD CONSTRAINT [FK_Passkeys_Users] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserID]) ON DELETE CASCADE
ALTER TABLE [dbo].[StudentCourses] WITH CHECK ADD CONSTRAINT [FK_StudentCourses_Courses] FOREIGN KEY([CourseID]) REFERENCES [dbo].[Courses] ([CourseID])
ALTER TABLE [dbo].[StudentCourses] WITH CHECK ADD CONSTRAINT [FK_StudentCourses_Students] FOREIGN KEY([StudentID]) REFERENCES [dbo].[Students] ([StudentID])
ALTER TABLE [dbo].[Students] WITH CHECK ADD CONSTRAINT [FK_Students_Classes] FOREIGN KEY([ClassID]) REFERENCES [dbo].[Classes] ([ClassID])
ALTER TABLE [dbo].[Students] WITH CHECK ADD CONSTRAINT [FK_Students_Users] FOREIGN KEY([UserID]) REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Teachers] WITH CHECK ADD CONSTRAINT [FK_Teachers_Users] FOREIGN KEY([UserID]) REFERENCES [dbo].[Users] ([UserID])
GO

PRINT '========================================='
PRINT '3. Inserting current seed data (passwords are PBKDF2 hashes of 123456)...'
PRINT '========================================='

-- 角色对应: 0=管理员, 1=教师, 2=学生
INSERT INTO [dbo].[Users] ([Username], [Password], [Role], [PasskeyEnabled])
VALUES
('admin', 'PBKDF2$100000$5+JzqdMMQCX9HvxOim0KsQ==$V9eH40Zzi2EpmZFmNwLgvWXks346PTc98YNEauMK3SM=', 0, 0),
('T001', 'PBKDF2$100000$blGwt7BiITiW99qYt0oSgA==$qdoIF9lxia2n/pr98pQnGTFRg9YJWi1suFpss55inYM=', 1, 0),
('T002', 'PBKDF2$100000$SluJJXIthLQidlLKRaWERA==$3I1IcZdwO4C/dZ3XEN19o24gULHLwFLRDp3Z1999LCE=', 1, 0),
('T003', 'PBKDF2$100000$WkLNXPapmS/BW0/iroMlaw==$EY3RgKf17V34n1CrRKN8cjZYOqrqrKkiA0bL1VafBnI=', 1, 0),
('T004', 'PBKDF2$100000$GYryCCOLCVNBcBiWXqKOvA==$cMpLP1bm8it9nWyeuF3hULW+BJPR9qQetp+relC5BmM=', 1, 0),
('T005', 'PBKDF2$100000$3fVQ5IwCWKE4udMPsXFkTw==$XcwbLT/x4iu/3JA9nvA+x6peTB4uhJ1LutN1H2/WA5w=', 1, 0),
('S2101001', 'PBKDF2$100000$kvcio9I6VypZ8GWstjBCYg==$4RDTrOxcCfaZyBP8sH7MDgk5ICTWjbIfdN5Qyt8pNqw=', 2, 0),
('S2101002', 'PBKDF2$100000$QXhkyYhQ1Xo9VccDND0FjQ==$FxKuVPUhw1gZJSkKspONrhDCfnLeMV0sBH56ZCnTXYA=', 2, 0),
('S2102001', 'PBKDF2$100000$C1xRCbzSjP0I0Cpgr3eQBg==$y8iYfQMHJKbC8uq6A0/Yn4pQ29wt8KWcPsxi9UsrHB8=', 2, 0);

INSERT INTO [dbo].[Classes] ([ClassName], [Major], [AcademicYear], [ClassNumber])
VALUES
('软件工程1班', '软件工程', 2021, 1),
('计算机科学1班', '计算机科学与技术', 2021, 1);

INSERT INTO [dbo].[Teachers] ([TeacherID], [TeacherName], [Title], [UserID])
VALUES
('T001', '张建国', '教授', 2),
('T002', '王丽', '副教授', 3),
('T003', '赵铁柱', '讲师', 4),
('T004', '马冬梅', '教授', 5),
('T005', '苏炳添', '讲师', 6);

INSERT INTO [dbo].[Students] ([StudentID], [StudentName], [Gender], [ClassID], [UserID])
VALUES
('S2101001', '李华', '男', 1, 7),
('S2101002', '韩梅梅', '女', 1, 8),
('S2102001', '刘星', '男', 2, 9);

-- CourseType: 1=专业必修, 2=公共/思政必修, 3=专业选修, 4=其他公共选修, 5=体育选修
INSERT INTO [dbo].[Courses] ([CourseName], [Credits], [TeacherID], [CourseType])
VALUES
('C# 高级程序设计', 4.0, 'T001', 1),
('数据库系统原理', 3.0, 'T002', 1),
('毛泽东思想和中国特色社会主义理论体系概论', 3.0, 'T004', 2),
('大学英语(三)', 2.0, 'T003', 2),
('体育选修：篮球', 1.0, 'T005', 5),
('体育选修：足球', 1.0, 'T005', 5),
('体育选修：乒乓球', 1.0, 'T005', 5),
('Python 数据分析基础', 2.0, 'T002', 3),
('大学生心理健康与心理调适', 2.0, 'T003', 4),
('影视鉴赏与美学', 1.5, 'T003', 4);

INSERT INTO [dbo].[ClassSessions] ([CourseID], [DayOfWeek], [StartPeriod], [EndPeriod], [Classroom], [StartWeek], [EndWeek])
VALUES
(1, 1, 1, 2, '一教-101', 1, 18),
(2, 2, 3, 4, '二教-205', 1, 18),
(3, 3, 5, 6, '阶梯教室A', 1, 16),
(4, 4, 1, 2, '三教-302', 1, 16),
(5, 5, 7, 8, '室外篮球场', 1, 16),
(6, 5, 7, 8, '操场', 1, 16),
(7, 5, 7, 8, '体育馆二楼', 1, 16),
(8, 2, 7, 8, '机房-401', 1, 10),
(9, 4, 9, 10, '阶梯教室B', 5, 15),
(10, 5, 1, 2, '多媒体教室', 1, 16);

INSERT INTO [dbo].[StudentCourses] ([StudentID], [CourseID], [Grade])
VALUES
('S2101001', 1, NULL),
('S2101001', 2, NULL),
('S2101001', 3, NULL),
('S2101001', 4, NULL),
('S2101001', 5, NULL),
('S2101001', 9, NULL),
('S2101002', 1, 95.0),
('S2101002', 2, 89.0),
('S2101002', 10, NULL),
('S2102001', 3, 76.5),
('S2102001', 8, NULL);

INSERT INTO [dbo].[Exams] ([CourseID], [StartTime], [EndTime], [Location], [Details])
VALUES
(1, '2026-06-15 09:00:00', '2026-06-15 11:00:00', '一教-101', '期末闭卷考试，请携带学生证。'),
(3, '2026-06-17 14:00:00', '2026-06-17 16:00:00', '二教-205', '上机考试。'),
(8, '2026-06-20 10:00:00', '2026-06-20 12:00:00', '机房-401', 'Python大作业验收。');

PRINT '========================================='
PRINT '4. Database script completed.'
PRINT '========================================='
GO
