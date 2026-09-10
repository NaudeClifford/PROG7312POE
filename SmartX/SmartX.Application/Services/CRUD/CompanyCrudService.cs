using FirebaseAdmin.Auth;
using FluentValidation;
using SmartX.Application.Requests.Company;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Mapping;
using SmartX.Shared.Models;
using System.Security.Claims;

namespace SmartX.Application.Services.CRUD;

public class CompanyCrudService :
    ICrudService<
        CompanyDto,
        CreateCompanyRequest,
        UpdateCompanyRequest>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly ISensorRepository _sensorRepository;
    private readonly ISensorLogFileRepository _sensorLogFileRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ICompanyConfigurationRepository _configurationRepository;
    private readonly IFirebaseUserService _firebaseUserService;

    private readonly IValidator<CreateCompanyRequest> _createValidator;
    private readonly IValidator<UpdateCompanyRequest> _updateValidator;

    private readonly IMapper _mapper;
    private readonly AuditLogService _auditLog;

    public CompanyCrudService(
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        IGatewayRepository gatewayRepository,
        ISensorRepository sensorRepository,
        ISensorLogFileRepository sensorLogFileRepository,
        ITelemetryRepository telemetryRepository,
        IValidator<CreateCompanyRequest> createValidator,
        IValidator<UpdateCompanyRequest> updateValidator,
        ICompanyConfigurationRepository configurationRepository,
        IFirebaseUserService firebaseUserService,
        IMapper mapper,
        AuditLogService auditLog)
    {
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _gatewayRepository = gatewayRepository;
        _sensorRepository = sensorRepository;
        _sensorLogFileRepository = sensorLogFileRepository;
        _telemetryRepository = telemetryRepository;
        _configurationRepository = configurationRepository;
        _firebaseUserService = firebaseUserService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _mapper = mapper;
        _auditLog = auditLog;
    }

    public async Task<Result<IReadOnlyList<CompanyDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var companies = await _companyRepository.GetAllAsync(
            cancellationToken);

        var dtos = _mapper.Map<List<CompanyDto>>(companies);

        return Result<IReadOnlyList<CompanyDto>>.Ok(dtos);
    }

    public async Task<Result<CompanyDto>> GetByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<CompanyDto>.Fail(
                "Company ID is required.");
        }

        if (!CanAccessCompany(user, id))
        {
            return Result<CompanyDto>.Fail(
                "You do not have access to this company.");
        }

        var company = await _companyRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (company is null)
        {
            return Result<CompanyDto>.Fail(
                "Company not found.");
        }

        var dto = _mapper.Map<CompanyDto>(company);

        return Result<CompanyDto>.Ok(dto);
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateCompanyRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<Guid>.Fail(
                "Request is required.");
        }

        var validationResult = await _createValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors.Select(x => x.ErrorMessage));

            return Result<Guid>.Fail(errors);
        }

        var now = DateTime.UtcNow;

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            DeletionRequested = false
        };

        await _companyRepository.AddAsync(
            company,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: company.Id,
            action: "Created",
            companyId: company.Id,
            details: "Company created.",
            cancellationToken: cancellationToken);

        return Result<Guid>.Ok(company.Id);
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateCompanyRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<bool>.Fail(
                "Request is required.");
        }

        if (request.Id == Guid.Empty)
        {
            return Result<bool>.Fail(
                "Company ID is required.");
        }

        if (!CanAccessCompany(user, request.Id))
        {
            return Result<bool>.Fail(
                "You do not have access to this company.");
        }

        var validationResult = await _updateValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors.Select(x => x.ErrorMessage));

            return Result<bool>.Fail(errors);
        }

        var company = await _companyRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail(
                "Company not found.");
        }

        company.Name = request.Name.Trim();
        company.Description =
            request.Description?.Trim() ?? string.Empty;
        company.IsActive = request.IsActive;
        company.UpdatedAt = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(
            company,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: company.Id,
            action: "Updated",
            companyId: company.Id,
            details: "Company updated.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<bool>.Fail(
                "Company ID is required.");
        }

        if (!IsSuperAdmin(user))
        {
            return Result<bool>.Fail(
                "Only SuperAdmin can delete a company.");
        }

        var company = await _companyRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail(
                "Company not found.");
        }

        return await DeleteCompanyDataAsync(
            id,
            "Company and all associated data deleted.",
            cancellationToken);
    }

    public async Task<Result<bool>> DeleteGuestAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<bool>.Fail(
                "Company ID is required.");
        }

        var company = await _companyRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail(
                "Company not found.");
        }

        return await DeleteCompanyDataAsync(
            id,
            "Guest company and all associated data deleted.",
            cancellationToken);
    }

    private async Task<Result<bool>> DeleteCompanyDataAsync(
        Guid companyId,
        string auditDetails,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var users = await _userRepository.GetByCompanyIdAsync(
                companyId,
                cancellationToken);

            foreach (var userRecord in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(
                    userRecord.FirebaseUid))
                {
                    try
                    {
                        await _firebaseUserService.DeleteUserAsync(
                            userRecord.FirebaseUid);
                    }
                    catch (FirebaseAuthException ex)
                        when (ex.AuthErrorCode ==
                               AuthErrorCode.UserNotFound)
                    {
                    }
                }

                await _userRepository.DeleteAsync(
                    userRecord.Id,
                    cancellationToken);
            }

            var gateways = await _gatewayRepository.GetByCompanyIdAsync(
                companyId,
                cancellationToken);

            var gatewayIds = gateways
                .Select(g => g.Id)
                .ToHashSet();

            var allSensors = await _sensorRepository.GetAllAsync(
                cancellationToken);

            var companySensors = allSensors
                .Where(sensor =>
                    sensor.GatewayId.HasValue &&
                    gatewayIds.Contains(sensor.GatewayId.Value))
                .ToList();

            foreach (var sensor in companySensors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var logFiles =
                    await _sensorLogFileRepository.GetBySensorIdAsync(
                        sensor.Id,
                        cancellationToken);

                foreach (var logFile in logFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await _sensorLogFileRepository.DeleteAsync(
                        logFile.Id,
                        cancellationToken);
                }

                var telemetry =
                    await _telemetryRepository.GetBySensorIdAsync(
                        sensor.Id,
                        cancellationToken);

                foreach (var telemetryRecord in telemetry)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await _telemetryRepository.DeleteAsync(
                        telemetryRecord.Id,
                        cancellationToken);
                }

                await _sensorRepository.DeleteAsync(
                    sensor.Id,
                    cancellationToken);
            }

            foreach (var gateway in gateways)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _gatewayRepository.DeleteAsync(
                    gateway.Id,
                    cancellationToken);
            }

            var configuration =
                await _configurationRepository.GetByCompanyIdAsync(
                    companyId,
                    cancellationToken);

            if (configuration is not null)
            {
                await _configurationRepository.DeleteAsync(
                    companyId,
                    cancellationToken);
            }

            await _companyRepository.DeleteAsync(
                companyId,
                cancellationToken);

            await _auditLog.LogAsync(
                entityType: "Company",
                entityId: companyId,
                action: "Deleted",
                companyId: companyId,
                details: auditDetails,
                cancellationToken: cancellationToken);

            return Result<bool>.Ok(true);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(
                $"Unable to delete company: {ex.Message}");
        }
    }

    public async Task<Result<CompanyConfigurationDto>>
        GetConfigurationAsync(
            Guid companyId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            return Result<CompanyConfigurationDto>.Fail(
                "Company ID is required.");
        }

        if (!CanAccessCompany(user, companyId))
        {
            return Result<CompanyConfigurationDto>.Fail(
                "You do not have access to this company.");
        }

        var company = await _companyRepository.GetByIdAsync(
            companyId,
            cancellationToken);

        if (company is null)
        {
            return Result<CompanyConfigurationDto>.Fail(
                "Company not found.");
        }

        var configuration =
            await _configurationRepository.GetByCompanyIdAsync(
                companyId,
                cancellationToken);

        configuration ??= new CompanyConfiguration
        {
            CompanyId = companyId,
            UseCustomApi = false,
            UseCustomFirebase = false,
            ApiBaseUrl = string.Empty,
            FirebaseProjectId = string.Empty,
            FirebaseApiKey = string.Empty,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<CompanyConfigurationDto>(
            configuration);

        return Result<CompanyConfigurationDto>.Ok(dto);
    }

    public async Task<Result<bool>> UpdateConfigurationAsync(
        UpdateCompanyConfigurationRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<bool>.Fail(
                "Request is required.");
        }

        if (request.CompanyId == Guid.Empty)
        {
            return Result<bool>.Fail(
                "Company ID is required.");
        }

        if (!CanAccessCompany(user, request.CompanyId))
        {
            return Result<bool>.Fail(
                "You do not have access to this company.");
        }

        var company = await _companyRepository.GetByIdAsync(
            request.CompanyId,
            cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail(
                "Company not found.");
        }

        if (request.UseCustomApi &&
            string.IsNullOrWhiteSpace(request.ApiBaseUrl))
        {
            return Result<bool>.Fail(
                "API URL is required when using a custom API.");
        }

        if (request.UseCustomFirebase &&
            string.IsNullOrWhiteSpace(request.FirebaseProjectId))
        {
            return Result<bool>.Fail(
                "Firebase Project ID is required when using custom Firebase.");
        }

        var configuration =
            await _configurationRepository.GetByCompanyIdAsync(
                request.CompanyId,
                cancellationToken);

        var isNew = configuration is null;

        configuration ??= new CompanyConfiguration
        {
            CompanyId = request.CompanyId
        };

        configuration.UseCustomApi =
            request.UseCustomApi;

        configuration.ApiBaseUrl =
            request.UseCustomApi
                ? request.ApiBaseUrl.Trim()
                : string.Empty;

        configuration.UseCustomFirebase =
            request.UseCustomFirebase;

        configuration.FirebaseProjectId =
            request.UseCustomFirebase
                ? request.FirebaseProjectId.Trim()
                : string.Empty;

        configuration.FirebaseApiKey =
            request.UseCustomFirebase
                ? request.FirebaseApiKey.Trim()
                : string.Empty;

        configuration.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            await _configurationRepository.AddAsync(
                configuration,
                cancellationToken);
        }
        else
        {
            await _configurationRepository.UpdateAsync(
                configuration,
                cancellationToken);
        }

        await _auditLog.LogAsync(
            entityType: "CompanyConfiguration",
            entityId: request.CompanyId,
            action: isNew ? "Created" : "Updated",
            companyId: request.CompanyId,
            details: "Company configuration updated.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RequestDeletionAsync(
        Guid companyId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            return Result<bool>.Fail(
                "Company ID is required.");
        }

        if (!CanAccessCompany(user, companyId))
        {
            return Result<bool>.Fail(
                "You do not have access to this company.");
        }

        var company = await _companyRepository.GetByIdAsync(
            companyId,
            cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail(
                "Company not found.");
        }

        if (company.DeletionRequested)
        {
            return Result<bool>.Fail(
                "A deletion request already exists.");
        }

        company.DeletionRequested = true;
        company.UpdatedAt = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(
            company,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: company.Id,
            action: "DeletionRequested",
            companyId: company.Id,
            details: "Company deletion requested.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CancelDeletionAsync(
        Guid companyId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            return Result<bool>.Fail(
                "Company ID is required.");
        }

        if (!CanAccessCompany(user, companyId))
        {
            return Result<bool>.Fail(
                "You do not have access to this company.");
        }

        var company = await _companyRepository.GetByIdAsync(
            companyId,
            cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail(
                "Company not found.");
        }

        if (!company.DeletionRequested)
        {
            return Result<bool>.Fail(
                "No deletion request exists.");
        }

        company.DeletionRequested = false;
        company.UpdatedAt = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(
            company,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: company.Id,
            action: "DeletionRequestCancelled",
            companyId: company.Id,
            details: "Company deletion request cancelled.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    private static bool IsSuperAdmin(
        ClaimsPrincipal user)
    {
        return user?.IsInRole("SuperAdmin") == true;
    }

    private static bool IsAdministrator(
        ClaimsPrincipal user)
    {
        return user?.IsInRole("Administrator") == true;
    }

    private static Guid? GetUserCompanyId(
        ClaimsPrincipal user)
    {
        if (user is null)
            return null;

        var claim = user.FindFirst(
            "CompanyId")?.Value;

        return Guid.TryParse(
            claim,
            out var companyId) &&
            companyId != Guid.Empty
                ? companyId
                : null;
    }

    private static bool CanAccessCompany(
        ClaimsPrincipal user,
        Guid companyId)
    {
        if (user is null ||
            companyId == Guid.Empty)
        {
            return false;
        }

        if (IsSuperAdmin(user))
            return true;

        if (!IsAdministrator(user))
            return false;

        var userCompanyId =
            GetUserCompanyId(user);

        return userCompanyId == companyId;
    }
}
