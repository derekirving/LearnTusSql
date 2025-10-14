CREATE TABLE TusFiles (
    FileId NVARCHAR(50) PRIMARY KEY,
    UploadLength BIGINT NULL,
    UploadOffset BIGINT NOT NULL DEFAULT 0,
    Metadata NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NULL,
    UploadConcat NVARCHAR(20) NULL,
    PartialUploads NVARCHAR(MAX) NULL,
    SessionId NVARCHAR(50) NULL,
    ZoneId NVARCHAR(50) NULL,
    AppId NVARCHAR(50) NULL,
    IsCommitted BIT NOT NULL DEFAULT 0
);

CREATE INDEX IX_TusFiles_ExpiresAt ON TusFiles(ExpiresAt) WHERE ExpiresAt IS NOT NULL;
CREATE INDEX IX_TusFiles_SessionId ON TusFiles(SessionId, IsCommitted) WHERE SessionId IS NOT NULL;
CREATE INDEX IX_TusFiles_ZoneId ON TusFiles(ZoneId, IsCommitted) WHERE ZoneId IS NOT NULL;
CREATE INDEX IX_TusFiles_AppId ON TusFiles(AppId) WHERE AppId IS NOT NULL;
CREATE INDEX IX_TusFiles_Uncommitted ON TusFiles(CreatedAt) WHERE IsCommitted = 0;