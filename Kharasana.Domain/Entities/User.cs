using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Enums;

namespace Kharasana.Domain.Entities;

public class User
{
    // Primary Key
    public int UserId { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    // User Information
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? ProfileImage { get; set; }

    // Role
    public UserRole Role { get; set; }

    // Driver Only
    public string? LicenseNumber { get; set; }
    public DriverStatus? DriverStatus { get; set; }

    // Employee & Driver
    public int? FactoryId { get; set; }

    // Status
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Brute-force protection
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }

    // Navigation Properties
    public Factory? Factory { get; set; }
    public ICollection<Order> ClientOrders { get; set; } = new List<Order>();
    public ICollection<Order> DriverOrders { get; set; } = new List<Order>();
}