using System;
using System.Collections.Generic;

namespace Intranet.Models;

public partial class BlogContent
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string SubTitle { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Content { get; set; }

    public string Template { get; set; }

    public string? Img { get; set; } = null!;

    public int? IdUser { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string PageType { get; set; }

    public virtual Users? IdUserNavigation { get; set; }
}