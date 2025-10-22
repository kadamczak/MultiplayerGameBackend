using FluentValidation;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests.Validators;

// public class LoginDtoValidator : AbstractValidator<LoginDto>
// {
//     public LoginDtoValidator()
//     {
//         RuleFor(x => x.UserName)
//             .NotEmpty().WithMessage("Username is required.")
//             .Length(User.Constraints.UserNameMinLength, User.Constraints.UserNameMaxLength)
//             .WithMessage($"Username must be between {User.Constraints.UserNameMinLength} and {User.Constraints.UserNameMaxLength} characters long.");
//         
//         RuleFor(x => x.Password)
//             .NotEmpty().WithMessage("Password is required.");
//     }
// }