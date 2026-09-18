using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository
{
    public List<Payment> Payments = new();
    
    public void Add(Payment payment)
    {
        Payments.Add(payment);
    }

    public Payment? Get(Guid id)
    {
        return Payments.FirstOrDefault(p => p.Id == id);
    }
}