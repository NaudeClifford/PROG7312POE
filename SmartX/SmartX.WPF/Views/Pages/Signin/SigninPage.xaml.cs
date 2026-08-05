using SmartX.WPF.Authentication;
using System.Windows;
using System.Windows.Controls;

namespace SmartX.WPF.Views.Pages.Signin;

public partial class SigninPage : Page
{
    private readonly FirebaseAuthService _firebaseAuthService;

    public SigninPage(FirebaseAuthService firebaseAuthService)
    {
        InitializeComponent();

        _firebaseAuthService = firebaseAuthService;
    }

    private async void SignInButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Clear previous error
        ErrorBorder.Visibility = Visibility.Collapsed;

        string email = EmailTextBox.Text.Trim();
        string password = PasswordBox.Password;

        // Validate email
        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Please enter your email address.");
            EmailTextBox.Focus();
            return;
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter your password.");
            PasswordBox.Focus();
            return;
        }

        try
        {
            // Prevent double-clicking while Firebase is processing
            SignInButton.IsEnabled = false;
            SignInButton.Content = "Signing in...";

            var result = await _firebaseAuthService.SignInAsync(
                email,
                password);

            // Authentication succeeded
            MessageBox.Show(
                $"Welcome {result.Email}!",
                "SmartX",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // IMPORTANT:
            // We will navigate to the authenticated SmartX
            // application from here next.
        }
        catch (Exception)
        {
            ShowError(
                "Unable to sign in. Please check your email and password.");
        }
        finally
        {
            SignInButton.IsEnabled = true;
            SignInButton.Content = "Sign In";
        }
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }
}