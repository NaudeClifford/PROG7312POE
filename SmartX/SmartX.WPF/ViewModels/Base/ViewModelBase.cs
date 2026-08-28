using SmartX.WPF.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartX.WPF.ViewModels.Base;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    protected SmartXSession? Session { get; private set; }


    // CURRENT GATEWAY
    public string CurrentGatewayName =>
        Session?.GatewayName ?? "No gateway selected";

    public bool HasCurrentGateway =>
        Session?.GatewayId.HasValue == true;

    // SESSION INITIALIZATION
    protected void InitializeSession(
        SmartXSession session)
    {
        Session = session
            ?? throw new ArgumentNullException(nameof(session));

        Session.PropertyChanged += Session_PropertyChanged;

        RefreshSessionDisplay();
    }

    // SESSION PROPERTY CHANGED
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
    }


    // CURRENT COMPANY
    public Guid CurrentCompanyId =>
        Session?.SelectedCompanyId ?? Guid.Empty;

    public string CurrentCompanyName =>
        Session?.SelectedCompanyName ?? "No company selected";


    // DISPLAY REFRESH
    protected void RefreshGatewayDisplay()
    {
        OnPropertyChanged(nameof(CurrentGatewayName));
        OnPropertyChanged(nameof(HasCurrentGateway));
    }

    protected void RefreshSessionDisplay()
    {
        OnPropertyChanged(nameof(CurrentGatewayName));
        OnPropertyChanged(nameof(HasCurrentGateway));
        OnPropertyChanged(nameof(CurrentCompanyId));
        OnPropertyChanged(nameof(CurrentCompanyName));
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
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;

        OnPropertyChanged(propertyName);

        return true;
    }
}
