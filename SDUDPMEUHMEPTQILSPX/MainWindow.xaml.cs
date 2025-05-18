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

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
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

    private readonly DispatcherTimer _positionTimer;
    private TimeSpan _lastPosition;
    private bool _isUserDraggingSlider = false;
    private bool _isPlaying = false;

    private readonly double _volumeCoef = 0.6;

    public MainWindow() {
        double volume = Properties.Settings.Default.Volume;

        InitializeComponent();
        DataContext = this;

        var dbDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SDUDPMEUHMEPTQILSPX");
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "library.db");
        _dbContext = new DBContext(dbPath);

        RefreshAllTracks();

        lbQueue.ItemsSource = Queue;

        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _positionTimer.Tick += OnPositionCheck;

        mediaElement.Volume = volume * _volumeCoef;
        intVolume.Value = (int?)Math.Round(volume * 100);
        sliderVolume.Value = volume;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) {
        PropertyChanged!.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void btnAddTrack_Click(object sender, RoutedEventArgs e) {
        OpenFileDialog openFileDialog = new() {
            Filter =
                "Audio files (*.mp3;*.wma;*.wav;*.aac;*.m4a;*.asf;*.mid;*.midi)|*.mp3;*.wma;*.wav;*.aac;*.m4a;*.asf;*.mid;*.midi",
            Title = "Select audio files",
            Multiselect = true
        };
        if (openFileDialog.ShowDialog() == true) {
            foreach (var filePath in openFileDialog.FileNames)
                _dbContext.Tracks.Insert(TrackRecord.ToRecord(new Track(filePath)));
            RefreshAllTracks();
        }
    }

    private void mediaElement_MediaOpened(object sender, RoutedEventArgs e) {
        if (CurrentTrack == null) return;
        if (CurrentTrack.Duration - mediaElement.NaturalDuration.TimeSpan > TimeSpan.FromSeconds(1)) {
            MessageBox.Show($"Duration missmatch: {CurrentTrack.Duration}, {mediaElement.NaturalDuration.TimeSpan}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            CurrentTrack.Duration = mediaElement.NaturalDuration.TimeSpan;
        }

        _lastPosition = mediaElement.Position;
        lblPosition.Content =
            $"00:00/{CurrentTrack.DurationString}";
    }

    private void mediaElement_MediaEnded(object sender, RoutedEventArgs e) {
        NextTrack();
    }

    private void OnPositionCheck(object? sender, EventArgs e) {
        if (_isUserDraggingSlider)
            return;

        var current = mediaElement.Position;
        if (current != _lastPosition) {
            OnPositionChanged(current);
            _lastPosition = current;
        }
    }

    private void OnPositionChanged(TimeSpan newPosition) {
        if (CurrentTrack == null) return;
        if (CurrentTrack.Duration.TotalNanoseconds == 0) {
            MessageBox.Show("Zero duration", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string Format(TimeSpan t) => CurrentTrack.Duration.TotalHours >= 1
            ? t.ToString(@"h\:mm\:ss")
            : t.ToString(@"mm\:ss");
        lblPosition.Content =
            $"{Format(newPosition)}/{CurrentTrack.DurationString}";

        sliderPosition.Value = newPosition.TotalSeconds / CurrentTrack.Duration.TotalSeconds;
        if (newPosition != CurrentTrack.Duration)
            btnNext.IsEnabled = true;
        if (newPosition != TimeSpan.Zero)
            btnPrev.IsEnabled = true;
    }

    private void btnPlay_Click(object sender, RoutedEventArgs e) {
        if (_isPlaying) {
            mediaElement.Pause();
            _isPlaying = false;
        } else {
            mediaElement.Play();
            _positionTimer.Start();
            _isPlaying = true;
        }
    }

    private void sliderPosition_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
        _isUserDraggingSlider = true;
    }

    private void sliderPosition_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
        _isUserDraggingSlider = false;
        if (CurrentTrack != null)
            mediaElement.Position = TimeSpan.FromSeconds(
                CurrentTrack.Duration.TotalSeconds * sliderPosition.Value);
    }

    private void ResetPosition() {
        sliderPosition.Value = 0;
        lblPosition.Content = "00:00/00:00";
    }

    private void ResetMediaElement() {
        _positionTimer.Stop();
        mediaElement.Stop();
        btnPlay.IsEnabled = false;
        ResetPosition();
    }

    private void SetTrack() {
        mediaElement.Source = new Uri(CurrentTrack.Path);
        mediaElement.Play();

        btnPlay.IsEnabled = true;
        btnNext.IsEnabled = true;
    }

    private void NextTrack() {
        int currentIndex = Queue.IndexOf(CurrentTrack);
        if (currentIndex >= 0 && currentIndex < Queue.Count - 1) {
            ResetMediaElement();
            CurrentTrack = Queue[currentIndex + 1];
            SetTrack();
            _isPlaying = true;
            _positionTimer.Start();
            mediaElement.Play();
        } else {
            _positionTimer.Stop();
            mediaElement.Stop();
            _isPlaying = false;

            btnNext.IsEnabled = false;
            lblPosition.Content = $"{CurrentTrack.Duration.Minutes:D2}:{CurrentTrack.Duration.Seconds:D2}/{CurrentTrack.Duration.Minutes:D2}:{CurrentTrack.Duration.Seconds:D2}";
            sliderPosition.Value = 1;
        }
    }

    private void PreviousTrack() {
        int currentIndex = Queue.IndexOf(CurrentTrack);
        if (currentIndex != 0 && _lastPosition.Seconds < 5) {
            ResetMediaElement();
            CurrentTrack = Queue[currentIndex - 1];
            SetTrack();
            _isPlaying = true;
            _positionTimer.Start();
            mediaElement.Play();
        } else {
            mediaElement.Position = TimeSpan.Zero;
        }
    }

    private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        mediaElement.Volume = sliderVolume.Value * _volumeCoef;

        if (intVolume != null)
            intVolume.Value = (int?)Math.Round(sliderVolume.Value * 100);

        Properties.Settings.Default.Volume = sliderVolume.Value;
        Properties.Settings.Default.Save();
    }

    private void intVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        mediaElement.Volume = (intVolume.Value ?? 0) * 0.01 * _volumeCoef;

        if (sliderVolume != null)
            sliderVolume.Value = (intVolume.Value ?? 0) * 0.01;

        Properties.Settings.Default.Volume = (intVolume.Value ?? 0) * 0.01;
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
            if (mediaElement.Source == null) {
                CurrentTrack = Queue[0];
                _positionTimer.Start();
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

    private void miLibraryRemove_Click(object sender, RoutedEventArgs e) {
        if (sender is MenuItem mi && mi.DataContext is Track track) {
            ;
            var record = _dbContext.GetTrackByPath(track.Path);
            if (record != null)
                _dbContext.Tracks.Delete(record.Id);
            AllTracks.Remove(track);
        }
    }
}