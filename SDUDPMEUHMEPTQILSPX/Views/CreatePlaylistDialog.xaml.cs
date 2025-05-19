using System.Windows;

namespace SDUDPMEUHMEPTQILSPX {
    /// <summary>
    /// Interaction logic for CreatePlaylistDialog.xaml
    /// </summary>
    public partial class CreatePlaylistDialog : Window {
        public string PlaylistName => txtPlaylistName.Text.Trim();

        public CreatePlaylistDialog() {
            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(PlaylistName)) {
                MessageBox.Show("Please enter a name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }

}
