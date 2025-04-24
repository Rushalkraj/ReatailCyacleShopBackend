using RetailCycleShopAPI.Models;
using RetailCycleShopAPI.module;
using System.Threading.Tasks;

namespace RetailCycleShopAPI.Interfaces
{
    public interface IInvitationService
    {
        Task<InvitedUser> CreateInvitation(AdminCreateUserModel model);
        Task<bool> IsEmailInvited(string email);
        Task<InvitedUser?> GetInvitationByToken(string token);
        Task MarkAsRegistered(string email);
    }
}