using SmartX.WPF.Views.Pages;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.Signin;
using System.Windows;
using System.Windows.Input;

namespace SmartX.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HomePage _homePage;

        public MainWindow(HomePage homePage)
        {
            InitializeComponent();

            _homePage = homePage;

            MainFrame.Navigate(_homePage);
        }

        /*private void ApplyRolePermissions()
        {
            if (_session.Role == "Admin")
            {
                AdminMenu.Visibility = Visibility.Visible;
                ReportsMenu.Visibility = Visibility.Visible;
            }
            else if (_session.Role == "Manager")
            {
                AdminMenu.Visibility = Visibility.Collapsed;
                ReportsMenu.Visibility = Visibility.Visible;
            }
            else
            {
                AdminMenu.Visibility = Visibility.Collapsed;
                ReportsMenu.Visibility = Visibility.Collapsed;
            }
        }*/

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState =
                WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}