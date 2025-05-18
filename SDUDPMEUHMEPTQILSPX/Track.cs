using System.Collections.Specialized;

namespace SDUDPMEUHMEPTQILSPX {
    public class Track {
        public string Title { get; }
        public string Path { get; }
        public string? Artist { get; }
        public string? Album { get; }
        public TimeSpan Duration { get; set; }

        public Track(string path) {
            try {
                var file = TagLib.File.Create(path);
                Title = file.Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(path);
                Artist = file.Tag.FirstAlbumArtist;
                Album = file.Tag.Album;
                Duration = file.Properties.Duration;
            }
            catch (TagLib.CorruptFileException) {
                Title = System.IO.Path.GetFileNameWithoutExtension(path);
                Artist = null;
                Album = null;
                Duration = TimeSpan.Zero;
            }

            Path = path;
        }
    }
}