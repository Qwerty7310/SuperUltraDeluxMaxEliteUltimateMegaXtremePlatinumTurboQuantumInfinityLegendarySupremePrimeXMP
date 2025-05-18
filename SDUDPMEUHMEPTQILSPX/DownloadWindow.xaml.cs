using Microsoft.Win32;
using SDUDPMEUHMEPTQILSPX.Player;
using System.IO;
using System.Windows;
using YoutubeExplode;

namespace SDUDPMEUHMEPTQILSPX {
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadDialog {
        public Track? DownloadedTrack { get; set; }

        public DownloadDialog() {
            InitializeComponent();
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e) {
            var url = txtUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url)) {
                MessageBox.Show("Please enter a URL.");
                return;
            }

            btnDownload.IsEnabled = false;
            txtUrl.IsEnabled = false;
            txtStatus.Text = "Preparing...";
            txtStatus.Visibility = pbProgress.Visibility = Visibility.Visible;

            try {
                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(url);
                var manifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                var stream = manifest
                    .GetAudioOnlyStreams()
                    .OrderByDescending(x => x.Bitrate)
                    .FirstOrDefault();

                if (stream == null)
                    throw new Exception("No audio stream available.");

                var tempInput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{stream.Container.Name}");

                txtStatus.Text = $"Downloading: {video.Title}";
                var progress = new Progress<double>(p => pbProgress.Value = p * 100);
                await youtube.Videos.Streams.DownloadAsync(stream, tempInput, progress);

                txtStatus.Text = "Download complete.";

                var saveDialog = new SaveFileDialog {
                    Title = "Save downloaded track as",
                    FileName = CleanFileName(video.Title),
                    DefaultExt = $".{stream.Container.Name}",
                    Filter = $"Audio File (*.{stream.Container.Name})|*.{stream.Container.Name}|All Files|*.*"
                };

                if (saveDialog.ShowDialog() != true) {
                    File.Delete(tempInput);
                    DialogResult = false;
                    return;
                }

                var outputPath = saveDialog.FileName;
                File.Copy(tempInput, outputPath, true);
                File.Delete(tempInput);

                DownloadedTrack = new Track(outputPath);
                DialogResult = true;
            } catch (Exception ex) {
                MessageBox.Show("Download failed:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            } finally {
                //Close();
            }
        }

        private string CleanFileName(string name) {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
