using FluentValidation;
using SmartX.Application.Requests.Mesh;

public class MeshSpanningTreeValidator
    : AbstractValidator<MeshSpanningTreeRequest>
{
    public MeshSpanningTreeValidator()
    {
        RuleFor(x => x.GatewayId).NotEmpty();
        RuleFor(x => x.StartNodeId).NotEmpty();
    }
}