using System.ComponentModel.DataAnnotations;

namespace Kharasana.Web.ViewModels.Orders
{
    public class AssignDriverViewModel
    {
        [Required(ErrorMessage = "يرجى اختيار السائق")]
        public int DriverId { get; set; }

        public string? TruckPlate { get; set; } // ✅ اختياري
    }
}