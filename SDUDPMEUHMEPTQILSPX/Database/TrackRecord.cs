using SDUDPMEUHMEPTQILSPX.Player;

namespace SDUDPMEUHMEPTQILSPX.Database {
    public class TrackRecord {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public int? ArtistId { get; set; }
        public int? AlbumId { get; set; }
        public TimeSpan Duration { get; set; }

        public static TrackRecord ToRecord(Track track, int? artistId = null, int? albumId = null) {
            return new TrackRecord {
                Title = track.Title,
                Path = track.Path,
                Duration = track.Duration,
                ArtistId = artistId,
                AlbumId = albumId
            };
        }
    }
}
