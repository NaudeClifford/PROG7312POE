using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace SmartX.WPF.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    private Frame? _mainFrame;

    public NavigationService(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    // SET FRAME
    public void SetFrame(Frame frame)
    {
        _mainFrame = frame
            ?? throw new ArgumentNullException(nameof(frame));
    }

    // NAVIGATE WITHOUT PARAMETER
    public void NavigateTo<TPage>()
        where TPage : class
    {
        EnsureFrame();

        var page = _serviceProvider
            .GetRequiredService<TPage>();

        _mainFrame!.Navigate(page);
    }

    // NAVIGATE WITH PARAMETER
    public void NavigateTo<TPage>(object parameter)
        where TPage : class
    {
        EnsureFrame();

        var page = _serviceProvider
            .GetRequiredService<TPage>();

        if (page is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(parameter);
        }

        _mainFrame!.Navigate(page);
    }


    // ENSURE FRAME
    private void EnsureFrame()
    {
        if (_mainFrame == null)
        {
            throw new InvalidOperationException(
                "NavigationService has not been initialized with a Frame.");
        }
    }
}
