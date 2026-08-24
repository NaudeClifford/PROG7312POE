using SmartX.Application.Commands.Gateway;
using SmartX.Shared.DTOs;
using SmartX.WPF.Services.Api;
using SmartX.WPF.ViewModels.Base;
using System.Net.Http;

namespace SmartX.WPF.ViewModels.Gateway;

public class GatewayEditViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;

    private GatewayDto? _gateway;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private string? _serialNumber;
    private string? _ipAddress;
    private bool _isActive;

    private bool _isBusy;
    private bool _isOnline;

    private string? _statusMessage;
    private string _errorMessage = string.Empty;

    public GatewayEditViewModel(
        ISmartXApiClient apiClient)
    {
        _apiClient = apiClient;

        SaveCommand = new AsyncRelayCommand(
                SaveAsync,
                CanSave);

        DeleteCommand = new AsyncRelayCommand(
                DeleteAsync,
                CanDelete);

        CancelCommand = new AsyncRelayCommand(
                CancelAsync,
                CanCancel);
    }

    // PROPERTIES
    public string Name
    {
        get => _name;
        set
        {
            if (!SetProperty(
                    ref _name, value))
                return;

            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(
            ref _description, value);
    }

    public string? SerialNumber
    {
        get => _serialNumber;
        set => SetProperty(
            ref _serialNumber, value);
    }

    public string? IpAddress
    {
        get => _ipAddress;
        set => SetProperty(
            ref _ipAddress, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(
            ref _isActive, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(
                    ref _isBusy, value))
                return;

            RaiseCommandStates();
        }
    }

    public bool IsOnline
    {
        get => _isOnline;
        private set
        {
            if (!SetProperty(
                    ref _isOnline, value))
                return;

            RaiseCommandStates();
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(
            ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(
            ref _errorMessage, value);
    }

    // COMMANDS
    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand DeleteCommand { get; }

    public AsyncRelayCommand CancelCommand { get; }

    // LOAD
    public async Task LoadAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;

            StatusMessage = null;
            ErrorMessage = string.Empty;

            // CHECK API
            IsOnline = await _apiClient.IsAvailableAsync(
                    cancellationToken);

            if (!IsOnline)
            {
                ErrorMessage = "Unable to connect to the SmartX API.";

                return;
            }

            // LOAD GATEWAY
            var gateway = await _apiClient.GetGatewayByIdAsync(
                    gatewayId,
                    cancellationToken);

            if (gateway is null)
            {
                StatusMessage = "Gateway could not be found.";

                return;
            }

            _gateway = gateway;

            Name = gateway.Name;
            Description = gateway.Description;
            SerialNumber = gateway.SerialNumber;
            IpAddress = gateway.IpAddress;
            IsActive = gateway.IsActive;

            RaiseCommandStates();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

            ErrorMessage = "Unable to connect to the SmartX API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
      
     // SAVE
    private bool CanSave()
    {
        return IsOnline &&
               !IsBusy &&
               _gateway is not null &&
               !string.IsNullOrWhiteSpace(Name);
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
            return;

        try
        {
            IsBusy = true;

            StatusMessage = null;
            ErrorMessage = string.Empty;

            var command = new UpdateGatewayCommand
                {
                    Id = _gateway!.Id,

                    CompanyId = _gateway.CompanyId,

                    Name = Name.Trim(),

                    Description = Description.Trim(),

                    SerialNumber = string.IsNullOrWhiteSpace(
                            SerialNumber)
                            ? null
                            : SerialNumber.Trim(),

                    IpAddress = string.IsNullOrWhiteSpace(
                            IpAddress)
                            ? null
                            : IpAddress.Trim(),

                    IsActive = IsActive
                };


            var success = await _apiClient.UpdateGatewayAsync(
                    command);

            if (!success)
            {
                StatusMessage = "The gateway could not be updated.";

                return;
            }


            StatusMessage = "Gateway updated successfully.";
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

            ErrorMessage = "Unable to connect to the SmartX API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }


     // DELETE
    private bool CanDelete()
    {
        return IsOnline &&
               !IsBusy &&
               _gateway is not null;
    }

    private async Task DeleteAsync()
    {
        if (!CanDelete())
            return;

        try
        {
            IsBusy = true;

            StatusMessage = null;
            ErrorMessage = string.Empty;


            var success = await _apiClient.DeleteGatewayAsync(
                    _gateway!.Id);

            if (!success)
            {
                StatusMessage =
                    "The gateway could not be deleted.";

                return;
            }

            GatewayDeleted?.Invoke(
                this, EventArgs.Empty);
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

            ErrorMessage = "Unable to connect to the SmartX API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

     // CANCEL
    private bool CanCancel()
    {
        return !IsBusy;
    }

    private Task CancelAsync()
    {
        CancelRequested?.Invoke(
            this, EventArgs.Empty);

        return Task.CompletedTask;
    }

    // EVENTS
    public event EventHandler? GatewayDeleted;

    public event EventHandler? CancelRequested;

    // COMMAND STATES
    private void RaiseCommandStates()
    {
        SaveCommand.RaiseCanExecuteChanged();

        DeleteCommand.RaiseCanExecuteChanged();

        CancelCommand.RaiseCanExecuteChanged();
    }
}