namespace SmartX.WPF.Navigation
{
    public interface INavigationService
    {
        void NavigateTo<TPage>() where TPage : class;
    }
}