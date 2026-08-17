using SmartX.WPF.Authentication;
using SmartX.WPF.ViewModels.Base;

namespace SmartX.WPF.ViewModels.Signin;

public class SigninViewModel : ViewModelBase
{
    private readonly FirebaseAuthService _firebaseAuthService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isBusy;

    public SigninViewModel(
        FirebaseAuthService firebaseAuthService)
    {
        _firebaseAuthService = firebaseAuthService;

        SignInCommand = new AsyncRelayCommand(
            SignInAsync,
            CanSignIn);
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                ClearError();
                SignInCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ClearError();
                SignInCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(
            ref _errorMessage,
            value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(
            ref _hasError,
            value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(SignInButtonText));
                SignInCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SignInButtonText =>
        IsBusy
            ? "Signing in..."
            : "Sign In";

    public AsyncRelayCommand SignInCommand { get; }

    public event EventHandler? SignInSucceeded;

    private bool CanSignIn()
    {
        return !IsBusy &&
               !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(Password);
    }

    private async Task SignInAsync()
    {
        ClearError();

        if (string.IsNullOrWhiteSpace(Email))
        {
            ShowError(
                "Please enter your email address.");

            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowError(
                "Please enter your password.");

            return;
        }

        try
        {
            IsBusy = true;

            var result =
                await _firebaseAuthService.SignInAsync(
                    Email.Trim(),
                    Password);

            SignInSucceeded?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (Exception)
        {
            ShowError(
                "Unable to sign in. Please check your email and password.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}