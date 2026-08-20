using Microsoft.Extensions.DependencyInjection;

namespace SmartX.WPF.Navigation
{
    public class NavigationService : INavigationService
    {
        private readonly MainWindow _mainWindow;
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(
            MainWindow mainWindow,
            IServiceProvider serviceProvider)
        {
            _mainWindow = mainWindow;
            _serviceProvider = serviceProvider;
        }

        public void NavigateTo<TPage>() where TPage : class
        {
            var page = _serviceProvider.GetRequiredService<TPage>();

            _mainWindow.MainFrame.Navigate(page);
        }

        public void NavigateTo<TPage>(object parameter)
            where TPage : class
        {
            var page = _serviceProvider.GetRequiredService<TPage>();

            if (page is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(parameter);
            }

            _mainWindow.MainFrame.Navigate(page);
        }
    }
}