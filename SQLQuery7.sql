CREATE TABLE DeltaTracks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TrackName NVARCHAR(255) NOT NULL,
    ArtistName NVARCHAR(255) NOT NULL,
    PlayedAt DATETIME,
    RunID INT,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE INDEX IX_DeltaTracks_PlayedAt ON DeltaTracks(PlayedAt);
CREATE INDEX IX_DeltaTracks_RunID ON DeltaTracks(RunID);