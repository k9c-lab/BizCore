using BizCore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Data;

public class AccountingDbContext : DbContext
{
    public AccountingDbContext(DbContextOptions<AccountingDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Salesperson> Salespersons => Set<Salesperson>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();
    public DbSet<QuotationHeader> QuotationHeaders => Set<QuotationHeader>();
    public DbSet<QuotationDetail> QuotationDetails => Set<QuotationDetail>();
    public DbSet<InvoiceHeader> InvoiceHeaders => Set<InvoiceHeader>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();
    public DbSet<InvoiceSerial> InvoiceSerials => Set<InvoiceSerial>();
    public DbSet<PaymentHeader> PaymentHeaders => Set<PaymentHeader>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<ReceiptHeader> ReceiptHeaders => Set<ReceiptHeader>();
    public DbSet<PurchaseOrderHeader> PurchaseOrderHeaders => Set<PurchaseOrderHeader>();
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails => Set<PurchaseOrderDetail>();
    public DbSet<ReceivingHeader> ReceivingHeaders => Set<ReceivingHeader>();
    public DbSet<ReceivingDetail> ReceivingDetails => Set<ReceivingDetail>();
    public DbSet<ReceivingSerial> ReceivingSerials => Set<ReceivingSerial>();
    public DbSet<SerialClaimLog> SerialClaimLogs => Set<SerialClaimLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.PasswordHash).HasMaxLength(300);
            entity.Property(x => x.Role).HasMaxLength(30);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(x => x.CreditLimit).HasPrecision(18, 2);
            entity.HasIndex(x => x.CustomerCode).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(x => x.CreditLimit).HasPrecision(18, 2);
            entity.HasIndex(x => x.SupplierCode).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        });

        modelBuilder.Entity<Salesperson>(entity =>
        {
            entity.Property(x => x.CommissionRate).HasPrecision(5, 2);
            entity.HasIndex(x => x.SalespersonCode).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.CurrentStock).HasPrecision(18, 2);
            entity.HasIndex(x => x.ItemCode).IsUnique();
            entity.HasIndex(x => x.PartNumber).IsUnique();
        });

        modelBuilder.Entity<SerialNumber>(entity =>
        {
            entity.HasKey(x => x.SerialId);
            entity.HasIndex(x => x.SerialNo).IsUnique();
            entity.HasOne(x => x.Item)
                .WithMany(x => x.SerialNumbers)
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CurrentCustomer)
                .WithMany()
                .HasForeignKey(x => x.CurrentCustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InvoiceHeader)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuotationHeader>(entity =>
        {
            entity.Property(x => x.ReferenceNo).HasMaxLength(50);
            entity.Property(x => x.DiscountMode).HasMaxLength(10);
            entity.Property(x => x.HeaderDiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.VatType).HasMaxLength(10);
            entity.Property(x => x.VatAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.QuotationNumber).IsUnique();
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Salesperson)
                .WithMany()
                .HasForeignKey(x => x.SalespersonId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ConvertedByUser)
                .WithMany()
                .HasForeignKey(x => x.ConvertedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuotationDetail>(entity =>
        {
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.LineTotal).HasPrecision(18, 2);
            entity.HasOne(x => x.QuotationHeader)
                .WithMany(x => x.QuotationDetails)
                .HasForeignKey(x => x.QuotationHeaderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceHeader>(entity =>
        {
            entity.HasKey(x => x.InvoiceId);
            entity.Property(x => x.ReferenceNo).HasMaxLength(50);
            entity.Property(x => x.CancelReason).HasMaxLength(500);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.VatType).HasMaxLength(10);
            entity.Property(x => x.VatAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.PaidAmount).HasPrecision(18, 2);
            entity.Property(x => x.BalanceAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.InvoiceNo).IsUnique();
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Salesperson)
                .WithMany()
                .HasForeignKey(x => x.SalespersonId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Quotation)
                .WithMany()
                .HasForeignKey(x => x.QuotationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.IssuedByUser)
                .WithMany()
                .HasForeignKey(x => x.IssuedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CancelledByUser)
                .WithMany()
                .HasForeignKey(x => x.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.Property(x => x.Qty).HasPrecision(18, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.LineTotal).HasPrecision(18, 2);
            entity.HasOne(x => x.InvoiceHeader)
                .WithMany(x => x.InvoiceDetails)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceSerial>(entity =>
        {
            entity.HasKey(x => x.InvoiceSerialId);
            entity.HasIndex(x => new { x.InvoiceDetailId, x.SerialId }).IsUnique();
            entity.HasIndex(x => x.SerialId).IsUnique();
            entity.HasOne(x => x.InvoiceDetail)
                .WithMany(x => x.InvoiceSerials)
                .HasForeignKey(x => x.InvoiceDetailId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SerialNumber)
                .WithMany()
                .HasForeignKey(x => x.SerialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentHeader>(entity =>
        {
            entity.HasKey(x => x.PaymentId);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.CancelReason).HasMaxLength(500);
            entity.HasIndex(x => x.PaymentNo).IsUnique();
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PostedByUser)
                .WithMany()
                .HasForeignKey(x => x.PostedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CancelledByUser)
                .WithMany()
                .HasForeignKey(x => x.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReceiptHeader)
                .WithOne(x => x.PaymentHeader)
                .HasForeignKey<ReceiptHeader>(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentAllocation>(entity =>
        {
            entity.HasKey(x => x.PaymentAllocationId);
            entity.Property(x => x.AppliedAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.PaymentId, x.InvoiceId }).IsUnique();
            entity.HasOne(x => x.PaymentHeader)
                .WithMany(x => x.PaymentAllocations)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.InvoiceHeader)
                .WithMany(x => x.PaymentAllocations)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReceiptHeader>(entity =>
        {
            entity.HasKey(x => x.ReceiptId);
            entity.Property(x => x.TotalReceivedAmount).HasPrecision(18, 2);
            entity.Property(x => x.CancelReason).HasMaxLength(500);
            entity.HasIndex(x => x.ReceiptNo).IsUnique();
            entity.HasIndex(x => x.PaymentId).IsUnique();
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.IssuedByUser)
                .WithMany()
                .HasForeignKey(x => x.IssuedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CancelledByUser)
                .WithMany()
                .HasForeignKey(x => x.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderHeader>(entity =>
        {
            entity.HasKey(x => x.PurchaseOrderId);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.VatType).HasMaxLength(10);
            entity.Property(x => x.VatAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.CancelReason).HasMaxLength(500);
            entity.HasIndex(x => x.PONo).IsUnique();
            entity.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CancelledByUser)
                .WithMany()
                .HasForeignKey(x => x.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderDetail>(entity =>
        {
            entity.Property(x => x.Qty).HasPrecision(18, 2);
            entity.Property(x => x.ReceivedQty).HasPrecision(18, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.LineTotal).HasPrecision(18, 2);
            entity.HasOne(x => x.PurchaseOrderHeader)
                .WithMany(x => x.PurchaseOrderDetails)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReceivingHeader>(entity =>
        {
            entity.HasKey(x => x.ReceivingId);
            entity.HasIndex(x => x.ReceivingNo).IsUnique();
            entity.Property(x => x.CancelReason).HasMaxLength(500);
            entity.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseOrderHeader)
                .WithMany(x => x.Receivings)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PostedByUser)
                .WithMany()
                .HasForeignKey(x => x.PostedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CancelledByUser)
                .WithMany()
                .HasForeignKey(x => x.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReceivingDetail>(entity =>
        {
            entity.Property(x => x.QtyReceived).HasPrecision(18, 2);
            entity.HasOne(x => x.ReceivingHeader)
                .WithMany(x => x.ReceivingDetails)
                .HasForeignKey(x => x.ReceivingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PurchaseOrderDetail)
                .WithMany(x => x.ReceivingDetails)
                .HasForeignKey(x => x.PurchaseOrderDetailId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReceivingSerial>(entity =>
        {
            entity.HasIndex(x => x.SerialNo).IsUnique();
            entity.HasOne(x => x.ReceivingDetail)
                .WithMany(x => x.ReceivingSerials)
                .HasForeignKey(x => x.ReceivingDetailId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SerialClaimLog>(entity =>
        {
            entity.HasKey(x => x.SerialClaimLogId);
            entity.HasIndex(x => x.SerialId);
            entity.HasIndex(x => x.SupplierId);
            entity.Property(x => x.ClaimStatus).HasMaxLength(20);
            entity.HasOne(x => x.SerialNumber)
                .WithMany()
                .HasForeignKey(x => x.SerialId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
