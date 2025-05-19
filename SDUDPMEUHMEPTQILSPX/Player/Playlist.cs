namespace SDUDPMEUHMEPTQILSPX.Player {
    public class Playlist {
        public string Name { get; set; }
        public List<Track> TrackList { get; set; } = new();

        public Playlist(string name) {
            Name = name;
        }

        public Playlist() {}
        public Playlist(string name, List<Track> tracks) {
            Name = name;
            TrackList = tracks;
        }
    }
}
