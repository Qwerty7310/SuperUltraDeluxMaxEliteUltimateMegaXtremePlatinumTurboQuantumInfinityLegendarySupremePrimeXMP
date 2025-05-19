using LibVLCSharp.Shared;
using Microsoft.Win32;
using SDUDPMEUHMEPTQILSPX.Database;
using SDUDPMEUHMEPTQILSPX.Player;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SDUDPMEUHMEPTQILSPX;

public partial class MainWindow : Window, INotifyPropertyChanged {
    private Track? _currentTrack;
    private Playlist? _currentPlaylist;
    public Track? CurrentTrack {
        get => _currentTrack;
        set {
            if (_currentTrack != value) {
                _currentTrack = value;
                OnPropertyChanged(nameof(CurrentTrack));

                if (_currentTrack != null)
                    SetTrack();
            }
        }
    }
    public ObservableCollection<Track> Queue { get; } = new();

    public ObservableCollection<Track> AllTracks { get; set; } = new();

    public ObservableCollection<Track> DisplayedTracks { get; set; }

    public ObservableCollection<Playlist> AllPlaylists { get; set; } = new();

    private DBContext _dbContext;

    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;

    private bool _isUserDraggingSlider = false;
    private readonly double _volumeCoef = 0.6;
    private bool _isRepeatEnabled = false;


    public MainWindow() {
        int volume = Properties.Settings.Default.Volume;

        InitializeComponent();
        Core.Initialize();
        DataContext = this;

        _libVLC = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC);
        videoView.MediaPlayer = _mediaPlayer;

        _mediaPlayer.EndReached += OnMediaEnded;
        _mediaPlayer.TimeChanged += OnPositionChanged;

        var dbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SDUDPMEUHMEPTQILSPX");
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "library.db");
        _dbContext = new DBContext(dbPath);

        RefreshAllTracks();
        DisplayedTracks = new ObservableCollection<Track>(AllTracks);
        RefreshAllPlaylists();

        string lastPath = Properties.Settings.Default.LastTrackPath;
        if (!string.IsNullOrEmpty(lastPath)) {
            var record = _dbContext.GetTrackByPath(lastPath);
            if (record != null) {
                var track = Track.FromRecord(record,
                    record.ArtistId.HasValue ? _dbContext.Artists.FindById(record.ArtistId.Value)?.Name : null,
                    record.AlbumId.HasValue ? _dbContext.Albums.FindById(record.AlbumId.Value)?.Title : null);

                Queue.Add(track);
                CurrentTrack = track;
                SetTrack();
            }
        }

        _mediaPlayer.Volume = volume;
        intVolume.Value = volume;
        sliderVolume.Value = volume;
    }

    protected override void OnClosing(CancelEventArgs e) {
        base.OnClosing(e);

        _mediaPlayer.TimeChanged -= OnPositionChanged;
        _mediaPlayer.EndReached -= OnMediaEnded;

        _mediaPlayer.Dispose();
        _libVLC.Dispose();
        _dbContext.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void AddTrackToLibrary(object sender, RoutedEventArgs e) {
        OpenFileDialog openFileDialog = new() {
            Filter = "Audio files (*.mp3;*.wav;*.aac;*.m4a;*.wma;*.flac;*.ogg;*.opus;*.webm;*.mid;*.midi)|*.mp3;*.wav;*.aac;*.m4a;*.wma;*.flac;*.ogg;*.opus;*.webm;*.mid;*.midi",
            Title = "Select audio files",
            Multiselect = true
        };
        if (openFileDialog.ShowDialog() == true) {
            foreach (var filePath in openFileDialog.FileNames) {
                Track newTrack = new Track(filePath);
                _dbContext.InsertTrack(newTrack);
                if (_currentPlaylist == null)
                    DisplayedTracks.Add(newTrack);
            }
            RefreshAllTracks();
        }
    }

    private void OnMediaEnded(object sender, EventArgs e) {
        Dispatcher.BeginInvoke(() => {
            if (_isRepeatEnabled && CurrentTrack != null) {
                ResetMediaElement();
                SetTrack();
            } else {
                NextTrack();
            }
        });
    }

    private void OnPositionChanged(object sender, MediaPlayerTimeChangedEventArgs e) {
        Dispatcher.Invoke(() => {
            if (CurrentTrack == null || _isUserDraggingSlider) return;

            string Format(TimeSpan t) => CurrentTrack.Duration.TotalHours >= 1
                ? t.ToString(@"h\:mm\:ss")
                : t.ToString(@"mm\:ss");

            var currentTime = TimeSpan.FromMilliseconds(e.Time);
            lblPosition.Content = $"{Format(currentTime)}/{CurrentTrack.DurationString}";

            if (CurrentTrack.Duration != TimeSpan.Zero)
                sliderPosition.Value = currentTime.TotalSeconds / CurrentTrack.Duration.TotalSeconds;

            if (e.Time != CurrentTrack.Duration.TotalMilliseconds)
                btnNext.IsEnabled = true;
            if (e.Time != 0)
                btnPrev.IsEnabled = true;
        });
    }

    private void btnPlay_Click(object sender, RoutedEventArgs e) {
        if (_mediaPlayer.IsPlaying) {
            _mediaPlayer.Pause();
            btnPlay.Content = "Play";
        } else {
            _mediaPlayer.Play();
            btnPlay.Content = "Pause";
        }
    }

    private void sliderPosition_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
        _isUserDraggingSlider = true;
    }

    private void sliderPosition_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
        _isUserDraggingSlider = false;
        if (CurrentTrack != null)
            _mediaPlayer.Time = (long)(CurrentTrack.Duration.TotalMilliseconds * sliderPosition.Value);
    }

    private void ResetPosition() {
        sliderPosition.Value = 0;
        lblPosition.Content = "00:00/00:00";
    }

    private void ResetMediaElement() {
        try {
            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Pause();
            _mediaPlayer.Media?.Dispose();
            _mediaPlayer.Media = null;
            ResetPosition();
        } catch {
            // silent fail to prevent locking
        }
    }

    private void SetTrack() {
        try {
            ResetMediaElement();

            var media = new Media(_libVLC, CurrentTrack.Path, FromType.FromPath);

            _mediaPlayer.Play(media);

            Properties.Settings.Default.LastTrackPath = CurrentTrack?.Path;
            Properties.Settings.Default.Save();

            btnPlay.IsEnabled = true;
            btnNext.IsEnabled = true;
            btnPlay.Content = "Pause";
        } catch (Exception ex) {
            MessageBox.Show($"Failed to play: {ex.Message}", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NextTrack() {
        int currentIndex = Queue.IndexOf(CurrentTrack);
        if (currentIndex >= 0 && currentIndex < Queue.Count - 1) {
            ResetMediaElement();
            CurrentTrack = Queue[currentIndex + 1];
            SetTrack();
        } else {
            _mediaPlayer.Pause();
            _mediaPlayer.Time = 0;
            btnPlay.Content = "Play";
            btnNext.IsEnabled = false;
            lblPosition.Content = $"{CurrentTrack.Duration.Minutes:D2}:{CurrentTrack.Duration.Seconds:D2}/{CurrentTrack.Duration.Minutes:D2}:{CurrentTrack.Duration.Seconds:D2}";
            sliderPosition.Value = 1;
        }
    }

    private void PreviousTrack() {
        int currentIndex = Queue.IndexOf(CurrentTrack);
        if (currentIndex != 0 && _mediaPlayer.Time < 5000) {
            ResetMediaElement();
            CurrentTrack = Queue[currentIndex - 1];
            SetTrack();
            _mediaPlayer.Play();
        } else {
            _mediaPlayer.Time = 0;
        }
    }

    private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        if (_mediaPlayer != null) _mediaPlayer.Volume = (int)sliderVolume.Value;

        if (intVolume != null)
            intVolume.Value = (int)sliderVolume.Value;

        Properties.Settings.Default.Volume = (int)sliderVolume.Value;
        Properties.Settings.Default.Save();
    }

    private void intVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        if (_mediaPlayer != null) _mediaPlayer.Volume = (intVolume.Value ?? 0);

        if (sliderVolume != null)
            sliderVolume.Value = intVolume.Value ?? 0;

        Properties.Settings.Default.Volume = intVolume.Value ?? 0;
        Properties.Settings.Default.Save();
    }

    private void btnNext_Click(object sender, RoutedEventArgs e) {
        NextTrack();
    }

    private void btnPrev_Click(object sender, RoutedEventArgs e) {
        PreviousTrack();
    }

    private void miQueueRemove_Click(object sender, RoutedEventArgs e) {
        if (sender is MenuItem menuItem && menuItem.DataContext is Track track) {
            if (CurrentTrack == track) {
                NextTrack();
            }
            Queue.Remove(track);

            if (Queue.Count == 0) {
                ResetMediaElement();
                CurrentTrack = null;
            }
        }
    }

    private void lbQueue_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
        var listBoxItem = ItemsControl.ContainerFromElement(lbQueue, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (listBoxItem != null)
            e.Handled = true;
    }

    private void dgLibrary_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
        if (dgLibrary.SelectedItem is not Track selected)
            return;

        Queue.Clear();

        int startIndex = DisplayedTracks.IndexOf(selected);
        if (startIndex == -1)
            return;

        for (int i = startIndex; i < DisplayedTracks.Count; i++)
            Queue.Add(Track.CloneTrack(DisplayedTracks[i]));

        CurrentTrack = Queue[0];
        SetTrack();
    }


    private void RefreshAllTracks() {
        AllTracks.Clear();
        var updatedTracks = _dbContext.GetAllTracksAsUiModels();
        foreach (var track in updatedTracks)
            AllTracks.Add(track);
    }

    private void RefreshAllPlaylists() {
        AllPlaylists.Clear();
        var updatedPlaylists = _dbContext.GetAllPlaylists();
        foreach (var playlist in updatedPlaylists)
            AllPlaylists.Add(playlist);
    }

    private void RemoveFromLibrary(object sender, RoutedEventArgs e) {
        if (sender is MenuItem mi && mi.DataContext is Track track) {
            var record = _dbContext.GetTrackByPath(track.Path);
            if (record != null)
                _dbContext.Tracks.Delete(record.Id);
            AllTracks.Remove(track);
            if (_currentPlaylist == null)
                DisplayedTracks.Remove(track);
        }
    }

    private void AddToQueue(object sender, RoutedEventArgs e) {
        if (dgLibrary.SelectedItem is Track selected) {
            Queue.Add(Track.CloneTrack(selected));
            if (CurrentTrack == null) {
                CurrentTrack = Queue[0];
                SetTrack();
            }
        }
    }

    private void DownloadYoutube(object sender, RoutedEventArgs e) {
        var dialog = new DownloadDialog { Owner = this };
        dialog.ShowDialog();
        if (dialog.DialogResult == true && dialog.DownloadedTrack is Track track) {
            _dbContext.InsertTrack(track);
            RefreshAllTracks();
        }
    }

    private void CreatePlaylist(object sender, RoutedEventArgs e) {
        var dialog = new CreatePlaylistDialog { Owner = this };
        if (dialog.ShowDialog() == true) {
            var playlist = _dbContext.GetOrCreatePlaylist(dialog.PlaylistName);
            RefreshAllPlaylists();
        }
    }

    private void DeletePlaylist(object sender, RoutedEventArgs e) {
        if (sender is MenuItem mi && mi.DataContext is Playlist playlist) {
            var record = _dbContext.GetOrCreatePlaylist(playlist.Name);
            if (record != null)
                _dbContext.Playlists.Delete(record.Id);
            AllPlaylists.Remove(playlist);
        }
    }

    private void OpenPlaylist(object sender, MouseButtonEventArgs e) {
        if (lbPlaylists.SelectedItem is not Playlist selected)
            return;

        _currentPlaylist = selected;
        btnAddTracksPlaylist.Visibility = Visibility.Visible;
        DisplayedTracks.Clear();
        foreach (var track in selected.TrackList)
            DisplayedTracks.Add(track);
    }

    private void OpenHome(object sender, RoutedEventArgs e) {
        _currentPlaylist = null;
        btnAddTracksPlaylist.Visibility = Visibility.Collapsed;
        DisplayedTracks.Clear();
        foreach (var track in AllTracks)
            DisplayedTracks.Add(track);
    }

    private void AddTracksToPlaylist(object sender, RoutedEventArgs e) {
        var dialog = new SelectTracksDialog(AllTracks) { Owner = this };
        if (dialog.ShowDialog() == true) {
            var selectedTracks = dialog.SelectedTracks;
            foreach (var track in selectedTracks) {
                DisplayedTracks.Add(track);
                var record = _dbContext.GetTrackByPath(track.Path);
                if (record != null)
                    _dbContext.AddTrackToPlaylist(record.Id, _currentPlaylist.Name);
            }
            RefreshAllPlaylists();
        }
    }

    void RemoveFromPlaylist(object sender, RoutedEventArgs e) {
        if (sender is MenuItem mi && mi.DataContext is Track track) {
            var record = _dbContext.GetTrackByPath(track.Path);
            if (record != null)
                _dbContext.RemoveTrackFromPlaylist(record.Id, _currentPlaylist.Name);
            RefreshAllPlaylists();
            DisplayedTracks.Remove(track);
        }
    }

    private void dgLibrary_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
        if (dgLibrary.SelectedItem is not Track track) {
            e.Handled = true;
            return;
        }

        var row = (DataGridRow)dgLibrary.ItemContainerGenerator.ContainerFromItem(track);
        if (row == null)
            return;

        var menu = new ContextMenu();

        var addToQueue = new MenuItem { Header = "Add to queue" };
        addToQueue.Click += AddToQueue;

        var addToPlaylist = new MenuItem { Header = "Add to playlist" };
        if (AllPlaylists.Count == 0) {
            addToPlaylist.IsEnabled = false;
            addToPlaylist.ToolTip = "No playlists available";
        } else {
            foreach (var playlist in AllPlaylists) {
                var item = new MenuItem { Header = playlist.Name };
                item.Click += (s, args) => {
                    var record = _dbContext.GetTrackByPath(track.Path);
                    if (record != null) {
                        _dbContext.AddTrackToPlaylist(record.Id, playlist.Name);
                        RefreshAllPlaylists();
                        MessageBox.Show($"Added to playlist: {playlist.Name}", "Playlist", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                };
                addToPlaylist.Items.Add(item);
            }
        }

        var removeFromPlaylist = new MenuItem { Header = "Remove from playlist" };
        removeFromPlaylist.Click += RemoveFromPlaylist;

        var removeFromLibrary = new MenuItem { Header = "Remove from library" };
        removeFromLibrary.Click += RemoveFromLibrary;

        menu.Items.Add(addToQueue);
        menu.Items.Add(addToPlaylist);

        if (_currentPlaylist != null)
            menu.Items.Add(removeFromPlaylist);

        menu.Items.Add(removeFromLibrary);

        row.ContextMenu = menu;
    }

    private void btnRepeat_Click(object sender, RoutedEventArgs e) {
        _isRepeatEnabled = !_isRepeatEnabled;
        btnRepeat.Opacity = _isRepeatEnabled ? 1.0 : 0.5;
    }
}