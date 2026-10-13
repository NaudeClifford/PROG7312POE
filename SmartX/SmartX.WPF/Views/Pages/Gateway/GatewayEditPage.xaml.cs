using SmartX.WPF.ViewModels.Gateway;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Gateway
{
    /// <summary>
    /// Interaction logic for GatewayEditPage.xaml
    /// </summary>
    public partial class GatewayEditPage : Page
    {
        public GatewayEditPage(GatewayViewModel viewModel)
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

            if (DataContext is not GatewayViewModel viewModel)
                return;

            if (!viewModel.SaveGatewayCommand.CanExecute(null))
                return;

            viewModel.SaveGatewayCommand.Execute(null);

            e.Handled = true;
        }
    }
}
