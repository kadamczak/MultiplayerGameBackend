using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Items.Requests.Validators;

// public class UpdateItemDtoValidator : AbstractValidator<UpdateItemDto>
// {
//     public UpdateItemDtoValidator()
//     {
//         RuleFor(x => x.Id)
//             .GreaterThan(0).WithMessage("Id must be greater than 0.");
//
//         RuleFor(x => x.Name)
//             .NotEmpty().WithMessage("Name is required.")
//             .Length(Item.Constraints.NameMinLength, Item.Constraints.NameMaxLength)
//             .WithMessage($"Name must be between {Item.Constraints.NameMinLength} and {Item.Constraints.NameMaxLength} characters.");
//
//         RuleFor(x => x.Description)
//             .NotEmpty().WithMessage("Description is required.")
//             .Length(Item.Constraints.DescriptionMinLength, Item.Constraints.DescriptionMaxLength)
//             .WithMessage($"Description must be between {Item.Constraints.DescriptionMinLength} and {Item.Constraints.DescriptionMaxLength} characters.");
//     }
// }