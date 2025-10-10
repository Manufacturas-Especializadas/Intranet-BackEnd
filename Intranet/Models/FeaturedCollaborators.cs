using System;
using System.Collections.Generic;

namespace Intranet.Models;

public partial class FeaturedCollaborators
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Testimonial { get; set; }

    public string Photo { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? IdUser { get; set; }

    public virtual Users IdUserNavigation { get; set; }
}