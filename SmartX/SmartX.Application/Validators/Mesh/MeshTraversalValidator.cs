
using FluentValidation;
using SmartX.Application.Requests.Mesh;

namespace SmartX.Application.Validators.Mesh;

public class MeshTraversalValidator
    : AbstractValidator<MeshTraversalRequest>
{
    public MeshTraversalValidator()
    {
        RuleFor(x => x.GatewayId).NotEmpty();
        RuleFor(x => x.StartNodeId).NotEmpty();
    }
}

