using deltafm;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
namespace deltafm
{
    public class LastFmFetcher
    {
        private readonly string apiKey = Environment.GetEnvironmentVariable("LASTFM_API_KEY")
            ?? throw new InvalidOperationException("Environment variable 'LASTFM_API_KEY' is not set.");

        private readonly string username = Environment.GetEnvironmentVariable("LASTFM_USERNAME")
            ?? throw new InvalidOperationException("Environment variable 'LASTFM_USERNAME' is not set.");


        public async Task<List<TrackInfo>> FetchRecentTracksAsync(int limit = 100)
        {
            var trackList = new List<TrackInfo>();
            using var httpClient = new HttpClient();

            string url = $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={username}&api_key={apiKey}&format=json&limit={limit}";
            var response = await httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            var tracks = json["recenttracks"]?["track"];
            if (tracks != null)
            {
                foreach (var track in tracks)
                {
                    var name = track["name"]?.ToString();
                    var artist = track["artist"]?["#text"]?.ToString();

                    DateTime? playedAt = null;
                    var dateToken = track["date"];
                    if (dateToken != null && dateToken["uts"] != null && long.TryParse(dateToken["uts"]?.ToString(), out long uts))
                    {
                        playedAt = DateTimeOffset.FromUnixTimeSeconds(uts).DateTime;
                    }

                    trackList.Add(new TrackInfo
                    {
                        TrackName = name,
                        ArtistName = artist,
                        PlayedAt = playedAt
                    });
                }
            }

            return trackList;
        }
    }
}
