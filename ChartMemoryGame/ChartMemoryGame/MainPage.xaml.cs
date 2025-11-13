using Syncfusion.Maui.Charts;

namespace ChartMemoryGame
{
    public partial class MainPage : ContentPage
    {
        #region Constructor

        public MainPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        #region Override Methods

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.EnsureAudioAsync();
        }

        #endregion

        #region Event Handlers
     
        private void OnPointSelectionChanging(object? sender, ChartSelectionChangingEventArgs e)
        {
            if (viewModel.IsAnimating || !viewModel.IsAcceptingInput)
            {
                e.Cancel = true;
            }
        }

        private async void OnPointSelectionChanged(object? sender, ChartSelectionChangedEventArgs e)
        {
            if (viewModel.IsAnimating || !viewModel.IsAcceptingInput) return;
            int tappedIndex = (e.NewIndexes != null && e.NewIndexes.Count > 0) ? e.NewIndexes[0] : -1;
            if (tappedIndex < 0) return;

            // Let the VM validate and also trigger a quick tap flash
            await viewModel.OnTappedAsync(tappedIndex, i => viewModel.HighlightBarAsync(i, new SolidColorBrush(Color.FromArgb("#76ffad")), 150, 0));

            // Clear selection so next tap is obvious
            PointSelection.ClearSelection();
        }

        #endregion

        #endregion
    }
}