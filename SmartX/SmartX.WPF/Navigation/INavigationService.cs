using System.Windows.Controls;

namespace SmartX.WPF.Navigation
{
    public interface INavigationService
    {
        void SetFrame(Frame frame);

        void NavigateTo<TPage>() where TPage : class;

        void NavigateTo<TPage>(object parameter) where TPage : class;
    }
}