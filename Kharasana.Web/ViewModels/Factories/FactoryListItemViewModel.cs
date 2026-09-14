namespace Kharasana.Web.ViewModels.Factories
{
    public class FactoryListItemViewModel
    {
        public int FactoryId { get; set; }
        public string FactoryName { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public string? Phone { get; set; }
        public string Area { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public bool IsActive { get; set; }
        public bool HasAccount { get; set; }
    }
}