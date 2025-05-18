using SDUDPMEUHMEPTQILSPX.Database;

namespace SDUDPMEUHMEPTQILSPX.Player {
    public class Track {
        public string Title { get; set; }
        public string Path { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public TimeSpan Duration { get; set; }

        public Track() { }
        public Track(string path) {
            try {
                var file = TagLib.File.Create(path);
                Title = file.Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(path);
                Artist = file.Tag.FirstAlbumArtist;
                Album = file.Tag.Album;
                Duration = file.Properties.Duration;
            } catch (TagLib.CorruptFileException) {
                Title = System.IO.Path.GetFileNameWithoutExtension(path);
                Artist = null;
                Album = null;
                Duration = TimeSpan.Zero;
            }

            Path = path;
        }

        public static Track FromRecord(TrackRecord rec, string? artist = null, string? album = null) {
            return new Track { Title = rec.Title, Path = rec.Path, Duration = rec.Duration, Artist = artist, Album = album };
        }

        public string DurationString =>
        Duration.TotalHours >= 1
        ? Duration.ToString(@"h\:mm\:ss")
        : Duration.ToString(@"mm\:ss");

        public static Track CloneTrack(Track track) {
            return new Track { Title = track.Title, Path = track.Path, Artist = track.Artist, Album = track.Album, Duration = track.Duration };
        }
    }
}