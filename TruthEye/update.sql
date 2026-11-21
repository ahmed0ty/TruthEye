IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Universities] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [City] nvarchar(max) NOT NULL,
        [Country] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Universities] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Faculties] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [UniversityId] int NOT NULL,
        CONSTRAINT [PK_Faculties] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Faculties_Universities_UniversityId] FOREIGN KEY ([UniversityId]) REFERENCES [Universities] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [FacultyId] int NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Departments_Faculties_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculties] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [Email] nvarchar(450) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [NationalId] nvarchar(450) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [ProfileImage] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [LastLogin] datetime2 NULL,
        [RoleId] int NOT NULL,
        [UserType] nvarchar(13) NOT NULL,
        [UniversityId] int NULL,
        [FacultyId] int NULL,
        [Doctor_University] nvarchar(max) NULL,
        [Doctor_Faculty] nvarchar(max) NULL,
        [Doctor_Department] nvarchar(max) NULL,
        [Doctor_Specialization] nvarchar(max) NULL,
        [AcademicDegree] nvarchar(max) NULL,
        [YearsOfExperience] int NULL,
        [Proctor_Department] nvarchar(max) NULL,
        [IsOnDuty] bit NULL,
        [ActiveSessionsCount] int NULL,
        [LastDutyDate] datetime2 NULL,
        [AverageResponseTime] float NULL,
        [University] nvarchar(max) NULL,
        [Faculty] nvarchar(max) NULL,
        [Student_Department] nvarchar(max) NULL,
        [Year] int NULL,
        [LastSystemBackup] datetime2 NULL,
        [TotalActionsPerformed] int NULL,
        [Department] nvarchar(max) NULL,
        [Specialization] nvarchar(max) NULL,
        [IsOnline] bit NULL,
        [TicketsHandled] int NULL,
        [OpenTickets] int NULL,
        [LastTicketHandled] datetime2 NULL,
        [AverageResolutionTime] float NULL,
        [ShiftTiming] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] int NULL,
        [CreatedBy] int NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Faculties_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculties] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Users_Universities_UniversityId] FOREIGN KEY ([UniversityId]) REFERENCES [Universities] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Courses] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [DepartmentId] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Courses_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [AIConfigs] (
        [Id] int NOT NULL IDENTITY,
        [ParameterName] nvarchar(max) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [Threshold] float NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [SuperAdminId] int NULL,
        CONSTRAINT [PK_AIConfigs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AIConfigs_Users_SuperAdminId] FOREIGN KEY ([SuperAdminId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] int NOT NULL IDENTITY,
        [MessageText] nvarchar(max) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        [SenderId] int NOT NULL,
        [ReceiverId] int NOT NULL,
        [UserId] int NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChatMessages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChatMessages_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [Message] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [IsRead] bit NOT NULL,
        [UserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [SupportTickets] (
        [Id] int NOT NULL IDENTITY,
        [TicketNumber] nvarchar(max) NOT NULL,
        [UserId] int NOT NULL,
        [AssignedToSupportId] int NULL,
        [Category] nvarchar(max) NOT NULL,
        [Priority] nvarchar(max) NOT NULL,
        [Status] nvarchar(450) NOT NULL,
        [Subject] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Resolution] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ResolvedAt] datetime2 NULL,
        CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupportTickets_Users_AssignedToSupportId] FOREIGN KEY ([AssignedToSupportId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_SupportTickets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [SystemLogs] (
        [Id] int NOT NULL IDENTITY,
        [AdminId] int NULL,
        [Action] nvarchar(max) NOT NULL,
        [EntityType] nvarchar(max) NOT NULL,
        [EntityId] int NULL,
        [Description] nvarchar(max) NOT NULL,
        [IpAddress] nvarchar(max) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [SuperAdminId] int NULL,
        CONSTRAINT [PK_SystemLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SystemLogs_Users_SuperAdminId] FOREIGN KEY ([SuperAdminId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [DoctorCourses] (
        [InstructorsId] int NOT NULL,
        [TaughtCoursesId] int NOT NULL,
        CONSTRAINT [PK_DoctorCourses] PRIMARY KEY ([InstructorsId], [TaughtCoursesId]),
        CONSTRAINT [FK_DoctorCourses_Courses_TaughtCoursesId] FOREIGN KEY ([TaughtCoursesId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DoctorCourses_Users_InstructorsId] FOREIGN KEY ([InstructorsId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Exams] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Duration] int NOT NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RandomizeQuestions] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RandomizeOptions] bit NOT NULL DEFAULT CAST(0 AS bit),
        [PassingScore] int NOT NULL DEFAULT 50,
        [TotalMarks] int NOT NULL DEFAULT 0,
        [Instructions] nvarchar(max) NOT NULL,
        [CourseId] int NOT NULL,
        [DoctorId] int NOT NULL,
        CONSTRAINT [PK_Exams] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Exams_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Exams_Users_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [StudentCourses] (
        [RegisteredCoursesId] int NOT NULL,
        [RegisteredStudentsId] int NOT NULL,
        CONSTRAINT [PK_StudentCourses] PRIMARY KEY ([RegisteredCoursesId], [RegisteredStudentsId]),
        CONSTRAINT [FK_StudentCourses_Courses_RegisteredCoursesId] FOREIGN KEY ([RegisteredCoursesId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentCourses_Users_RegisteredStudentsId] FOREIGN KEY ([RegisteredStudentsId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [ExamSessions] (
        [Id] int NOT NULL IDENTITY,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [AttemptNumber] int NOT NULL,
        [AIScore] float NULL,
        [TotalScore] float NOT NULL,
        [TotalMarks] int NOT NULL,
        [Percentage] float NOT NULL,
        [IsPassed] bit NOT NULL,
        [StudentId] int NOT NULL,
        [ExamId] int NOT NULL,
        [AdminId] int NULL,
        [DoctorId] int NULL,
        [ProctorId] int NULL,
        [SuperAdminId] int NULL,
        [SupportId] int NULL,
        CONSTRAINT [PK_ExamSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExamSessions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExamSessions_Users_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_ExamSessions_Users_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_ExamSessions_Users_ProctorId] FOREIGN KEY ([ProctorId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_ExamSessions_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExamSessions_Users_SuperAdminId] FOREIGN KEY ([SuperAdminId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_ExamSessions_Users_SupportId] FOREIGN KEY ([SupportId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [PerformanceReports] (
        [Id] int NOT NULL IDENTITY,
        [Score] float NOT NULL,
        [AIAlertsCount] int NOT NULL,
        [Duration] time NOT NULL,
        [GeneratedAt] datetime2 NOT NULL,
        [StudentId] int NOT NULL,
        [ExamId] int NOT NULL,
        CONSTRAINT [PK_PerformanceReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PerformanceReports_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PerformanceReports_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [ProctorExams] (
        [AssignedExamsId] int NOT NULL,
        [ProctorsId] int NOT NULL,
        CONSTRAINT [PK_ProctorExams] PRIMARY KEY ([AssignedExamsId], [ProctorsId]),
        CONSTRAINT [FK_ProctorExams_Exams_AssignedExamsId] FOREIGN KEY ([AssignedExamsId]) REFERENCES [Exams] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProctorExams_Users_ProctorsId] FOREIGN KEY ([ProctorsId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [ProctorSessions] (
        [Id] int NOT NULL IDENTITY,
        [ProctorId] int NOT NULL,
        [ExamId] int NOT NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NULL,
        [StudentsMonitored] int NOT NULL,
        [AlertsReviewed] int NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ProctorSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProctorSessions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProctorSessions_Users_ProctorId] FOREIGN KEY ([ProctorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Questions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionText] nvarchar(max) NOT NULL,
        [QuestionType] nvarchar(8) NOT NULL,
        [Marks] int NOT NULL,
        [ExamId] int NOT NULL,
        [ModelAnswer] nvarchar(max) NULL,
        [AIThreshold] float NULL,
        [EvaluationModel] nvarchar(max) NULL,
        CONSTRAINT [PK_Questions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Questions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Reports] (
        [Id] int NOT NULL IDENTITY,
        [FilePath] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [GeneratedBy] int NOT NULL,
        [ExamId] int NOT NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reports_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [AILogs] (
        [Id] int NOT NULL IDENTITY,
        [EventType] nvarchar(max) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [ScreenshotPath] nvarchar(max) NOT NULL,
        [Confidence] float NOT NULL,
        [ExamSessionId] int NOT NULL,
        [ProctorId] int NULL,
        CONSTRAINT [PK_AILogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AILogs_ExamSessions_ExamSessionId] FOREIGN KEY ([ExamSessionId]) REFERENCES [ExamSessions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AILogs_Users_ProctorId] FOREIGN KEY ([ProctorId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [Answers] (
        [Id] int NOT NULL IDENTITY,
        [ExamSessionId] int NOT NULL,
        [QuestionId] int NOT NULL,
        [AnswerText] nvarchar(max) NOT NULL,
        [SelectedOptionId] int NULL,
        [IsCorrect] bit NOT NULL,
        [MarksObtained] float NOT NULL,
        [AnsweredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Answers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Answers_ExamSessions_ExamSessionId] FOREIGN KEY ([ExamSessionId]) REFERENCES [ExamSessions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Answers_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE TABLE [MCQOptions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionId] int NOT NULL,
        [OptionText] nvarchar(max) NOT NULL,
        [IsCorrect] bit NOT NULL,
        CONSTRAINT [PK_MCQOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MCQOptions_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([Id], [Name])
    VALUES (1, N''Student''),
    (2, N''Doctor''),
    (3, N''Admin''),
    (4, N''Proctor''),
    (5, N''Support''),
    (6, N''SuperAdmin'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_AIConfigs_SuperAdminId] ON [AIConfigs] ([SuperAdminId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_AILogs_ExamSessionId_Timestamp] ON [AILogs] ([ExamSessionId], [Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_AILogs_ProctorId] ON [AILogs] ([ProctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Answers_ExamSessionId] ON [Answers] ([ExamSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Answers_QuestionId] ON [Answers] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_ReceiverId] ON [ChatMessages] ([ReceiverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SenderId] ON [ChatMessages] ([SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_UserId] ON [ChatMessages] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Courses_DepartmentId] ON [Courses] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Departments_FacultyId] ON [Departments] ([FacultyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_DoctorCourses_TaughtCoursesId] ON [DoctorCourses] ([TaughtCoursesId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Exams_CourseId] ON [Exams] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Exams_DoctorId] ON [Exams] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ExamSessions_AdminId] ON [ExamSessions] ([AdminId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ExamSessions_DoctorId] ON [ExamSessions] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ExamSessions_ExamId] ON [ExamSessions] ([ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ExamSessions_ProctorId] ON [ExamSessions] ([ProctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ExamSessions_StudentId_ExamId] ON [ExamSessions] ([StudentId], [ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ExamSessions_SuperAdminId] ON [ExamSessions] ([SuperAdminId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ExamSessions_SupportId] ON [ExamSessions] ([SupportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Faculties_UniversityId] ON [Faculties] ([UniversityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_MCQOptions_QuestionId] ON [MCQOptions] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_PerformanceReports_ExamId] ON [PerformanceReports] ([ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_PerformanceReports_StudentId] ON [PerformanceReports] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ProctorExams_ProctorsId] ON [ProctorExams] ([ProctorsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ProctorSessions_ExamId] ON [ProctorSessions] ([ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_ProctorSessions_ProctorId] ON [ProctorSessions] ([ProctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Questions_ExamId] ON [Questions] ([ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Reports_ExamId] ON [Reports] ([ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_StudentCourses_RegisteredStudentsId] ON [StudentCourses] ([RegisteredStudentsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_SupportTickets_AssignedToSupportId] ON [SupportTickets] ([AssignedToSupportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_SupportTickets_Status] ON [SupportTickets] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_SupportTickets_UserId] ON [SupportTickets] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_SystemLogs_SuperAdminId] ON [SystemLogs] ([SuperAdminId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Users_FacultyId] ON [Users] ([FacultyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_NationalId] ON [Users] ([NationalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    CREATE INDEX [IX_Users_UniversityId] ON [Users] ([UniversityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030205634_init'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251030205634_init', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [AIConfigs] DROP CONSTRAINT [FK_AIConfigs_Users_SuperAdminId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [AILogs] DROP CONSTRAINT [FK_AILogs_ExamSessions_ExamSessionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Answers] DROP CONSTRAINT [FK_Answers_ExamSessions_ExamSessionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Answers] DROP CONSTRAINT [FK_Answers_Questions_QuestionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ChatMessages] DROP CONSTRAINT [FK_ChatMessages_Users_ReceiverId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ChatMessages] DROP CONSTRAINT [FK_ChatMessages_Users_SenderId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Courses] DROP CONSTRAINT [FK_Courses_Departments_DepartmentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Departments] DROP CONSTRAINT [FK_Departments_Faculties_FacultyId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Exams] DROP CONSTRAINT [FK_Exams_Courses_CourseId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Exams] DROP CONSTRAINT [FK_Exams_Users_DoctorId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ExamSessions] DROP CONSTRAINT [FK_ExamSessions_Exams_ExamId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ExamSessions] DROP CONSTRAINT [FK_ExamSessions_Users_StudentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Faculties] DROP CONSTRAINT [FK_Faculties_Universities_UniversityId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [MCQOptions] DROP CONSTRAINT [FK_MCQOptions_Questions_QuestionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Notifications] DROP CONSTRAINT [FK_Notifications_Users_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [PerformanceReports] DROP CONSTRAINT [FK_PerformanceReports_Users_StudentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ProctorSessions] DROP CONSTRAINT [FK_ProctorSessions_Exams_ExamId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ProctorSessions] DROP CONSTRAINT [FK_ProctorSessions_Users_ProctorId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Questions] DROP CONSTRAINT [FK_Questions_Exams_ExamId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [SupportTickets] DROP CONSTRAINT [FK_SupportTickets_Users_AssignedToSupportId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [SupportTickets] DROP CONSTRAINT [FK_SupportTickets_Users_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Faculties_FacultyId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Universities_UniversityId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Doctor_Department');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Users] DROP COLUMN [Doctor_Department];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Doctor_Faculty');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Users] DROP COLUMN [Doctor_Faculty];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Doctor_University');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Users] DROP COLUMN [Doctor_University];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Faculty');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Users] DROP COLUMN [Faculty];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Student_Department');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Users] DROP COLUMN [Student_Department];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'University');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Users] DROP COLUMN [University];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'AcademicDegree');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Users] ALTER COLUMN [AcademicDegree] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD [Admin_FacultyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD [Admin_UniversityId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD [DepartmentId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD [Doctor_DepartmentId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD [Doctor_FacultyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD [Doctor_UniversityId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    CREATE INDEX [IX_Users_Admin_FacultyId] ON [Users] ([Admin_FacultyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    CREATE INDEX [IX_Users_Admin_UniversityId] ON [Users] ([Admin_UniversityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    CREATE INDEX [IX_Users_DepartmentId] ON [Users] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    CREATE INDEX [IX_Users_Doctor_DepartmentId] ON [Users] ([Doctor_DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    CREATE INDEX [IX_Users_Doctor_FacultyId] ON [Users] ([Doctor_FacultyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    CREATE INDEX [IX_Users_Doctor_UniversityId] ON [Users] ([Doctor_UniversityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [AIConfigs] ADD CONSTRAINT [FK_AIConfigs_Users_SuperAdminId] FOREIGN KEY ([SuperAdminId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [AILogs] ADD CONSTRAINT [FK_AILogs_ExamSessions_ExamSessionId] FOREIGN KEY ([ExamSessionId]) REFERENCES [ExamSessions] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Answers] ADD CONSTRAINT [FK_Answers_ExamSessions_ExamSessionId] FOREIGN KEY ([ExamSessionId]) REFERENCES [ExamSessions] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Answers] ADD CONSTRAINT [FK_Answers_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ChatMessages] ADD CONSTRAINT [FK_ChatMessages_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ChatMessages] ADD CONSTRAINT [FK_ChatMessages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Courses] ADD CONSTRAINT [FK_Courses_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Departments] ADD CONSTRAINT [FK_Departments_Faculties_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculties] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Exams] ADD CONSTRAINT [FK_Exams_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Exams] ADD CONSTRAINT [FK_Exams_Users_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ExamSessions] ADD CONSTRAINT [FK_ExamSessions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ExamSessions] ADD CONSTRAINT [FK_ExamSessions_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Faculties] ADD CONSTRAINT [FK_Faculties_Universities_UniversityId] FOREIGN KEY ([UniversityId]) REFERENCES [Universities] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [MCQOptions] ADD CONSTRAINT [FK_MCQOptions_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [PerformanceReports] ADD CONSTRAINT [FK_PerformanceReports_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ProctorSessions] ADD CONSTRAINT [FK_ProctorSessions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [ProctorSessions] ADD CONSTRAINT [FK_ProctorSessions_Users_ProctorId] FOREIGN KEY ([ProctorId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Questions] ADD CONSTRAINT [FK_Questions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [SupportTickets] ADD CONSTRAINT [FK_SupportTickets_Users_AssignedToSupportId] FOREIGN KEY ([AssignedToSupportId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [SupportTickets] ADD CONSTRAINT [FK_SupportTickets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Departments_Doctor_DepartmentId] FOREIGN KEY ([Doctor_DepartmentId]) REFERENCES [Departments] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Faculties_Admin_FacultyId] FOREIGN KEY ([Admin_FacultyId]) REFERENCES [Faculties] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Faculties_Doctor_FacultyId] FOREIGN KEY ([Doctor_FacultyId]) REFERENCES [Faculties] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Faculties_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculties] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Universities_Admin_UniversityId] FOREIGN KEY ([Admin_UniversityId]) REFERENCES [Universities] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Universities_Doctor_UniversityId] FOREIGN KEY ([Doctor_UniversityId]) REFERENCES [Universities] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Universities_UniversityId] FOREIGN KEY ([UniversityId]) REFERENCES [Universities] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251031024820_init3'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251031024820_init3', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [ExamSessions] ADD [SubmittedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Exams] ADD [CreatedBy] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Exams] ADD [DeletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Exams] ADD [DeletedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Exams] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Exams] ADD [MaxAttempts] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Exams] ADD [UpdatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Exams] ADD [UpdatedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Answers] ADD [AIFeedback] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Answers] ADD [AISimilarityScore] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    CREATE INDEX [IX_Answers_SelectedOptionId] ON [Answers] ([SelectedOptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    ALTER TABLE [Answers] ADD CONSTRAINT [FK_Answers_MCQOptions_SelectedOptionId] FOREIGN KEY ([SelectedOptionId]) REFERENCES [MCQOptions] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101152732_init4'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251101152732_init4', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010123259_MakeTicketResolutionNullable'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SupportTickets]') AND [c].[name] = N'Resolution');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [SupportTickets] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [SupportTickets] ALTER COLUMN [Resolution] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010123259_MakeTicketResolutionNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010123259_MakeTicketResolutionNullable', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013071110_RefreshTokens'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013071110_RefreshTokens'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013071110_RefreshTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013071110_RefreshTokens', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013150446_AddRefreshTokens'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013150446_AddRefreshTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013150446_AddRefreshTokens', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015234037_AddLoginLockout'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015234037_AddLoginLockout', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015235215_AddLoginLockouts'
)
BEGIN
    CREATE TABLE [LoginLockouts] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(200) NOT NULL,
        [Failures] int NOT NULL,
        [LockedUntil] datetime2 NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_LoginLockouts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015235215_AddLoginLockouts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LoginLockouts_Key] ON [LoginLockouts] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015235215_AddLoginLockouts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015235215_AddLoginLockouts', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'UserType');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [Users] ALTER COLUMN [UserType] nvarchar(21) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    ALTER TABLE [Users] ADD [Proctor_FacultyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    ALTER TABLE [SystemLogs] ADD [ActorRole] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    ALTER TABLE [SystemLogs] ADD [FacultyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    ALTER TABLE [SystemLogs] ADD [IsReverted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    ALTER TABLE [SystemLogs] ADD [NewValues] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    ALTER TABLE [SystemLogs] ADD [OldValues] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    ALTER TABLE [SystemLogs] ADD [RevertedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    ALTER TABLE [SystemLogs] ADD [RevertedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([Id], [Name])
    VALUES (7, N''FacultySecretary''),
    (8, N''TechnicalStaff'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    CREATE INDEX [IX_Users_Proctor_FacultyId] ON [Users] ([Proctor_FacultyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    CREATE INDEX [IX_SystemLogs_FacultyId_Timestamp] ON [SystemLogs] ([FacultyId], [Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Faculties_Proctor_FacultyId] FOREIGN KEY ([Proctor_FacultyId]) REFERENCES [Faculties] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031120646_AddFacultyStaffAndAuditTrail'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031120646_AddFacultyStaffAndAuditTrail', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101204725_AddMustChangePassword'
)
BEGIN
    ALTER TABLE [Users] ADD [MustChangePassword] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101204725_AddMustChangePassword'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101204725_AddMustChangePassword', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102125457_AddSessionReviews'
)
BEGIN
    CREATE TABLE [SessionReviews] (
        [Id] int NOT NULL IDENTITY,
        [ExamSessionId] int NOT NULL,
        [Decision] nvarchar(max) NOT NULL,
        [Note] nvarchar(max) NULL,
        [RiskScore] int NOT NULL,
        [ReviewedBy] int NOT NULL,
        [ReviewedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SessionReviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SessionReviews_ExamSessions_ExamSessionId] FOREIGN KEY ([ExamSessionId]) REFERENCES [ExamSessions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102125457_AddSessionReviews'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SessionReviews_ExamSessionId] ON [SessionReviews] ([ExamSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102125457_AddSessionReviews'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102125457_AddSessionReviews', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102232819_AddGradingAndAppeals'
)
BEGIN
    CREATE TABLE [Appeals] (
        [Id] int NOT NULL IDENTITY,
        [ExamSessionId] int NOT NULL,
        [StudentId] int NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Response] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RespondedAt] datetime2 NULL,
        [RespondedBy] int NULL,
        CONSTRAINT [PK_Appeals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Appeals_ExamSessions_ExamSessionId] FOREIGN KEY ([ExamSessionId]) REFERENCES [ExamSessions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102232819_AddGradingAndAppeals'
)
BEGIN
    CREATE TABLE [GradeAdjustments] (
        [Id] int NOT NULL IDENTITY,
        [AnswerId] int NOT NULL,
        [ExamSessionId] int NOT NULL,
        [OldMarks] float NOT NULL,
        [NewMarks] float NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [ChangedBy] int NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_GradeAdjustments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GradeAdjustments_Answers_AnswerId] FOREIGN KEY ([AnswerId]) REFERENCES [Answers] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102232819_AddGradingAndAppeals'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Appeals_ExamSessionId] ON [Appeals] ([ExamSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102232819_AddGradingAndAppeals'
)
BEGIN
    CREATE INDEX [IX_Appeals_StudentId] ON [Appeals] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102232819_AddGradingAndAppeals'
)
BEGIN
    CREATE INDEX [IX_GradeAdjustments_AnswerId] ON [GradeAdjustments] ([AnswerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102232819_AddGradingAndAppeals'
)
BEGIN
    CREATE INDEX [IX_GradeAdjustments_ExamSessionId] ON [GradeAdjustments] ([ExamSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102232819_AddGradingAndAppeals'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102232819_AddGradingAndAppeals', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104045003_AddUserSessions'
)
BEGIN
    CREATE TABLE [UserSessions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Sid] nvarchar(450) NOT NULL,
        [DeviceId] nvarchar(max) NOT NULL,
        [Ip] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastSeenAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104045003_AddUserSessions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserSessions_Sid] ON [UserSessions] ([Sid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104045003_AddUserSessions'
)
BEGIN
    CREATE INDEX [IX_UserSessions_UserId_EndedAt] ON [UserSessions] ([UserId], [EndedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104045003_AddUserSessions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104045003_AddUserSessions', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108105528_AddResumeRequests'
)
BEGIN
    ALTER TABLE [ExamSessions] ADD [EndReason] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108105528_AddResumeRequests'
)
BEGIN
    CREATE TABLE [ResumeRequests] (
        [Id] int NOT NULL IDENTITY,
        [ExamSessionId] int NOT NULL,
        [StudentId] int NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Status] nvarchar(450) NOT NULL,
        [Note] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DecidedAt] datetime2 NULL,
        [DecidedBy] int NULL,
        CONSTRAINT [PK_ResumeRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ResumeRequests_ExamSessions_ExamSessionId] FOREIGN KEY ([ExamSessionId]) REFERENCES [ExamSessions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108105528_AddResumeRequests'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ResumeRequests_ExamSessionId] ON [ResumeRequests] ([ExamSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108105528_AddResumeRequests'
)
BEGIN
    CREATE INDEX [IX_ResumeRequests_Status_CreatedAt] ON [ResumeRequests] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108105528_AddResumeRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108105528_AddResumeRequests', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121145710_AddPasswordResetTokens'
)
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        [RequestedBy] int NOT NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121145710_AddPasswordResetTokens'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121145710_AddPasswordResetTokens'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121145710_AddPasswordResetTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121145710_AddPasswordResetTokens', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121150335_AddUniqueSessionAndAnswerIndexes'
)
BEGIN
    DROP INDEX [IX_ExamSessions_StudentId_ExamId] ON [ExamSessions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121150335_AddUniqueSessionAndAnswerIndexes'
)
BEGIN
    DROP INDEX [IX_Answers_ExamSessionId] ON [Answers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121150335_AddUniqueSessionAndAnswerIndexes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExamSessions_StudentId_ExamId] ON [ExamSessions] ([StudentId], [ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121150335_AddUniqueSessionAndAnswerIndexes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Answers_ExamSessionId_QuestionId] ON [Answers] ([ExamSessionId], [QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121150335_AddUniqueSessionAndAnswerIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121150335_AddUniqueSessionAndAnswerIndexes', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121170146_AddSessionRiskCounters'
)
BEGIN
    ALTER TABLE [ExamSessions] ADD [RiskScore] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121170146_AddSessionRiskCounters'
)
BEGIN
    ALTER TABLE [ExamSessions] ADD [ViolationCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121170146_AddSessionRiskCounters'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121170146_AddSessionRiskCounters', N'9.0.0');
END;

COMMIT;
GO

