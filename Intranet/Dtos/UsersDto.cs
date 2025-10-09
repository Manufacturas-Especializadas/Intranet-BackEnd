namespace Intranet.Dtos
{
    public class UsersDto
    {
        public string? Email { get; set; }

        public int? TelephoneExtension { get; set; }

        public string WorkPosition { get; set; } = null!;

        public string OfficeLocation { get; set; } = null!;
    }
}