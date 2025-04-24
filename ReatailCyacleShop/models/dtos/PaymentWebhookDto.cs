using RetailCycleShopAPI.models.enums;

namespace RetailCycleShopAPI.models.dtos
{
    public class PaymentWebhookDto
    {
        public string TransactionId { get; set; }
        public PaymentWebhookEventType EventType { get; set; }
        public string Signature { get; set; }
        public DateTime EventTime { get; set; }
    }
}
