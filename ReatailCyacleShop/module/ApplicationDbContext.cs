using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RetailCycleShopAPI.Controllers;
using RetailCycleShopAPI.Models;
using RetailCycleShopAPI.Models.Identity;

namespace RetailCycleShopAPI.module
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {


        public DbSet<Cycle> Cycles { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Inventory> Inventories { get; set; } = null!;
        public DbSet<InvitedUser>? InvitedUsers { get; set; }
        public DbSet<EmployeeResponse>? employeeResponses { get; set; }
        public DbSet<LoginModel>? loginModels { get; set; }

        public DbSet<OrderItem>? orderItems { get; set; }
        public DbSet<AdminCreateUserModel>? adminCreateUserModels { get; set; }

        public DbSet<InventoryHistory> InventoryHistories { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Address> Addresses { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<ForgotPasswordModel> forgotPasswordModels { get; set; } = null!;
        public DbSet<ResetPasswordModel>? ResetPasswordModels { get; set; }

      

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Order-Payment relationship
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId)
                .IsRequired(false);
            modelBuilder.Entity<InvitedUser>(entity =>
            {
                entity.HasIndex(i => i.Email).IsUnique();
                entity.HasIndex(i => i.Token).IsUnique();
            });

            // Configure Order-OrderItems relationship
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId);

            // Configure Order-ShippingAddress relationship
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId);

            // Configure Order-Customer relationship
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId);

            // Configure Customer-Address relationships
            modelBuilder.Entity<Customer>()
               .HasOne(c => c.BillingAddress)
               .WithMany()
               .HasForeignKey(c => c.BillingAddressId)
               .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.ShippingAddress)
                .WithMany()
                .HasForeignKey(c => c.ShippingAddressId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.Customer)
                .WithMany()
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Cycle>()
        .HasOne(c => c.Inventory)
        .WithOne(i => i.Cycle)
        .HasForeignKey<Inventory>(i => i.CycleId);

            // Configure InventoryHistory relationships
            modelBuilder.Entity<InventoryHistory>()
                .HasKey(ih => ih.HistoryId);

            modelBuilder.Entity<InventoryHistory>()
                .HasOne(ih => ih.Cycle)
                .WithMany(c => c.InventoryHistories)
                .HasForeignKey(ih => ih.CycleId);

            modelBuilder.Entity<InventoryHistory>()
                .HasOne(ih => ih.Order)
                .WithMany()
                .HasForeignKey(ih => ih.OrderId)
                .IsRequired(false);

            // Configure enum conversions
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentType)
                .HasConversion<int>();

            modelBuilder.Entity<Payment>()
                .Property(p => p.Status)
                .HasConversion<int>();
            modelBuilder.Entity<InventoryHistory>(entity =>
            {
                entity.HasKey(ih => ih.HistoryId);

                entity.HasOne(ih => ih.Cycle)
                    .WithMany(c => c.InventoryHistories)
                    .HasForeignKey(ih => ih.CycleId);

                entity.HasOne(ih => ih.Order)
                    .WithMany()
                    .HasForeignKey(ih => ih.OrderId)
                    .IsRequired(false);
            });

        }
    }
    }