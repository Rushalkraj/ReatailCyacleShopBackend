// Services/InvitationService.cs
using Microsoft.EntityFrameworkCore;
using RetailCycleShopAPI.Interfaces;
using RetailCycleShopAPI.Models;
using RetailCycleShopAPI.module;
using System;
using System.Threading.Tasks;

namespace RetailCycleShopAPI.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public InvitationService(
            ApplicationDbContext context,
            IEmailService emailService,
            IConfiguration config)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<InvitedUser> CreateInvitation(AdminCreateUserModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var token = Guid.NewGuid().ToString();

            var invitedUser = new InvitedUser
            {
                Email = model.Email,
                FullName = model.FullName,
                Role = model.Role,
                Token = token,
                IsRegistered = false,
                InvitationDate = DateTime.UtcNow
            };

            _context.InvitedUsers.Add(invitedUser);
            await _context.SaveChangesAsync();

            var setupLink = $"{_config["Frontend:BaseUrl"]}/register?token={token}&email={model.Email}";
            await _emailService.SendPasswordSetupEmail(model.Email, model.FullName, setupLink);

            return invitedUser;
        }

        public async Task<bool> IsEmailInvited(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;

            return await _context.InvitedUsers
                .AsNoTracking()
                .AnyAsync(i => i.Email == email && !i.IsRegistered);
        }

        public async Task<InvitedUser?> GetInvitationByToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            return await _context.InvitedUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Token == token && !i.IsRegistered);
        }

        public async Task MarkAsRegistered(string email)
        {
            if (string.IsNullOrEmpty(email)) return;

            var invitedUser = await _context.InvitedUsers
                .FirstOrDefaultAsync(i => i.Email == email);

            if (invitedUser != null)
            {
                invitedUser.IsRegistered = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}