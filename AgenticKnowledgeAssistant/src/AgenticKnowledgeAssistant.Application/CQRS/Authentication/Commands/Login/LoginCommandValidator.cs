using FluentValidation;

namespace AgenticKnowledgeAssistant.Application.CQRS.Authentication.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Login request details are required");
        RuleFor(x => x.Request.Password).NotEmpty().WithMessage("Password is required");
        RuleFor(x => x.Request.Email).NotEmpty().WithMessage("Email or username is required");
    }
}
