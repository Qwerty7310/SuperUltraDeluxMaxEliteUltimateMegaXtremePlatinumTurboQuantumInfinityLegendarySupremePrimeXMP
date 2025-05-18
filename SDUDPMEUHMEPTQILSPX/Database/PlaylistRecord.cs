namespace SDUDPMEUHMEPTQILSPX.Database {
    class PlaylistRecord {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<int> TrackIds { get; set; } = new();
    }
}
