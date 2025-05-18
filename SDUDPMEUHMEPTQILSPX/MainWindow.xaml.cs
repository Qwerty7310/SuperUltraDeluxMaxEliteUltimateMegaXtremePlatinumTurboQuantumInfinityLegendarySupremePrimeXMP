using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

    private readonly DispatcherTimer _positionTimer;
    private TimeSpan _lastPosition;
    private bool _isUserDraggingSlider = false;
    private bool _isPlaying = false;

    private readonly double _volumeCoef = 0.6;

    public MainWindow() {
        double volume = Properties.Settings.Default.Volume;

        InitializeComponent();
        DataContext = this;

        lbQueue.ItemsSource = Queue;

        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) }; // Helper timer for position slider updates
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
                "Music files (*.mp3;*.wma;*.wav;*.aac;*.m4a;*.asf;*.mid;*.midi)|*.mp3;*.wma;*.wav;*.aac;*.m4a;*.asf;*.mid;*.midi"
        };
        if (openFileDialog.ShowDialog() == true) {
            Queue.Add(new Track(openFileDialog.FileName));
            if (mediaElement.Source == null) {
                CurrentTrack = Queue[0];
                _positionTimer.Start();
                SetTrack();
            }
        } else
            MessageBox.Show("An error occurred", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void mediaElement_MediaOpened(object sender, RoutedEventArgs e) {
        if (CurrentTrack == null) return;
        if (CurrentTrack.Duration - mediaElement.NaturalDuration.TimeSpan > TimeSpan.FromSeconds(1)) {
            MessageBox.Show($"Duration missmatch: {CurrentTrack.Duration}, {mediaElement.NaturalDuration.TimeSpan}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            CurrentTrack.Duration = mediaElement.NaturalDuration.TimeSpan;
        }

        _lastPosition = mediaElement.Position;
        lblPosition.Content =
            $"00:00/{CurrentTrack.Duration.Minutes:D2}:{CurrentTrack.Duration.Seconds:D2}";
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
        lblPosition.Content =
            $"{newPosition.Minutes:D2}:{newPosition.Seconds:D2}/{CurrentTrack.Duration.Minutes:D2}:{CurrentTrack.Duration.Seconds:D2}";
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
        //mediaElement.Pause();

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

    private void miRemove_Click(object sender, RoutedEventArgs e) {
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
}