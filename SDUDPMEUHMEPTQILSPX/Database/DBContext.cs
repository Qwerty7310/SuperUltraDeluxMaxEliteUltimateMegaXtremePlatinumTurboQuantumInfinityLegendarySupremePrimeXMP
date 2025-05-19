using LiteDB;
using SDUDPMEUHMEPTQILSPX.Player;

namespace SDUDPMEUHMEPTQILSPX.Database {
    class DBContext {
        private readonly LiteDatabase _db;

        public ILiteCollection<TrackRecord> Tracks => _db.GetCollection<TrackRecord>("tracks");

        public ILiteCollection<ArtistRecord> Artists => _db.GetCollection<ArtistRecord>("artists");

        public ILiteCollection<AlbumRecord> Albums => _db.GetCollection<AlbumRecord>("albums");

        public ILiteCollection<PlaylistRecord> Playlists => _db.GetCollection<PlaylistRecord>("playlists");

        public DBContext(string dbPath) {
            _db = new LiteDatabase(dbPath);
            EnsureIndexes();
        }

        private void EnsureIndexes() {
            Tracks.EnsureIndex(x => x.Path, unique: true);
            Artists.EnsureIndex(x => x.Name, unique: true);
            Albums.EnsureIndex(x => x.Title);
            Playlists.EnsureIndex(x => x.Name, unique: true);
        }

        public void InsertTrack(Track track) {
            if (Tracks.Exists(t => t.Path == track.Path))
                return;

            var artist = !string.IsNullOrWhiteSpace(track.Artist)
                ? GetOrCreateArtist(track.Artist)
                : null;

            var album = !string.IsNullOrWhiteSpace(track.Album)
                ? GetOrCreateAlbum(track.Album, artist?.Id)
                : null;

            var record = TrackRecord.ToRecord(track, artist != null ? artist.Id : null, album != null ? album.Id : null);

            Tracks.Insert(record);
        }

        public TrackRecord? GetTrackByPath(string path) => Tracks.FindOne(x => x.Path == path);

        public ArtistRecord GetOrCreateArtist(string name) {
            var artist = Artists.FindOne(a => a.Name == name);
            if (artist == null) {
                artist = new ArtistRecord { Name = name };
                Artists.Insert(artist);
            }
            return artist;
        }

        public AlbumRecord GetOrCreateAlbum(string title, int? artistId = null, int? year = null) {
            var album = Albums.FindOne(a => a.Title == title && a.ArtistId == artistId);
            if (album == null) {
                album = new AlbumRecord { Title = title, ArtistId = artistId };
                Albums.Insert(album);
            }
            return album;
        }

        public PlaylistRecord GetOrCreatePlaylist(string name) {
            var playlist = Playlists.FindOne(p => p.Name == name);
            if (playlist == null) {
                playlist = new PlaylistRecord { Name = name };
                Playlists.Insert(playlist);
            }
            return playlist;
        }

        public void AddTrackToPlaylist(int trackId, string name) {
            var playlist = Playlists.FindOne(p => p.Name == name);
            if (playlist != null && !playlist.TrackIds.Contains(trackId)) {
                playlist.TrackIds.Add(trackId);
                Playlists.Update(playlist);
            }
        }

        public void RemoveTrackFromPlaylist(int trackId, string name) {
            var playlist = Playlists.FindOne(p => p.Name == name);
            if (playlist != null && playlist.TrackIds.Contains(trackId)) {
                playlist.TrackIds.Remove(trackId);
                Playlists.Update(playlist);
            }
        }

        public List<TrackRecord> GetTracksInPlaylist(int playlistId) {
            var playlist = Playlists.FindById(playlistId);
            if (playlist == null)
                return new List<TrackRecord>();
            return Tracks.Find(t => playlist.TrackIds.Contains(t.Id)).ToList();
        }

        public List<Track> GetAllTracksAsUiModels() {
            var artistsDict = Artists.FindAll().ToDictionary(a => a.Id, a => a.Name);
            var albumsDict = Albums.FindAll().ToDictionary(a => a.Id, a => a.Title);

            return [.. Tracks.FindAll().Select(r => Track.FromRecord(r, (r.ArtistId.HasValue && artistsDict.TryGetValue(r.ArtistId.Value, out var artist) ? artist : null), (r.AlbumId.HasValue && albumsDict.TryGetValue(r.AlbumId.Value, out var album) ? album : null)))];
        }

        public List<Playlist> GetAllPlaylists() {
            var tracksDict = Tracks.FindAll().ToDictionary(t => t.Id, t => t);

            return Playlists.FindAll().Select(p => new Playlist {
                Name = p.Name,
                TrackList = p.TrackIds
                    .Where(id => tracksDict.ContainsKey(id))
                    .Select(id => {
                        var t = tracksDict[id];
                        return Track.FromRecord(t,
                            t.ArtistId.HasValue && Artists.FindById(t.ArtistId.Value) is { } a ? a.Name : null,
                            t.AlbumId.HasValue && Albums.FindById(t.AlbumId.Value) is { } al ? al.Title : null);
                    }).ToList()
            }).ToList();
        }

        public void Dispose() => _db.Dispose();
    }
}
