using System;
using System.Collections.Generic;

namespace Intranet.Models;

public partial class Users
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int? IdDepartment { get; set; }

    public int? IdRole { get; set; }

    public string Email { get; set; } = null!;

    public int? PayRollNumber { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public virtual Departments? IdDepartmentNavigation { get; set; }

    public virtual Roles? IdRoleNavigation { get; set; }

    public virtual ICollection<InternalNews?> InternalNews { get; set; } = new List<InternalNews?>();
}