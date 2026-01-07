using System;
using System.Collections.Generic;

namespace Intranet.Models;

public partial class BlogMedia
{
    public int Id { get; set; }

    public string Url { get; set; }

    public string MediaType { get; set; }

    public int BlogContentId { get; set; }

    public virtual BlogContent BlogContent { get; set; }
}