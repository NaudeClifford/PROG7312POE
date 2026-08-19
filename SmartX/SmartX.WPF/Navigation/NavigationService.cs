using System;
using Microsoft.Extensions.DependencyInjection;
using SmartX.WPF.Views.Pages;

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
    }
}