namespace Kharasana.Web.Models.Settings
{
    /// <summary>
    /// عرض بيانات مصنع موظف المصنع الحالي (للقراءة فقط) + شعار المصنع القابل للتعديل.
    /// </summary>
    public class FactorySettingsViewModel
    {
        public int FactoryId { get; set; }
        public string FactoryName { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public string? Phone { get; set; }
        public string Area { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public bool IsActive { get; set; }
    }
}