using FluentValidation;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Payment.Commands.CreatePayment;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    private readonly ITransactionRepository _transactionRepo;

    public CreatePaymentCommandValidator(ITransactionRepository transactionRepo)
    {
        _transactionRepo = transactionRepo;

        RuleFor(x => x.Request.OrderInvoiceNumber)
            .NotEmpty().WithMessage("order_invoice_number is required")
            .MaximumLength(100)
            .MustAsync(BeUniqueReferenceAsync)
                .WithMessage("order_invoice_number is already in use");

        RuleFor(x => x.Request.OrderAmount)
            .GreaterThan(0).WithMessage("order_amount must be greater than 0");

        RuleFor(x => x.Request.OrderDescription)
            .NotEmpty().WithMessage("order_description is required")
            .MaximumLength(255);

        RuleFor(x => x.Request.CustomerId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.CustomerId));

        RuleFor(x => x.Request.PaymentMethod)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.PaymentMethod));
    }

    private async Task<bool> BeUniqueReferenceAsync(string invoice, CancellationToken ct)
        => !await _transactionRepo.ExistsByReferenceCodeAsync(invoice, ct);
}
