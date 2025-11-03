using System;
using System.Collections.Generic;

namespace Intranet.Models;

public partial class InternalNews
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public string Img { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? IdUser { get; set; }

    public virtual Users IdUserNavigation { get; set; }
}