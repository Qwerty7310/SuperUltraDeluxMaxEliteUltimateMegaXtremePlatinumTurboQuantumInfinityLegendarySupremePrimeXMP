namespace SDUDPMEUHMEPTQILSPX.Player {
    public class Album {
        public string Title { get; set; }
        public string? Artist { get; set; }
        public int? Year { get; set; }

        public Album(string title, string? artist = null, int? year = null) {
            Title = title;
            Artist = artist;
            Year = year;
        }
    }
}
