using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace RohBot.Views
{
    public sealed partial class ConnectingOverlay : UserControl
    {
        private long _token;

        public ConnectingOverlay()
        {
            InitializeComponent();
        }

        private void ConnectingOverlay_OnLoaded(object sender, RoutedEventArgs e)
        {
            _token = RegisterPropertyChangedCallback(VisibilityProperty, (sender2, dp) =>
            {
                TakeFocusIfVisible();
            });

            TakeFocusIfVisible();
        }

        private void ConnectingOverlay_OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnregisterPropertyChangedCallback(VisibilityProperty, _token);
        }

        private async void TakeFocusIfVisible()
        {
            if (Visibility != Visibility.Visible)
                return;

            FocusStealer.Focus(FocusState.Programmatic);
            await Task.Delay(50);
            FocusStealer.Focus(FocusState.Programmatic);
        }
    }
}
