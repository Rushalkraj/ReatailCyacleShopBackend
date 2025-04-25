using Microsoft.AspNetCore.Mvc;
using Razorpay.Api;
using RetailCycleShopAPI.models.enums;
using RetailCycleShopAPI.module;
using Microsoft.EntityFrameworkCore; // Changed from System.Data.Entity
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("api/[controller]")]
public class RazorpayController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RazorpayController> _logger;

    public RazorpayController(
        IConfiguration configuration,
        ApplicationDbContext context,
        ILogger<RazorpayController> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateRazorpayOrder([FromBody] RazorpayOrderRequest request)
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

            // Initialize Razorpay client
            RazorpayClient client = new RazorpayClient(
                _configuration["Razorpay:KeyId"],
                _configuration["Razorpay:KeySecret"]);

            // Create order options
            Dictionary<string, object> options = new Dictionary<string, object>
            {
                { "amount", Convert.ToInt32(request.Amount * 100) }, // Amount in paise
                { "currency", "INR" },
                { "receipt", $"order_{order.OrderNumber}" },
                { "payment_capture", 1 } // Auto-capture payment
            };

            // Create Razorpay order
            Razorpay.Api.Order razorpayOrder = client.Order.Create(options);

            // Return order ID to frontend
            return Ok(new
            {
                id = razorpayOrder["id"],
                currency = razorpayOrder["currency"],
                amount = razorpayOrder["amount"]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Razorpay order");
            return StatusCode(500, "Error creating payment order");
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

            // Verify payment signature
            RazorpayClient client = new RazorpayClient(
                _configuration["Razorpay:KeyId"],
                _configuration["Razorpay:KeySecret"]);

            Dictionary<string, string> attributes = new Dictionary<string, string>
            {
                { "razorpay_payment_id", request.RazorpayPaymentId },
                { "razorpay_order_id", request.RazorpayOrderId },
                { "razorpay_signature", request.RazorpaySignature }
            };

            Utils.verifyPaymentSignature(attributes);

            // Payment is valid, create payment record
            var payment = new RetailCycleShopAPI.module.Payment
            {
                PaymentType = (int)PaymentType.Razorpay,
                Amount = request.Amount,
                Status = (int)PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderId = order.OrderId,
                ReceiptUrl = $"https://dashboard.razorpay.com/app/payments/{request.RazorpayPaymentId}"
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
}

public class RazorpayOrderRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentVerificationRequest
{
    public int OrderId { get; set; }
    public string RazorpayPaymentId { get; set; }
    public string RazorpayOrderId { get; set; }
    public string RazorpaySignature { get; set; }
    public decimal Amount { get; set; }
}