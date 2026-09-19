using Prometheus;

using PaymentGateway.Api.Models;

public static class PaymentMetrics
{
    private static readonly Counter PaymentsTotal = Metrics
        .CreateCounter(
            "payments_total",
            "Payments processed by the gateway, labelled by status.",
            new CounterConfiguration { LabelNames = ["status"] });

    public static void Record(PaymentStatus status) =>
        PaymentsTotal.WithLabels(status.ToString()).Inc();
}