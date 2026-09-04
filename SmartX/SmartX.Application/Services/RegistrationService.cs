using AutoMapper;
using FirebaseAdmin.Auth;
using SmartX.Application.Authentication;
using SmartX.Application.Requests.Company;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Services.Registration;

public class RegistrationService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IFirebaseTokenService _firebaseTokenService;

    public RegistrationService(
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        IMapper mapper,
        IFirebaseTokenService firebaseTokenService)
    {
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _firebaseTokenService = firebaseTokenService;
    }

    public async Task<Result<RegistrationResult>> RegisterAsync(
    RegisterCompanyRequest request,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return Result<RegistrationResult>.Fail(
                "Company name is required.");

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return Result<RegistrationResult>.Fail(
                "Display name is required.");

        if (string.IsNullOrWhiteSpace(request.IdToken))
            return Result<RegistrationResult>.Fail(
                "Firebase authentication is required.");

        var firebaseUser =
            await _firebaseTokenService.VerifyIdTokenAsync(
                request.IdToken,
                cancellationToken);

        if (firebaseUser is null)
        {
            return Result<RegistrationResult>.Fail(
                "Invalid Firebase authentication.");
        }

        var existingUser =
            await _userRepository.GetByFirebaseUidAsync(
                firebaseUser.FirebaseUid,
                cancellationToken);

        if (existingUser is not null)
        {
            return Result<RegistrationResult>.Fail(
                "A SmartX account already exists for this Firebase account.");
        }

        var now = DateTime.UtcNow;

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,

            IsActive = true,
            OnboardingComplete = false,

            CreatedAt = now,
            UpdatedAt = now
        };



        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            FirebaseUid = firebaseUser.FirebaseUid,
            Email = firebaseUser.Email,
            DisplayName = request.DisplayName.Trim(),
            Role = UserRole.Administrator,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _companyRepository.AddAsync(
            company,
            cancellationToken);

        try
        {
            await _userRepository.AddAsync(
                user,
                cancellationToken);
        }
        catch
        {
            await _companyRepository.DeleteAsync(
                company.Id,
                cancellationToken);

            throw;
        }

        return Result<RegistrationResult>.Ok(
            new RegistrationResult
            {
                CompanyId = company.Id,
                User = _mapper.Map<UserDto>(user)
            });
    }

    public async Task<Result<bool>> CompleteOnboardingAsync(
    Guid companyId,
    CancellationToken cancellationToken = default)
    {
        var company =
            await _companyRepository.GetByIdAsync(
                companyId,
                cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail(
                "Company was not found.");
        }

        if (!company.IsActive)
        {
            return Result<bool>.Fail(
                "Company is inactive.");
        }

        company.OnboardingComplete = true;
        company.UpdatedAt = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(
            company,
            cancellationToken);

        return Result<bool>.Ok(true);
    }

}
