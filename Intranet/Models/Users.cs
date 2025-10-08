using System;
using System.Collections.Generic;

namespace Intranet.Models;

public partial class Users
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? IdDepartment { get; set; }

    public int? IdRole { get; set; }

    public string? Email { get; set; }

    public int? PayRollNumber { get; set; }

    public int? TelephoneExtension { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public string WorkPosition { get; set; } = null!;

    public string OfficeLocation { get; set; } = null!;

    public virtual Departments? IdDepartmentNavigation { get; set; }

    public virtual Roles? IdRoleNavigation { get; set; }
}