using SmartX.WPF.ViewModels.Pages.Users;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Users;

public partial class UsersPage : Page
{
    private readonly UsersViewModel _viewModel;

    public UsersPage(
        UsersViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;

        Loaded += UsersPage_Loaded;
        Unloaded += UsersPage_Unloaded;
    }

    private async void UsersPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private void UsersPage_Unloaded(
        object sender,
        RoutedEventArgs e)
    {
        // Nothing required here currently.
        //
        // The ViewModel remains DI-managed.
    }
}