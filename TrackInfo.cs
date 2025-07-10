using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace deltafm
{
    public class TrackInfo
    {
        public required string TrackName { get; set; }
        public required string ArtistName { get; set; }
        public DateTime? PlayedAt { get; set; }
    }
}
