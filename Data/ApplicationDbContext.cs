using InventoryApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<InventoryTag> InventoryTags => Set<InventoryTag>();
        public DbSet<InventoryField> InventoryFields => Set<InventoryField>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<ItemField> ItemFields => Set<ItemField>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Unique tag names
            b.Entity<Tag>()
             .HasIndex(t => t.Name)
             .IsUnique();

            // Many-to-many Inventory <-> Tag via InventoryTag
            b.Entity<InventoryTag>()
             .HasKey(x => new { x.InventoryId, x.TagId });

            b.Entity<InventoryTag>()
             .HasOne(it => it.Inventory)
             .WithMany(i => i.InventoryTags)
             .HasForeignKey(it => it.InventoryId)
             .OnDelete(DeleteBehavior.Cascade);

            b.Entity<InventoryTag>()
             .HasOne(it => it.Tag)
             .WithMany(t => t.InventoryTags)
             .HasForeignKey(it => it.TagId)
             .OnDelete(DeleteBehavior.Cascade);

            // Inventory -> Fields (cascade)
            b.Entity<Inventory>()
             .HasMany(i => i.Fields)
             .WithOne(f => f.Inventory)
             .HasForeignKey(f => f.InventoryId)
             .OnDelete(DeleteBehavior.Cascade);

            // Inventory -> Items (cascade)
            b.Entity<Inventory>()
             .HasMany(i => i.Items)
             .WithOne(it => it.Inventory)
             .HasForeignKey(it => it.InventoryId)
             .OnDelete(DeleteBehavior.Cascade);

            // Item -> ItemFields (cascade)
            b.Entity<Item>()
             .HasMany(i => i.Values)
             .WithOne(v => v.Item)
             .HasForeignKey(v => v.ItemId)
             .OnDelete(DeleteBehavior.Cascade);

            // ItemField -> InventoryField (restrict: field must exist)
            b.Entity<ItemField>()
             .HasOne(v => v.Field)
             .WithMany()
             .HasForeignKey(v => v.FieldId)
             .OnDelete(DeleteBehavior.Restrict);

            // Optional: unique field names per inventory
            b.Entity<InventoryField>()
             .HasIndex(f => new { f.InventoryId, f.FieldName })
             .IsUnique();
        }
    }
}
