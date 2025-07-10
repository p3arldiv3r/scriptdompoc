using System;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
namespace deltafm
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // checks when last run was, and fetches new tracks since then (gets timestamp)
            DateTime lastRun = SqlServerHelper.GetLastRunTimestamp();

            // this is the "originalSQL"/base query that we are invoking the methods in SqlModifier on, we use it to fetch tracks from the database
            string reportSql = "Select TrackName, ArtistName, PlayedAt FROM Tracks";
            
            // dynamic injection of the where condition into the reportSQL line 
            // we only get tracks where playedAt > lastRun
            string deltaSql = SqlModifier.AppendWhereCondition(reportSql, lastRun);
            
            Console.WriteLine($"Last run was at: {lastRun}");
            SqlServerHelper.ExecuteTrackQuery(deltaSql);
            var fetcher = new LastFmFetcher();
            var tracks = await fetcher.FetchRecentTracksAsync();
            var existingTrackKeys = SqlServerHelper.GetExistingTrackKeys();
            var newTracks = tracks
                .Where(t => t.PlayedAt.HasValue && !existingTrackKeys.Contains((t.TrackName, t.ArtistName, t.PlayedAt.Value)))
                .DistinctBy(t => new { t.TrackName, t.ArtistName, t.PlayedAt }) 
                .ToList();

            if (newTracks.Count == 0)
            {
                Console.WriteLine("No tracks found to insert.");
                return;
            }


            Console.WriteLine($"Fetched {newTracks.Count} tracks from Last.fm.");
            SqlServerHelper.InsertTracks(newTracks);
            // takes a delta point for the next run
            
            Console.WriteLine("Inserted into database successfully.");
            SqlServerHelper.LogNewRun();
            Console.WriteLine("Enter a song name to find when you last played it:");
            string? input = Console.ReadLine();
            string query = input?.Trim().ToLower() ?? string.Empty;

            var match = tracks
                .Where(t => t.TrackName?.ToLower().Contains(query) == true)
                .OrderByDescending(t => t.PlayedAt)
                .FirstOrDefault();

            // harmless fun interactive field, leave it alone
            // possible improvement: add a fuzzy search or regex to match more closely
            if (match != null && match.PlayedAt.HasValue)
            {
                Console.WriteLine($"\n✅ Last played: '{match.TrackName}' by {match.ArtistName} on {match.PlayedAt.Value}");
            }
            else
            {
                Console.WriteLine("\n❌ No match found or no recorded play time.");
            }
            // end of harmless fun cli

            // write a report to csv
            // resource: https://www.c-sharpcorner.com/blogs/how-to-create-csv-file-using-c-sharp2
            // using nuget package csv helper
            string fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            using (var writer = new StreamWriter($"C:\\Users\\ltrent\\source\\repos\\deltafm\\{fileName}"))
            using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(newTracks);
                Console.WriteLine($"Report written to {fileName}");
            }

        }
    }
}