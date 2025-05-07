using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Api.Database.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Id).HasMaxLength(500);
        
        builder.Property(u => u.Email).HasMaxLength(200);
        builder.Property(u => u.IdentityId).HasMaxLength(50);
        
        builder.Property(u => u.Name).HasMaxLength(100);
        
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.IdentityId).IsUnique();
    }
}