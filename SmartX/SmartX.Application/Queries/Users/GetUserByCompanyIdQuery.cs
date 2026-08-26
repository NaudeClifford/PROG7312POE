namespace SmartX.Application.Queries.Users;

public class GetUsersByCompanyIdQuery
{
    public Guid CompanyId { get; }

    public GetUsersByCompanyIdQuery(
        Guid companyId)
    {
        CompanyId = companyId;
    }
}