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
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // Add timeout
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

            // Ensure amount is in the correct format (Cashfree expects amount in INR, no paise)
            var amount = Math.Round(request.Amount, 2);

            var cashfreeRequest = new
            {
                order_id = $"order_{order.OrderNumber}_{DateTime.UtcNow.Ticks}",
                order_amount = amount.ToString("0.00"), // Format as string with 2 decimal places
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
                     notify_url = "http://localhost:5104/api/cashfree/webhook"
                }
            };

            var clientId = _configuration["Cashfree:AppId"];
            var clientSecret = _configuration["Cashfree:SecretKey"];
            var baseUrl = _configuration["Cashfree:BaseUrl"] ?? "https://sandbox.cashfree.com/pg";

            if (string.IsNullOrEmpty(clientId) )throw new Exception("Cashfree AppId is missing");
            if (string.IsNullOrEmpty(clientSecret)) throw new Exception("Cashfree SecretKey is missing");

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl}/orders");

            requestMessage.Headers.Add("x-client-id", clientId);
            requestMessage.Headers.Add("x-client-secret", clientSecret);
            requestMessage.Headers.Add("x-api-version", "2022-09-01");

            var jsonContent = JsonSerializer.Serialize(cashfreeRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
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
                return StatusCode(500, $"Cashfree API Error: {responseContent}");
            }

            var cashfreeResponse = JsonSerializer.Deserialize<CashfreeOrderResponse>(responseContent);

            if (cashfreeResponse == null || string.IsNullOrEmpty(cashfreeResponse.payment_link))
            {
                _logger.LogError("Invalid Cashfree response format: {Response}", responseContent);
                return StatusCode(500, "Invalid response format from payment gateway");
            }

            return Ok(new
            {
                payment_link = cashfreeResponse.payment_link,
                order_id = cashfreeResponse.order_id,
                cf_order_id = cashfreeResponse.order_id // Return Cashfree's order ID
            });
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
            // Validate order exists
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order == null)
            {
                return BadRequest("Order not found");
            }

            // Verify payment with Cashfree
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

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Cashfree payment verification failed: {Response}", responseContent);
                return BadRequest(new { success = false, error = "Payment verification failed" });
            }

            var paymentsResponse = JsonSerializer.Deserialize<CashfreePaymentsResponse>(responseContent);

            // Check if any payment was successful
            var successfulPayment = paymentsResponse?.FirstOrDefault(p =>
                p.payment_status == "SUCCESS" ||
                p.payment_status == "COMPLETED");

            if (successfulPayment == null)
            {
                return BadRequest(new { success = false, error = "No successful payment found" });
            }

            // Payment is valid, create payment record
            var payment = new RetailCycleShopAPI.module.Payment
            {
                PaymentType = (int)PaymentType.Cashfree,
                Amount = request.Amount,
                Status = (int)PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderId = order.OrderId,
                ReceiptUrl = successfulPayment.payment_url ?? $"https://sandbox.cashfree.com/pg/orders/{request.CashfreeOrderId}"
            };

            _context.Payments.Add(payment);

            // Update order status
            order.Status = (int)OrderStatus.Processing;
            order.PaymentId = payment.PaymentId;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, paymentId = payment.PaymentId });
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
            // Read request body
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            // Verify signature
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

            // Process the webhook based on event type
            switch (webhookData.type)
            {
                case "ORDER.PAYMENT_COMPLETED":
                    await ProcessPaymentCompleted(webhookData.data);
                    break;
                case "ORDER.PAYMENT_FAILED":
                    await ProcessPaymentFailed(webhookData.data);
                    break;
                    // Add other event types as needed
            }

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

    private async Task ProcessPaymentCompleted(CashfreeWebhookData.Data data)
    {
        // Extract order ID from the reference ID (order_123_123456789)
        var orderNumber = data.order.order_id.Split('_')[1];

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        if (order == null) return;

        // Create payment record
        var payment = new RetailCycleShopAPI.module.Payment
        {
            PaymentType = (int)PaymentType.Cashfree,
            Amount = data.order.order_amount,
            Status = (int)PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderId = order.OrderId,
            ReceiptUrl = data.payment.payment_url ?? $"https://sandbox.cashfree.com/pg/orders/{data.order.order_id}"
        };

        _context.Payments.Add(payment);

        // Update order status
        order.Status = (int)OrderStatus.Processing;
        order.PaymentId = payment.PaymentId;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task ProcessPaymentFailed(CashfreeWebhookData.Data data)
    {
        // Handle failed payment
        var orderNumber = data.order.order_id.Split('_')[1];

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        if (order == null) return;

        // Create payment record with failed status
        var payment = new RetailCycleShopAPI.module.Payment
        {
            PaymentType = (int)PaymentType.Cashfree,
            Amount = data.order.order_amount,
            Status = (int)PaymentStatus.Failed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderId = order.OrderId
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
    }
}

public class CashfreeOrderRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentVerificationRequest
{
    public int OrderId { get; set; }
    public string CashfreeOrderId { get; set; }
    public decimal Amount { get; set; }
}

public class CashfreeOrderResponse
{
    public string order_id { get; set; }
    public string payment_link { get; set; }
}

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
}