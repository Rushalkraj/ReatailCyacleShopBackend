using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailCycleShopAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using RetailCycleShopAPI.module;
using RetailCycleShopAPI.Models.Dtos;
using RetailCycleShopAPI.models.dtos;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")] 
    public class CustomerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Customer
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _context.Customers
                .Include(c => c.BillingAddress)
                .Include(c => c.ShippingAddress)
                .ToListAsync();
        }

        // GET: api/Customer/5
        // In CustomerController.cs
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.ShippingAddress) // Add this line
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
        }

        // POST: api/Customer
        [HttpPost("with-address")]
        public async Task<ActionResult<Customer>> CreateCustomerWithAddress([FromBody] CustomerCreate1Dto customerDto)
        {
            // Validate input
            if (string.IsNullOrEmpty(customerDto.Email))
            {
                return BadRequest("Email is required");
            }

            // Check if customer exists
            if (await _context.Customers.AnyAsync(c => c.Email == customerDto.Email))
            {
                return Conflict("Customer already exists");
            }

            // Create customer
            var customer = new Customer
            {
                FirstName = customerDto.FirstName,
                LastName = customerDto.LastName,
                Email = customerDto.Email,
                Phone = customerDto.Phone,
                CreatedAt = DateTime.UtcNow
            };

            // Create address
            var address = new Address
            {
                StreetLine1 = customerDto.Address.StreetLine1,
                StreetLine2 = customerDto.Address.StreetLine2,
                City = customerDto.Address.City,
                State = customerDto.Address.State,
                PostalCode = customerDto.Address.PostalCode,
                Country = customerDto.Address.Country,
                CreatedAt = DateTime.UtcNow
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            if (customerDto.Address.IsDefaultShipping)
            {
                customer.ShippingAddressId = address.AddressId;
            }
            if (customerDto.Address.IsDefaultBilling)
            {
                customer.BillingAddressId = address.AddressId;
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, customer);
        }


        // PUT: api/Customer/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return BadRequest();
            }

            customer.UpdatedAt = DateTime.UtcNow;
            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
        [HttpPut("{id}/with-address")]
        public async Task<ActionResult<Customer>> UpdateCustomerWithAddress(int id, [FromBody] CustomerUpdateDto customerDto)
        {
            var customer = await _context.Customers
                .Include(c => c.ShippingAddress)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            customer.FirstName = customerDto.FirstName;
            customer.LastName = customerDto.LastName;
            customer.Email = customerDto.Email;
            customer.Phone = customerDto.Phone;
            customer.UpdatedAt = DateTime.UtcNow;

          
            if (customerDto.Address != null)
            {
                if (customer.ShippingAddress == null)
                {
                    // Create new address
                    var address = new Address
                    {
                        StreetLine1 = customerDto.Address.StreetLine1,
                        StreetLine2 = customerDto.Address.StreetLine2,
                        City = customerDto.Address.City,
                        State = customerDto.Address.State,
                        PostalCode = customerDto.Address.PostalCode,
                        Country = customerDto.Address.Country,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Addresses.Add(address);
                    await _context.SaveChangesAsync();
                    customer.ShippingAddressId = address.AddressId;
                }
                else
                {
                    
                    customer.ShippingAddress.StreetLine1 = customerDto.Address.StreetLine1;
                    customer.ShippingAddress.StreetLine2 = customerDto.Address.StreetLine2;
                    customer.ShippingAddress.City = customerDto.Address.City;
                    customer.ShippingAddress.State = customerDto.Address.State;
                    customer.ShippingAddress.PostalCode = customerDto.Address.PostalCode;
                    customer.ShippingAddress.Country = customerDto.Address.Country;
                }
            }

            await _context.SaveChangesAsync();

              return customer;
        }

        // DELETE: api/Customer/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            // First, remove all inventory histories related to customer's orders
            var orderIds = customer.Orders.Select(o => o.OrderId).ToList();
            var inventoryHistories = await _context.InventoryHistories
                .Where(ih => orderIds.Contains(ih.OrderId ?? 0))
                .ToListAsync();

            _context.InventoryHistories.RemoveRange(inventoryHistories);

            // Then remove the customer's orders and order items
            _context.OrderItems.RemoveRange(customer.Orders.SelectMany(o => o.OrderItems));
            _context.Orders.RemoveRange(customer.Orders);

            // Finally remove the customer
            _context.Customers.Remove(customer);
            
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}