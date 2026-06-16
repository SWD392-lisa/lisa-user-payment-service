using FluentValidation;

namespace ProjectLucy.Application.Payment.Commands.HandleIpn;

public class HandleIpnCommandValidator : AbstractValidator<HandleIpnCommand>
{
    public HandleIpnCommandValidator()
    {
        RuleFor(x => x.Request.OrderInvoiceNumber)
            .NotEmpty().WithMessage("order_invoice_number is required");

        RuleFor(x => x.Request.TransactionStatus)
            .NotEmpty().WithMessage("transaction_status is required");

        RuleFor(x => x.Request.Signature)
            .NotEmpty().WithMessage("signature is required");
    }
}
