namespace SDUDPMEUHMEPTQILSPX {
    public class Track {
        public string Title { get; }
        public string Path { get; }
        public string? Artist { get; }
        public string? Album { get; }
        public TimeSpan Duration { get; }

        public Track(string path) {
            var file = TagLib.File.Create(path);

            Title = file.Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(path);
            Path = path;
            Artist = file.Tag.FirstAlbumArtist;
            Album = file.Tag.Album;
            Duration = file.Properties.Duration;
        }
    }
}
