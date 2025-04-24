using RetailCycleShopAPI.models.dtos;

public class CustomerUpdateDto
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public AddressUpdateDto Address { get; set; }
}
