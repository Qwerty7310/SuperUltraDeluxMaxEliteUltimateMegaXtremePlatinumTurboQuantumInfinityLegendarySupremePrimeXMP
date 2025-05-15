using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;

namespace SDUDPMEUHMEPTQILSPX;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public string currentFilePath;

    private readonly DispatcherTimer _positionTimer;
    private TimeSpan _lastPosition;
    private bool _isUserDraggingSlider = false;

    public MainWindow() {
        double volume = Properties.Settings.Default.Volume;
        InitializeComponent();

        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _positionTimer.Tick += OnPositionCheck;

        mediaElement.Volume = volume * 0.25;
        intVolume.Value = (int?)Math.Round(volume * 100);
        sliderVolume.Value = volume;
    }

    private void btnSelect_Click(object sender, RoutedEventArgs e) {
        OpenFileDialog openFileDialog = new();
        openFileDialog.Filter = "Music files (*.mp3;*.wma;*.wav;*.aac;*.m4a;*.asf;*.mid;*.midi)|*.mp3;*.wma;*.wav;*.aac;*.m4a;*.asf;*.mid;*.midi";
        if (openFileDialog.ShowDialog() == true) {
            ResetMediaElement();

            currentFilePath = openFileDialog.FileName;
            lblCurrentFile.Content = currentFilePath;
            mediaElement.Source = new Uri(currentFilePath);

            mediaElement.Play();
            mediaElement.Pause();

            btnPlay.IsEnabled = true;
            btnStop.IsEnabled = true;
        } else
            System.Windows.MessageBox.Show("An error occured", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void mediaElement_MediaOpened(object sender, RoutedEventArgs e) {
        _lastPosition = mediaElement.Position;
        //_positionTimer.Start();
        lblPosition.Content = $"00:00/{mediaElement.NaturalDuration.TimeSpan.Minutes:D2}:{mediaElement.NaturalDuration.TimeSpan.Seconds:D2}";
    }

    private void mediaElement_MediaEnded(object sender, RoutedEventArgs e) {
        _positionTimer.Stop();
        mediaElement.Stop();
        btnPlay.IsEnabled = true;
        btnPause.IsEnabled = false;
    }

    private void OnPositionCheck(object sender, EventArgs e) {
        if (_isUserDraggingSlider)
            return;

        var current = mediaElement.Position;
        if (current != _lastPosition) {
            OnPositionChanged(current);
            _lastPosition = current;
        }
    }

    private void OnPositionChanged(TimeSpan newPosition) {
        lblPosition.Content = $"{newPosition.Minutes:D2}:{newPosition.Seconds:D2}/{mediaElement.NaturalDuration.TimeSpan.Minutes:D2}:{mediaElement.NaturalDuration.TimeSpan.Seconds:D2}";
        sliderPosition.Value = newPosition.TotalSeconds / mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
    }

    private void btnPlay_Click(object sender, RoutedEventArgs e) {
        mediaElement.Play();
        _positionTimer.Start();
        btnPause.IsEnabled = true;
        btnPlay.IsEnabled = false;
    }

    private void btnPause_Click(object sender, RoutedEventArgs e) {
        mediaElement.Pause();
        btnPlay.IsEnabled = true;
        btnPause.IsEnabled = false;
    }

    private void btnStop_Click(object sender, RoutedEventArgs e) {
        mediaElement.Stop();
        _positionTimer.Stop();

        ResetPosition();

        btnPlay.IsEnabled = true;
        btnPause.IsEnabled = false;
    }

    private void sliderPosition_PreviewMouseDown(object sender, MouseButtonEventArgs e) { _isUserDraggingSlider = true; }

    private void sliderPosition_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
        _isUserDraggingSlider = false;
        if (mediaElement.NaturalDuration.HasTimeSpan)
            mediaElement.Position = TimeSpan.FromSeconds(
                mediaElement.NaturalDuration.TimeSpan.TotalSeconds * sliderPosition.Value);
    }

    private void ResetPosition() {
        sliderPosition.Value = 0;
        lblPosition.Content = "00:00/00:00";
    }

    private void ResetMediaElement() {
        _positionTimer.Stop();
        mediaElement.Stop();
        btnPlay.IsEnabled = false;
        btnPause.IsEnabled = false;
        btnStop.IsEnabled = false;
        ResetPosition();
    }

    private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        mediaElement.Volume = sliderVolume.Value * 0.25;
        if (intVolume != null)
            intVolume.Value = (int?)Math.Round(sliderVolume.Value * 100);
        Properties.Settings.Default.Volume = sliderVolume.Value;
        Properties.Settings.Default.Save();
    }

    private void intVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        mediaElement.Volume = (intVolume.Value ?? 0) * 0.0025;
        if (sliderVolume != null)
            sliderVolume.Value = (intVolume.Value ?? 0) * 0.01;
        Properties.Settings.Default.Volume = (intVolume.Value ?? 0) * 0.01;
        Properties.Settings.Default.Save();
    }
}