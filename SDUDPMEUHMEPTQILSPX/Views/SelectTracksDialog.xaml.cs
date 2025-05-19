using SDUDPMEUHMEPTQILSPX.Player;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SDUDPMEUHMEPTQILSPX {
    public partial class SelectTracksDialog : Window {
        public ObservableCollection<Track> AllTracks { get; set; }
        public List<Track> SelectedTracks { get; private set; } = new();

        public SelectTracksDialog(IEnumerable<Track> tracks) {
            InitializeComponent();
            AllTracks = new ObservableCollection<Track>(tracks);
            dgTracks.ItemsSource = AllTracks;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e) {
            foreach (Track selected in dgTracks.SelectedItems)
                SelectedTracks.Add(selected);

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void dgTracks_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var row = UIHelpers.FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row != null && !row.IsSelected) {
                row.IsSelected = true;
                e.Handled = true;
            }
        }
    }

    public static class UIHelpers {
        public static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject {
            while (child != null) {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
