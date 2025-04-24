// DummyPaymentProcessor.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RetailCycleShopAPI.models.dtos;
using RetailCycleShopAPI.models.enums;

namespace RetailCycleShopAPI.Services
{
    public class DummyPaymentProcessor : IPaymentProcessor
    {
        private readonly Random _random = new Random();

        public async Task<PaymentResult> ProcessPayment(
            PaymentType paymentMethod,
            decimal amount,
            string orderNumber,
            Dictionary<string, string> paymentDetails)
        {
            // Simulate processing delay
            await Task.Delay(1000);

            // 90% chance of success for demo purposes
            var success = _random.Next(1, 11) > 1;

            return new PaymentResult
            {
                Success = success,
                TransactionId = success ? $"DEMO-{Guid.NewGuid()}" : null,
                ReceiptUrl = success ? "https://example.com/receipt.pdf" : null,
                ErrorMessage = success ? null : "Simulated payment failure"
            };
        }

        public async Task<PaymentResult> ProcessRefund(string transactionId, decimal amount)
        {
            // Simulate processing delay
            await Task.Delay(800);

            return new PaymentResult
            {
                Success = true,
                TransactionId = $"REFUND-{Guid.NewGuid()}"
            };
        }

        public Task<bool> VerifyWebhookSignature(PaymentWebhookDto webhookDto)
        {
            // Always return true for dummy implementation
            return Task.FromResult(true);
        }
    }
}