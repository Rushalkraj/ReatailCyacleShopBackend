using Microsoft.AspNetCore.Mvc;
using RetailCycleShopAPI.models.enums;
using RetailCycleShopAPI.module;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

[ApiController]
[Route("api/[controller]")]
public class CashfreeController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CashfreeController> _logger;
    private readonly HttpClient _httpClient;

    public CashfreeController(
        IConfiguration configuration,
        ApplicationDbContext context,
        ILogger<CashfreeController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateCashfreeOrder([FromBody] CashfreeOrderRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Cashfree order for OrderId: {OrderId}", request.OrderId);

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found for OrderId: {OrderId}", request.OrderId);
                return BadRequest("Order not found");
            }

            if (request.Amount != order.TotalAmount)
            {
                _logger.LogWarning("Amount mismatch for OrderId: {OrderId}. Requested: {RequestAmount}, Order: {OrderAmount}",
                    request.OrderId, request.Amount, order.TotalAmount);
                return BadRequest("Amount does not match order total");
            }

            var cashfreeOrderId = $"order_{order.OrderNumber}_{DateTime.UtcNow.Ticks}";
            var amount = Math.Round(request.Amount, 2);

            var cashfreeRequest = new
            {
                order_id = cashfreeOrderId,
                order_amount = amount.ToString("0.00"),
                order_currency = "INR",
                customer_details = new
                {
                    customer_id = order.CustomerId.ToString(),
                    customer_email = order.Customer?.Email ?? "no-email@example.com",
                    customer_phone = order.Customer?.Phone ?? "9999999999"
                },
                order_meta = new
                {
                    return_url = $"{_configuration["Frontend:BaseUrl"]}/payment/callback?order_id={order.OrderId}",
                    notify_url = $"{_configuration["Backend:BaseUrl"]}/api/cashfree/webhook"
                }
            };

            var clientId = _configuration["Cashfree:AppId"];
            var clientSecret = _configuration["Cashfree:SecretKey"];
            var baseUrl = _configuration["Cashfree:BaseUrl"] ?? "https://sandbox.cashfree.com/pg";

            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Cashfree AppId is missing in configuration");
                return StatusCode(500, "Payment gateway configuration error");
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Cashfree SecretKey is missing in configuration");
                return StatusCode(500, "Payment gateway configuration error");
            }

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl}/orders");

            requestMessage.Headers.Add("x-client-id", clientId);
            requestMessage.Headers.Add("x-client-secret", clientSecret);
            requestMessage.Headers.Add("x-api-version", "2022-09-01");

            var jsonContent = JsonSerializer.Serialize(cashfreeRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Cashfree Request: {Request}", jsonContent);

            requestMessage.Content = new StringContent(
                jsonContent,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Cashfree Response: {StatusCode} - {Response}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Cashfree API Error: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);
                return StatusCode(500, $"Payment gateway error: {responseContent}");
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                if (!root.TryGetProperty("payment_session_id", out var paymentSessionId))
                {
                    _logger.LogError("Missing payment_session_id in response");
                    return StatusCode(500, "Invalid response from payment gateway");
                }

                // Construct the payment link manually since it's not directly in the response
                var paymentLink = $"https://sandbox.cashfree.com/pg/redirection/#/{paymentSessionId.GetString()}";

                return Ok(new
                {
                    payment_link = paymentLink,
                    order_id = order.OrderId,
                    cf_order_id = cashfreeOrderId
                });
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to parse Cashfree response");
                return StatusCode(500, "Failed to process payment gateway response");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateCashfreeOrder");
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
    [HttpPost("verify-payment")]
    public async Task<IActionResult> VerifyPayment([FromBody] PaymentVerificationRequest request)
    {
        try
        {
            _logger.LogInformation("Verifying payment for OrderId: {OrderId}, CashfreeOrderId: {CashfreeOrderId}",
                request.OrderId, request.CashfreeOrderId);

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found for OrderId: {OrderId}", request.OrderId);
                return BadRequest(new { success = false, error = "Order not found" });
            }

            var clientId = _configuration["Cashfree:AppId"];
            var clientSecret = _configuration["Cashfree:SecretKey"];
            var baseUrl = _configuration["Cashfree:BaseUrl"] ?? "https://sandbox.cashfree.com/pg";

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}/orders/{request.CashfreeOrderId}/payments");

            requestMessage.Headers.Add("x-client-id", clientId);
            requestMessage.Headers.Add("x-client-secret", clientSecret);
            requestMessage.Headers.Add("x-api-version", "2022-09-01");

            var response = await _httpClient.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Cashfree Verification Response: {StatusCode} - {Response}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Cashfree payment verification failed: {Response}", responseContent);
                return BadRequest(new { success = false, error = "Payment verification failed" });
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseContent);
                var payments = doc.RootElement.EnumerateArray();

                var successfulPayment = payments.FirstOrDefault(p =>
                    p.GetProperty("payment_status").GetString() == "SUCCESS" ||
                    p.GetProperty("payment_status").GetString() == "COMPLETED");

                if (successfulPayment.ValueKind == JsonValueKind.Undefined)
                {
                    _logger.LogWarning("No successful payment found for OrderId: {OrderId}", request.OrderId);
                    return BadRequest(new
                    {
                        success = false,
                        error = "Payment not completed yet. Please check again later."
                    });
                }

                var payment = new Payment
                {
                    PaymentType = (int)PaymentType.Cashfree,
                    Amount = request.Amount,
                    Status = (int)PaymentStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OrderId = order.OrderId,
                    ReceiptUrl = successfulPayment.TryGetProperty("payment_url", out var url) ?
                        url.GetString() :
                        $"https://sandbox.cashfree.com/pg/orders/{request.CashfreeOrderId}"
                };

                _context.Payments.Add(payment);

                order.Status = (int)OrderStatus.Processing;
                order.PaymentId = payment.PaymentId;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    paymentId = payment.PaymentId,
                    status = "SUCCESS",
                    cf_payment_id = successfulPayment.GetProperty("cf_payment_id").GetString()
                });
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to parse Cashfree verification response");
                return StatusCode(500, new { success = false, error = "Failed to process payment verification" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

 
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            var signature = Request.Headers["x-webhook-signature"].FirstOrDefault();
            var secret = _configuration["Cashfree:WebhookSecret"];

            if (!VerifySignature(requestBody, signature, secret))
            {
                return Unauthorized();
            }

            var webhookData = JsonSerializer.Deserialize<CashfreeWebhookData>(requestBody);

            if (webhookData == null)
            {
                return BadRequest("Invalid webhook data");
            }

            // Extract order number from Cashfree order ID (order_12345_123456789)
            var orderNumber = webhookData.data.order.order_id.Split('_')[1];
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null) return Ok();

            switch (webhookData.type)
            {
                case "ORDER.PAYMENT_COMPLETED":
                    var payment = new Payment
                    {
                        PaymentType = (int)PaymentType.Cashfree,
                        Amount = webhookData.data.order.order_amount,
                        Status = (int)PaymentStatus.Completed,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        OrderId = order.OrderId,
                        ReceiptUrl = webhookData.data.payment.payment_url ?? $"https://sandbox.cashfree.com/pg/orders/{webhookData.data.order.order_id}"
                    };

                    _context.Payments.Add(payment);
                    order.Status = (int)OrderStatus.Processing;
                    order.PaymentId = payment.PaymentId;
                    break;

                case "ORDER.PAYMENT_FAILED":
                    var failedPayment = new Payment
                    {
                        PaymentType = (int)PaymentType.Cashfree,
                        Amount = webhookData.data.order.order_amount,
                        Status = (int)PaymentStatus.Failed,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        OrderId = order.OrderId
                    };

                    _context.Payments.Add(failedPayment);
                    break;
            }

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }

    private bool VerifySignature(string payload, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = BitConverter.ToString(computedHash).Replace("-", "").ToLower();
        return computedSignature == signature?.ToLower();
    }
}

// DTO classes (add these to your project)
public class CashfreeOrderRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
}


public class CashfreeOrderResponse
{
    public string order_id { get; set; }
    public string payment_link { get; set; }
}

public class PaymentVerificationRequest
{
    public int OrderId { get; set; }
    public string CashfreeOrderId { get; set; }
    public decimal Amount { get; set; }
}

//public class CashfreeOrderResponse
//{
//    public string order_id { get; set; }
//    public string payment_link { get; set; }
//}

public class CashfreePaymentsResponse : List<CashfreePayment>
{
}

public class CashfreePayment
{
    public string cf_payment_id { get; set; }
    public string payment_status { get; set; }
    public string payment_url { get; set; }
}

public class CashfreeWebhookData
{
    public string type { get; set; }
    public Data data { get; set; }

    public class Data
    {
        public Order order { get; set; }
        public Payment payment { get; set; }

        public class Order
        {
            public string order_id { get; set; }
            public decimal order_amount { get; set; }
        }

        public class Payment
        {
            public string cf_payment_id { get; set; }
            public string payment_status { get; set; }
            public string payment_url { get; set; }
        }
    }
    public class PaymentVerificationRequest
    {
        public int OrderId { get; set; }
        public string CashfreeOrderId { get; set; }
        public decimal Amount { get; set; }
    }
}