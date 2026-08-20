namespace SmartX.WPF.Navigation
{
    public interface INavigationService
    {
        void NavigateTo<TPage>() where TPage : class;

        void NavigateTo<TPage>(object parameter) where TPage : class;
    }
}