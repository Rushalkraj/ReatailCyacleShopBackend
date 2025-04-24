using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailCycleShopAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using RetailCycleShopAPI.module;
using RetailCycleShopAPI.Models.Dtos;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")] 
    public class AddressController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AddressController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Address
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Address>>> GetAddresses()
        {
            return await _context.Addresses.ToListAsync();
        }

        // GET: api/Address/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Address>> GetAddress(int id)
        {
            var address = await _context.Addresses.FindAsync(id);

            if (address == null)
            {
                return NotFound();
            }

            return address;
        }
        [HttpGet("customer/{customerId}/addresses")]
        [Authorize(Roles = "Customer,Admin,Employee")] 
        public async Task<ActionResult<IEnumerable<Address>>> GetAddressesByCustomerId(int customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.BillingAddress)
                .Include(c => c.ShippingAddress)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
                return NotFound("Customer not found.");

            var addresses = new List<Address>();

            if (customer.BillingAddress != null)
                addresses.Add(customer.BillingAddress);

            if (customer.ShippingAddress != null &&
                customer.ShippingAddress.AddressId != customer.BillingAddress?.AddressId)
                addresses.Add(customer.ShippingAddress);

            return Ok(addresses);
        }


        // POST: api/Address
        [HttpPost("for-customer/{customerId}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<Address>> CreateAddressForCustomer(int customerId, [FromBody] AddressCreateDto addressDto)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound("Customer not found");
            }

            var address = new Address
            {
                StreetLine1 = addressDto.StreetLine1,
                StreetLine2 = addressDto.StreetLine2,
                City = addressDto.City,
                State = addressDto.State,
                PostalCode = addressDto.PostalCode,
                Country = addressDto.Country,
                CreatedAt = DateTime.UtcNow
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

           
            if (addressDto.IsDefaultBilling)
            {
                customer.BillingAddressId = address.AddressId;
            }
            if (addressDto.IsDefaultShipping)
            {
                customer.ShippingAddressId = address.AddressId;
            }

            await _context.SaveChangesAsync();

            return address;
        }

        // PUT: api/Address/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAddress(int id, Address address)
        {
            if (id != address.AddressId)
            {
                return BadRequest();
            }

            _context.Entry(address).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AddressExists(id))
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

        // DELETE: api/Address/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
            {
                return NotFound();
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AddressExists(int id)
        {
            return _context.Addresses.Any(e => e.AddressId == id);
        }
    }
}