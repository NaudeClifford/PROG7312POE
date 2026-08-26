using SmartX.WPF.Navigation;
using SmartX.WPF.ViewModels;
using SmartX.WPF.ViewModels.Pages.Sensor;
using System.Windows.Controls;


namespace SmartX.WPF.Views.Pages.Sensor
{
    /// <summary>
    /// Interaction logic for SensorEditPage.xaml
    /// </summary>
    public partial class SensorEditPage : Page, INavigationAware
    {
        public SensorEditPage(SensorViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }

        private void Page_PreviewKeyDown(
    object sender,
    System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter)
                return;

            if (DataContext is not SigninViewModel viewModel)
                return;

            if (!viewModel.SignInCommand.CanExecute(null))
                return;

            viewModel.SignInCommand.Execute(null);

            e.Handled = true;
        }

        public void OnNavigatedTo(object parameter)
        {
            if (DataContext is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(parameter);
            }
        }
    }
}
