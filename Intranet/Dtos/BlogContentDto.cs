﻿namespace Intranet.Dtos
{
    public class BlogContentDto
    {
        public string Title { get; set; }

        public string Content { get; set; }

        public string SubTitle { get; set; }

        public string Description { get; set; }

        public string Template { get; set; }

        public string PageType { get; set; }

        public IFormFile? Img { get; set; }

    }
}