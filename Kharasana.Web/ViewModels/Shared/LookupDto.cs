namespace Kharasana.Web.ViewModels.Shared
{
    public class LookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? FactoryId { get; set; }
        public decimal? UnitPrice { get; set; }
    }
}
