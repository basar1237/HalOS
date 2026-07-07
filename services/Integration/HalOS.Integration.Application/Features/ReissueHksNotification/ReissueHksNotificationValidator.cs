using FluentValidation;

namespace HalOS.Integration.Application.Features.ReissueHksNotification;

/// <summary>ReissueHksNotification doğrulaması (docs/07 §5).</summary>
public sealed class ReissueHksNotificationValidator : AbstractValidator<ReissueHksNotificationCommand>
{
    public ReissueHksNotificationValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty().WithMessage("Bildirim kimliği zorunludur.");
    }
}
