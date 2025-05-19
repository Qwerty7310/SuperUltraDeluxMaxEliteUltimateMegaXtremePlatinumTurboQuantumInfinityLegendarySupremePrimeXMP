using SDUDPMEUHMEPTQILSPX.Database;
using System.IO;
using System.Windows.Media.Imaging;

namespace SDUDPMEUHMEPTQILSPX.Player {
    public class Track {
        public string Title { get; set; }
        public string Path { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public TimeSpan Duration { get; set; }
        public byte[]? CoverImageBytes { get; set; }

        public Track() { }
        public Track(string path) {
            try {
                var file = TagLib.File.Create(path);
                Title = file.Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(path);
                Artist = file.Tag.FirstPerformer;
                Album = file.Tag.Album;
                Duration = file.Properties.Duration;
                var picture = file.Tag.Pictures.FirstOrDefault();
                if (picture != null)
                    CoverImageBytes = picture.Data.Data;
            } catch (TagLib.CorruptFileException) {
                Title = System.IO.Path.GetFileNameWithoutExtension(path);
                Artist = null;
                Album = null;
                Duration = TimeSpan.Zero;
            }

            Path = path;
        }

        public static Track FromRecord(TrackRecord rec, string? artist = null, string? album = null) {
            var track = new Track {
                Title = rec.Title,
                Path = rec.Path,
                Duration = rec.Duration,
                Artist = artist,
                Album = album
            };
            try {
                var file = TagLib.File.Create(rec.Path);
                var picture = file.Tag.Pictures.FirstOrDefault();
                if (picture != null)
                    track.CoverImageBytes = picture.Data.Data;
            } catch {
            }
            return track;
        }

        public string DurationString =>
        Duration.TotalHours >= 1
        ? Duration.ToString(@"h\:mm\:ss")
        : Duration.ToString(@"mm\:ss");

        public static Track CloneTrack(Track track) {
            return new Track { Title = track.Title, Path = track.Path, Artist = track.Artist, Album = track.Album, Duration = track.Duration, CoverImageBytes = track.CoverImageBytes };
        }

        public BitmapImage CoverImage {
            get {
                if (CoverImageBytes != null) {
                    using var ms = new MemoryStream(CoverImageBytes);
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                return new BitmapImage(new Uri("pack://application:,,,/Assets/default_cover.png"));
            }
        }
    }
}