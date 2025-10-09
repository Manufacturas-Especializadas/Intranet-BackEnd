using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Intranet.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Departments> Departments { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Departments>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Departme__3214EC07D5C314A8");

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC070966A443");

            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC075B704024");

            entity.HasIndex(e => new { e.Name, e.PayRollNumber }, "UQ_Users_Name_PayRollNumber").IsUnique();

            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(70)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnName("passwordHash");
            entity.Property(e => e.PayRollNumber).HasColumnName("payRollNumber");
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(256)
                .HasColumnName("refreshToken");
            entity.Property(e => e.RefreshTokenExpiryTime).HasColumnName("refreshTokenExpiryTime");

            entity.HasOne(d => d.IdDepartmentNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.IdDepartment)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Users__IdDepartm__4CA06362");

            entity.HasOne(d => d.IdRoleNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.IdRole)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Users__IdRole__4D94879B");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}