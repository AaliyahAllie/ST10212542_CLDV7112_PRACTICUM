using Microsoft.EntityFrameworkCore;
using AgriStream.Web.Models;

namespace AgriStream.Web.Data;

public class AgriStreamDbContext : DbContext
{
    public AgriStreamDbContext(DbContextOptions<AgriStreamDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<IoTDevice> IoTDevices => Set<IoTDevice>();
    public DbSet<DroneMedia> DroneMedia => Set<DroneMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.RoleName).IsRequired().HasMaxLength(50);
            entity.HasIndex(r => r.RoleName).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Password).IsRequired().HasMaxLength(255);
            entity.Property(u => u.Email).HasMaxLength(150);

            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Farm configuration
        modelBuilder.Entity<Farm>(entity =>
        {
            entity.HasKey(f => f.FarmId);
            entity.Property(f => f.FarmName).IsRequired().HasMaxLength(150);
            entity.Property(f => f.Location).IsRequired().HasMaxLength(200);
            entity.Property(f => f.TotalHectares).HasColumnType("decimal(10,2)");

            entity.HasOne(f => f.OwnerUser)
                  .WithMany(u => u.ManagedFarms)
                  .HasForeignKey(f => f.OwnerUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Field configuration
        modelBuilder.Entity<Field>(entity =>
        {
            entity.HasKey(f => f.FieldId);
            entity.Property(f => f.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(f => f.CropType).IsRequired().HasMaxLength(100);
            entity.Property(f => f.SoilType).HasMaxLength(100);
            entity.Property(f => f.AreaHectares).HasColumnType("decimal(10,2)");

            entity.HasOne(f => f.Farm)
                  .WithMany(farm => farm.Fields)
                  .HasForeignKey(f => f.FarmId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // IoTDevice configuration
        modelBuilder.Entity<IoTDevice>(entity =>
        {
            entity.HasKey(d => d.DeviceId);
            entity.Property(d => d.DeviceSerialNumber).IsRequired().HasMaxLength(100);
            entity.HasIndex(d => d.DeviceSerialNumber).IsUnique();
            entity.Property(d => d.DeviceType).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Status).HasMaxLength(50);

            entity.HasOne(d => d.Field)
                  .WithMany(field => field.IoTDevices)
                  .HasForeignKey(d => d.FieldId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // DroneMedia configuration
        modelBuilder.Entity<DroneMedia>(entity =>
        {
            entity.HasKey(m => m.MediaId);
            entity.Property(m => m.FileName).IsRequired().HasMaxLength(255);
            entity.Property(m => m.BlobUrl).IsRequired().HasMaxLength(1000);
            entity.Property(m => m.ImageResolution).HasMaxLength(50);

            entity.HasOne(m => m.Field)
                  .WithMany(f => f.DroneMediaItems)
                  .HasForeignKey(m => m.FieldId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.UploadedByUser)
                  .WithMany(u => u.UploadedMedia)
                  .HasForeignKey(m => m.UploadedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed initial roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin", Description = "System administrator with full management access" },
            new Role { RoleId = 2, RoleName = "Farmer", Description = "Farm owner/manager monitoring telemetry and drone media" },
            new Role { RoleId = 3, RoleName = "Agronomist", Description = "Agricultural specialist analyzing soil and crop telemetry" }
        );

        // Seed test users
        modelBuilder.Entity<User>().HasData(
            new User { UserId = 1, Username = "admin_agristream", Password = "AdminPass123!", Email = "admin@agristream.com", RoleId = 1, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { UserId = 2, Username = "john_farmer", Password = "FarmerPass123!", Email = "john@greenacres.com", RoleId = 2, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { UserId = 3, Username = "dr_sarah_agro", Password = "AgroPass123!", Email = "sarah@croptech.org", RoleId = 3, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // Seed sample farm
        modelBuilder.Entity<Farm>().HasData(
            new Farm { FarmId = 1, FarmName = "Green Valley Agricultural Hub", Location = "Free State, South Africa", TotalHectares = 1250.50m, OwnerUserId = 2, CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc) }
        );

        // Seed sample fields
        modelBuilder.Entity<Field>().HasData(
            new Field { FieldId = 1, FarmId = 1, FieldName = "North Parcel Alpha", CropType = "Maize", SoilType = "Sandy Loam", AreaHectares = 450.00m, Status = "Active" },
            new Field { FieldId = 2, FarmId = 1, FieldName = "South Parcel Beta", CropType = "Soybeans", SoilType = "Clay Loam", AreaHectares = 320.00m, Status = "Active" }
        );

        // Seed sample IoT devices
        modelBuilder.Entity<IoTDevice>().HasData(
            new IoTDevice { DeviceId = 1, DeviceSerialNumber = "SN-SOIL-001", DeviceType = "Multi-Depth Soil Probe", FieldId = 1, Status = "Online", BatteryLevelPercentage = 94, InstallationDate = new DateTime(2026, 1, 20) },
            new IoTDevice { DeviceId = 2, DeviceSerialNumber = "SN-WTH-002", DeviceType = "Solar Weather Station", FieldId = 1, Status = "Online", BatteryLevelPercentage = 98, InstallationDate = new DateTime(2026, 2, 5) },
            new IoTDevice { DeviceId = 3, DeviceSerialNumber = "SN-SOIL-003", DeviceType = "Multi-Depth Soil Probe", FieldId = 2, Status = "Online", BatteryLevelPercentage = 88, InstallationDate = new DateTime(2026, 2, 12) }
        );
    }
}
