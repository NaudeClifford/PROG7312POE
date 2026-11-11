using SmartX.WPF.Navigation;
using SmartX.WPF.ViewModels.CommandStream;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.CommandStream
{
    /// <summary>
    /// Interaction logic for RealTimeCommandStreamPage.xaml
    /// </summary>
    public partial class RealTimeCommandStreamPage : Page, INavigationAware
    {
        private readonly RealTimeCommandViewModel _viewModel;
        private readonly INavigationService _navigationService;

        public RealTimeCommandStreamPage(RealTimeCommandViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _navigationService = navigationService;

            DataContext = _viewModel;

            Loaded += RealTimeCommandStreamPage_Loaded;

        }

        private async void RealTimeCommandStreamPage_Loaded(
             object sender, RoutedEventArgs e)
        {
            Loaded -= RealTimeCommandStreamPage_Loaded;

            await _viewModel.LoadAsync();
        }

        public void OnNavigatedTo(object parameter)
        {
            if (parameter is CommandNavigationRequest request &&
                DataContext is RealTimeCommandViewModel viewModel)
            {
                viewModel.ReceiveRecommendation(request);
            }
        }
    }
}
