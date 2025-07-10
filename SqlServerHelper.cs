using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace deltafm
{
    public static class SqlServerHelper
    {
        private static readonly string connectionString = "Server=USALNLTRENT01;Database=MyTestDb;Integrated Security=True;TrustServerCertificate=True;";

        public static void InsertTracks(List<TrackInfo> tracks)
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            foreach (var track in tracks)
            {
                string query = "INSERT INTO Tracks (TrackName, ArtistName, PlayedAt) VALUES (@track, @artist, @playedAt)";
                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@track", track.TrackName);
                cmd.Parameters.AddWithValue("@artist", track.ArtistName);
                cmd.Parameters.AddWithValue("@playedAt", (object?)track.PlayedAt ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        // executes a SQL query and prints the result to the console.. isn't this counterintuitive when we also write to csv? should the writing occur in here? 
        // refactor to be a list of TrackInfo
        public static List<TrackInfo> ExecuteTrackQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));
            List<TrackInfo> tracklist = new List<TrackInfo>();
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            using SqlCommand cmd = new SqlCommand(sql, conn);
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var track = new TrackInfo
                {
                    TrackName = reader["TrackName"].ToString() ?? string.Empty,
                    ArtistName = reader["ArtistName"].ToString() ?? string.Empty,
                    PlayedAt = reader["PlayedAt"] as DateTime? 
                };
                tracklist.Add(track);
            }
            return tracklist;
        }

        public static int LogNewRun()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            string insertQuery = "INSERT INTO DeltaTracks DEFAULT VALUES; SELECT SCOPE_IDENTITY();";
            using SqlCommand cmd = new SqlCommand(insertQuery, conn);
            int newRunId = Convert.ToInt32(cmd.ExecuteScalar());

            return newRunId;
        }

        public static DateTime GetLastRunTimestamp()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            string query = "SELECT TOP 1 CreatedAt FROM DeltaTracks ORDER BY Id DESC";

            using SqlCommand cmd = new SqlCommand(query, conn);
            var result = cmd.ExecuteScalar();

            if (result != null && DateTime.TryParse(result.ToString(), out DateTime lastRun))
                return lastRun;

            return new DateTime(1900, 1, 1); // fallback
        }

        public static HashSet<(string TrackName, string ArtistName, DateTime PlayedAt)> GetExistingTrackKeys()
        {
            var keys = new HashSet<(string, string, DateTime)>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT TrackName, ArtistName, PlayedAt FROM Tracks", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        keys.Add((
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetDateTime(2)
                        ));
                    }
                }
            }

            return keys;
        }
    }
}