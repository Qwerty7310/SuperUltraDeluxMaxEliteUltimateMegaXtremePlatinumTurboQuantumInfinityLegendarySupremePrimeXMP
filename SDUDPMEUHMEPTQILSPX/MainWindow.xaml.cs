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

    private DBContext _dbContext;

    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;

    private bool _isUserDraggingSlider = false;
    private readonly double _volumeCoef = 0.6;

    public MainWindow() {
        int volume = Properties.Settings.Default.Volume;

        InitializeComponent();
        Core.Initialize();
        DataContext = this;

        dgLibrary.LoadingRow += (s, e) => {
            var row = e.Row;
            var menu = new ContextMenu();

            var addToQueue = new MenuItem { Header = "Add to queue" };
            var removeItem = new MenuItem { Header = "Remove from library" };

            addToQueue.Click += AddToQueue;
            removeItem.Click += RemoveFromLibrary;

            menu.Items.Add(addToQueue);
            menu.Items.Add(removeItem);
            row.ContextMenu = menu;
        };

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
        lbQueue.ItemsSource = Queue;

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

    private void btnAddTrack_Click(object sender, RoutedEventArgs e) {
        OpenFileDialog openFileDialog = new() {
            Filter = "Audio files (*.mp3;*.wav;*.aac;*.m4a;*.wma;*.flac;*.ogg;*.opus;*.webm;*.mid;*.midi)|*.mp3;*.wav;*.aac;*.m4a;*.wma;*.flac;*.ogg;*.opus;*.webm;*.mid;*.midi",
            Title = "Select audio files",
            Multiselect = true
        };
        if (openFileDialog.ShowDialog() == true) {
            foreach (var filePath in openFileDialog.FileNames)
                _dbContext.Tracks.Insert(TrackRecord.ToRecord(new Track(filePath)));
            RefreshAllTracks();
        }
    }

    private void OnMediaEnded(object sender, EventArgs e) {
        Dispatcher.Invoke(() => NextTrack());
    }

    private void OnPositionChanged(object sender, MediaPlayerTimeChangedEventArgs e) {
        Dispatcher.Invoke(() => {
            if (CurrentTrack == null || _isUserDraggingSlider) return;
            if (CurrentTrack.Duration.TotalNanoseconds == 0) {
                MessageBox.Show("Zero duration", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string Format(TimeSpan t) => CurrentTrack.Duration.TotalHours >= 1
                ? t.ToString(@"h\:mm\:ss")
                : t.ToString(@"mm\:ss");

            var currentTime = TimeSpan.FromMilliseconds(e.Time);
            lblPosition.Content = $"{Format(currentTime)}/{CurrentTrack.DurationString}";

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
        } else {
            _mediaPlayer.Play();
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
        _mediaPlayer.Stop();
        btnPlay.IsEnabled = false;
        ResetPosition();
    }

    private void SetTrack() {
        if (_mediaPlayer.IsPlaying)
            _mediaPlayer.Stop();

        using var media = new Media(_libVLC, CurrentTrack.Path, FromType.FromPath);
        _mediaPlayer.Play(media);

        btnPlay.IsEnabled = true;
        btnNext.IsEnabled = true;
    }

    private void NextTrack() {
        int currentIndex = Queue.IndexOf(CurrentTrack);
        if (currentIndex >= 0 && currentIndex < Queue.Count - 1) {
            ResetMediaElement();
            CurrentTrack = Queue[currentIndex + 1];
            SetTrack();
        } else {
            _mediaPlayer.Stop();
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
        if (dgLibrary.SelectedItem is Track selected) {
            Queue.Add(Track.CloneTrack(selected));
            if (CurrentTrack == null) {
                CurrentTrack = Queue[0];
                SetTrack();
            }
        }
    }

    private void RefreshAllTracks() {
        AllTracks.Clear();
        var updatedTracks = _dbContext.GetAllTracksAsUiModels();
        foreach (var track in updatedTracks)
            AllTracks.Add(track);
    }

    private void RemoveFromLibrary(object sender, RoutedEventArgs e) {
        if (sender is MenuItem mi && mi.DataContext is Track track) {
            var record = _dbContext.GetTrackByPath(track.Path);
            if (record != null)
                _dbContext.Tracks.Delete(record.Id);
            AllTracks.Remove(track);
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

    private void btnDownload_Click(object sender, RoutedEventArgs e) {
        var dialog = new DownloadDialog { Owner = this };
        dialog.ShowDialog();
        if (dialog.DialogResult == true && dialog.DownloadedTrack is Track track) {
            _dbContext.Tracks.Insert(TrackRecord.ToRecord(track));
            RefreshAllTracks();
        }
    }
}