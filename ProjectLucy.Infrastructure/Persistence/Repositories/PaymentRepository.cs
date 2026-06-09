using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Interfaces;
using PaymentEntity = ProjectLucy.Domain.Entities.Payment;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly NeondbContext _context;

    public PaymentRepository(NeondbContext context)
    {
        _context = context;
    }

    public Task<PaymentEntity?> GetByInvoiceNumberAsync(string orderInvoiceNumber, CancellationToken ct = default)
        => _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderInvoiceNumber == orderInvoiceNumber, ct);

    public Task<bool> ExistsByTransactionIdAsync(string transactionId, CancellationToken ct = default)
        => _context.Payments
            .AsNoTracking()
            .AnyAsync(p => p.TransactionId == transactionId, ct);

    public async Task AddAsync(PaymentEntity payment, CancellationToken ct = default)
        => await _context.Payments.AddAsync(payment, ct);

    public void Update(PaymentEntity payment)
        => _context.Payments.Update(payment);
}
