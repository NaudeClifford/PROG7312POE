using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SmartX.WPF.ViewModels.Base;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    // DEPENDENCIES
    protected IConnectivityService ConnectivityService { get; }

    protected SmartXSession Session { get; }

    // STATE
    private bool _isLoaded;
    private bool _isBusy;
    private bool _isOnline;
    private string _errorMessage = string.Empty;

    public bool IsLoaded
    {
        get => _isLoaded;

        protected set => SetProperty(
            ref _isLoaded,
            value);
    }

    public bool IsBusy
    {
        get => _isBusy;

        protected set
        {
            if (!SetProperty(
                    ref _isBusy,
                    value))
            {
                return;
            }

            OnBusyStateChanged();

            RaiseCommandStates();
        }
    }

    public Visibility IsBusyVisibility =>
        IsBusy
            ? Visibility.Visible
            : Visibility.Collapsed;

    public bool IsOnline
    {
        get => _isOnline;

        protected set
        {
            if (!SetProperty(
                    ref _isOnline,
                    value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanModify));

            RaiseConnectivityState();

            _ = OnConnectivityChangedAsync(value);
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;

        protected set => SetProperty(
            ref _errorMessage,
            value);
    }

    public bool CanModify => IsOnline;

    protected ViewModelBase(
        IConnectivityService connectivityService,
        SmartXSession session)
    {
        ConnectivityService =
            connectivityService
            ?? throw new ArgumentNullException(nameof(connectivityService));

        Session =
            session
            ?? throw new ArgumentNullException(nameof(session));

        Session.PropertyChanged += Session_PropertyChanged;

        RefreshSessionDisplay();
    }

    // CONNECTIVITY

    protected async Task<bool> CheckOnlineAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result =
                await ConnectivityService
                    .CheckConnectivityAsync(
                        cancellationToken);

            IsOnline = result;

            return result;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            Console.WriteLine(
                $"Connectivity error: {ex}");

            IsOnline = false;

            return false;
        }
    }


    protected async Task<bool> RequireOnlineAsync(
        CancellationToken cancellationToken = default)
    {
        return await CheckOnlineAsync(
            cancellationToken);
    }

    protected virtual void RaiseConnectivityState()
    {
    }

    protected virtual void OnBusyStateChanged()
    {
    }

    protected virtual Task OnConnectivityChangedAsync(
        bool isOnline)
    {
        return Task.CompletedTask;
    }

    private void Session_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.GatewayId) ||
            e.PropertyName == nameof(SmartXSession.GatewayName))
        {
            RefreshGatewayDisplay();
        }

        if (e.PropertyName == nameof(SmartXSession.SelectedCompanyId) ||
            e.PropertyName == nameof(SmartXSession.SelectedCompanyName))
        {
            OnPropertyChanged(nameof(CurrentCompanyId));
            OnPropertyChanged(nameof(CurrentCompanyName));
        }

        OnSessionPropertyChanged(e);
    }

    protected virtual void OnSessionPropertyChanged(
        PropertyChangedEventArgs e)
    {
    }

    // CURRENT GATEWAY

    public string CurrentGatewayName =>
        Session?.GatewayName ??
        "No gateway selected";

    public bool HasCurrentGateway =>
        Session?.GatewayId.HasValue == true;

    // CURRENT COMPANY

    public Guid CurrentCompanyId =>
        Session?.SelectedCompanyId ?? Guid.Empty;

    public string CurrentCompanyName =>
        Session?.SelectedCompanyName ??
        "No company selected";

    // DISPLAY REFRESH
    protected void RefreshGatewayDisplay()
    {
        OnPropertyChanged(nameof(CurrentGatewayName));
        OnPropertyChanged(nameof(HasCurrentGateway));
    }

    protected void RefreshSessionDisplay()
    {
        RefreshGatewayDisplay();

        OnPropertyChanged(nameof(CurrentCompanyId));
        OnPropertyChanged(nameof(CurrentCompanyName));
    }

    // COMMAND STATES
    protected virtual void RaiseCommandStates()
    {
    }

    // PROPERTY CHANGED
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    // SET PROPERTY
    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(
                field,
                value))
        {
            return false;
        }

        field = value;

        OnPropertyChanged(propertyName);

        return true;
    }

    // DISPOSE

    protected virtual void DisposeSession()
    {
        if (Session is null)
            return;

        Session.PropertyChanged -=
            Session_PropertyChanged;
    }
}
