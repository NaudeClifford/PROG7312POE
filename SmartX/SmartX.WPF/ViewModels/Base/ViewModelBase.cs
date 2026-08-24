using SmartX.WPF.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartX.WPF.ViewModels.Base;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    protected SmartXSession? Session { get; private set; }

    public string CurrentGatewayName =>
        Session?.GatewayName ?? "No gateway selected";

    public bool HasCurrentGateway =>
        Session?.GatewayId.HasValue == true;

    protected void InitializeSession(
        SmartXSession session)
    {
        Session = session;

        OnPropertyChanged(nameof(CurrentGatewayName));
        OnPropertyChanged(nameof(HasCurrentGateway));
    }

    protected void RefreshGatewayDisplay()
    {
        OnPropertyChanged(nameof(CurrentGatewayName));
        OnPropertyChanged(nameof(HasCurrentGateway));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;

        OnPropertyChanged(propertyName);

        return true;
    }
}