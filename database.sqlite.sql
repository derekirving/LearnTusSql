CREATE TABLE IF NOT EXISTS TusFiles
(
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    FileId         TEXT,
    FileName       TEXT    NOT NULL,
    UploadLength   INTEGER,
    UploadOffset   INTEGER NOT NULL DEFAULT 0,
    Metadata       TEXT,
    CreatedAt      TEXT    NOT NULL,
    ExpiresAt      TEXT,
    UploadConcat   TEXT,
    PartialUploads TEXT,
    UploadId       TEXT,
    ZoneId         TEXT,
    AppId          TEXT,
    IsCommitted    INTEGER NOT NULL DEFAULT 0
);

INSERT OR REPLACE INTO sqlite_sequence (name, seq)
VALUES ('TusFiles', 1000);

CREATE INDEX IF NOT EXISTS IX_TusFiles_ExpiresAt ON TusFiles (ExpiresAt);
CREATE INDEX IF NOT EXISTS IX_TusFiles_UploadId ON TusFiles (UploadId, IsCommitted);
CREATE INDEX IF NOT EXISTS IX_TusFiles_ZoneId ON TusFiles (ZoneId, IsCommitted);
CREATE INDEX IF NOT EXISTS IX_TusFiles_AppId ON TusFiles (AppId);
CREATE INDEX IF NOT EXISTS IX_TusFiles_Uncommitted ON TusFiles (CreatedAt);