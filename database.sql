IF NOT EXISTS (SELECT *
               FROM sys.tables
               WHERE name = 'TusFiles')
    BEGIN
        CREATE TABLE TusFiles
        (
            Id             INT IDENTITY (1,1) PRIMARY KEY,
            FileId         NVARCHAR(50),
            FileName       NVARCHAR(50)  NOT NULL,
            UploadLength   BIGINT        NULL,
            UploadOffset   BIGINT        NOT NULL DEFAULT 0,
            Metadata       NVARCHAR(MAX) NULL,
            CreatedAt      DATETIME2     NOT NULL,
            ExpiresAt      DATETIME2     NULL,
            UploadConcat   NVARCHAR(20)  NULL,
            PartialUploads NVARCHAR(MAX) NULL,
            UploadId       NVARCHAR(50)  NULL,
            ZoneId         NVARCHAR(50)  NULL,
            AppId          NVARCHAR(50)  NULL,
            IsCommitted    BIT           NOT NULL DEFAULT 0
        );

        CREATE INDEX IX_TusFiles_ExpiresAt ON TusFiles (ExpiresAt) WHERE ExpiresAt IS NOT NULL;
        CREATE INDEX IX_TusFiles_UploadId ON TusFiles (UploadId, IsCommitted) WHERE UploadId IS NOT NULL;
        CREATE INDEX IX_TusFiles_ZoneId ON TusFiles (ZoneId, IsCommitted) WHERE ZoneId IS NOT NULL;
        CREATE INDEX IX_TusFiles_AppId ON TusFiles (AppId) WHERE AppId IS NOT NULL;
        CREATE INDEX IX_TusFiles_Uncommitted ON TusFiles (CreatedAt) WHERE IsCommitted = 0;

        DBCC CHECKIDENT ('TusFiles', RESEED, 1000); -- Next ID will be 1001
    END