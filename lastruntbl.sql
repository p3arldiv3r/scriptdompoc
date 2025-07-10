CREATE TABLE dbo.LastRun (
    Id INT PRIMARY KEY CHECK (Id = 1), -- only one row, Id=1
    LastRunAt DATETIME NOT NULL
);

-- Insert initial row with very old date so first run fetches all data
INSERT INTO dbo.LastRun (Id, LastRunAt) VALUES (1, '1900-01-01 00:00:00');