
using FluentValidation;
using SmartX.Application.Requests.Mesh;

namespace SmartX.Application.Validators.Mesh;

public class MeshRouteValidator
    : AbstractValidator<MeshRouteRequest>
{
    public MeshRouteValidator()
    {
        RuleFor(x => x.GatewayId)
            .NotEmpty();

        RuleFor(x => x.SourceNodeId)
            .NotEmpty();

        RuleFor(x => x.DestinationNodeId)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.SourceNodeId != x.DestinationNodeId)
            .WithMessage(
                "Source and destination must be different.");
    }
}
