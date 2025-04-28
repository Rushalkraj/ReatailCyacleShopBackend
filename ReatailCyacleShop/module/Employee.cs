using System.ComponentModel.DataAnnotations;

namespace RetailCycleShopAPI.module
{
    public class EmployeeResponse
    {
        [Key]
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
    }

}
