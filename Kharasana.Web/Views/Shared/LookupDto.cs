namespace Kharasana.Web.ViewModels.Shared
{
    public class LookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? FactoryId { get; set; }  // ✅ أضف هذه الخاصية
        public decimal? UnitPrice { get; set; } // ✅ سعر الوحدة (اختياري)
    }
}