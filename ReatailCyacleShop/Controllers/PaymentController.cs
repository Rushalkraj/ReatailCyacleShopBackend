using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailCycleShopAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using RetailCycleShopAPI.models.dtos;
using RetailCycleShopAPI.models.enums;
using RetailCycleShopAPI.Services;
using RetailCycleShopAPI.module;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaymentProcessor _paymentProcessor;

        public PaymentController(
            ApplicationDbContext context,
            ILogger<PaymentController> logger,
            IPaymentProcessor paymentProcessor)
        {
            _context = context;
            _logger = logger;
            _paymentProcessor = paymentProcessor;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            return await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.Customer)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.Customer)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            return payment == null ? NotFound() : payment;
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> ProcessPayment([FromBody] PaymentProcessDto paymentDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate order exists
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.OrderId == paymentDto.OrderId);

                if (order == null)
                {
                    return BadRequest("Order not found");
                }

                // Process payment with external service
                var paymentResult = await _paymentProcessor.ProcessPayment(
                    paymentDto.PaymentMethod,
                    paymentDto.Amount,
                    order.OrderNumber,
                    paymentDto.PaymentDetails);

                if (!paymentResult.Success)
                {
                    return BadRequest(paymentResult.ErrorMessage);
                }

                // Create payment record
                var payment = new Payment
                {
                    PaymentType = (int)paymentDto.PaymentMethod,
                    Amount = paymentDto.Amount,
                    Status = (int)PaymentStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OrderId = order.OrderId,
                    //TransactionId = paymentResult.TransactionId,
                    ReceiptUrl = paymentResult.ReceiptUrl
                };

                _context.Payments.Add(payment);

                // Update order status
                order.Status = (int)OrderStatus.Processing;
                order.PaymentId = payment.PaymentId;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId }, payment);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing payment");
                return StatusCode(500, "Internal server error");
            }
        }

        //[HttpPost("webhook")]
        //[AllowAnonymous]
        //public async Task<IActionResult> PaymentWebhook([FromBody] PaymentWebhookDto webhookDto)
        //{
        //    try
        //    {
        //        var isValid = await _paymentProcessor.VerifyWebhookSignature(webhookDto);
        //        if (!isValid)
        //        {
        //            return Unauthorized();
        //        }

        //        var payment = await _context.Payments
        //            .FirstOrDefaultAsync(p => p.TransactionId == webhookDto.TransactionId);

        //        if (payment == null)
        //        {
        //            return NotFound();
        //        }

        //        switch (webhookDto.EventType)
        //        {
        //            case PaymentWebhookEventType.Completed:
        //                payment.Status = (int)PaymentStatus.Completed;
        //                break;
        //            case PaymentWebhookEventType.Failed:
        //                payment.Status = (int)PaymentStatus.Failed;
        //                break;
        //            case PaymentWebhookEventType.Refunded:
        //                payment.Status = (int)PaymentStatus.Refunded;
        //                break;
        //        }

        //        payment.UpdatedAt = DateTime.UtcNow;
        //        await _context.SaveChangesAsync();

        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error processing payment webhook");
        //        return StatusCode(500);
        //    }
        //}

    //    [HttpPost("refund/{paymentId}")]
    //    public async Task<IActionResult> ProcessRefund(int paymentId)
    //    {
    //        using var transaction = await _context.Database.BeginTransactionAsync();
    //        try
    //        {
    //            var payment = await _context.Payments
    //                .Include(p => p.Order)
    //                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

    //            if (payment == null)
    //            {
    //                return NotFound();
    //            }

    //            var refundResult = await _paymentProcessor.ProcessRefund(
    //                payment.TransactionId,
    //                payment.Amount);

    //            if (!refundResult.Success)
    //            {
    //                return BadRequest(refundResult.ErrorMessage);
    //            }

    //            payment.Status = (int)PaymentStatus.Refunded;
    //            payment.UpdatedAt = DateTime.UtcNow;

    //            if (payment.Order != null)
    //            {
    //                payment.Order.Status = (int)OrderStatus.Cancelled;
    //                payment.Order.UpdatedAt = DateTime.UtcNow;
    //            }

    //            await _context.SaveChangesAsync();
    //            await transaction.CommitAsync();

    //            return NoContent();
    //        }
    //        catch (Exception ex)
    //        {
    //            await transaction.RollbackAsync();
    //            _logger.LogError(ex, "Error processing refund");
    //            return StatusCode(500, "Internal server error");
    //        }
    //    }
    }
}