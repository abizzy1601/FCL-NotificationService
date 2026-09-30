using FluentValidation;

namespace NotificationService.Application.UseCases.Command.CreateNotification
{
    public class CreateNotificationValidator : AbstractValidator<CreateNotificationCommand>
    {
        public CreateNotificationValidator()
        {
            RuleFor(x => x.EventId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        }
    }
}
