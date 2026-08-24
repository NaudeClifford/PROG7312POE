using SmartX.WPF.ViewModels;
using SmartX.WPF.ViewModels.Pages.Sensor;
using System;
using System.Collections.Generic;
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

namespace SmartX.WPF.Views.Pages.Sensor
{
    /// <summary>
    /// Interaction logic for SensorEditPage.xaml
    /// </summary>
    public partial class SensorEditPage : Page
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
    }
}
