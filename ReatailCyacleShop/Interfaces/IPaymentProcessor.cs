using RetailCycleShopAPI.models.dtos;
using RetailCycleShopAPI.models.enums;

namespace RetailCycleShopAPI.Services
{
    public interface IPaymentProcessor
    {
        Task<PaymentResult> ProcessPayment(
            PaymentType paymentMethod,
            decimal amount,
            string orderNumber,
            Dictionary<string, string> paymentDetails);

        //Task<PaymentResult> ProcessRefund(string transactionId, decimal amount);
        Task<bool> VerifyWebhookSignature(PaymentWebhookDto webhookDto);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string ReceiptUrl { get; set; }
        public string ErrorMessage { get; set; }
    }
}
