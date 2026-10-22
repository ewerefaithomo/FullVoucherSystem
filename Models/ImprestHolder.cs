using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FullVoucherSystem.Models
{
    [Table("Imprest_Holder")]
    public class ImprestHolder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Disables SQL Identity auto-incrementing
        [Required(ErrorMessage = "Folio Number is required.")]
        [Display(Name = "Folio Number")]
        public string FolioNumber { get; set; }

        [Required(ErrorMessage = "Staff ID is required.")]
        [Display(Name = "Staff ID")]
        public string StaffId { get; set; }

        [Required(ErrorMessage = "Staff Name is required.")]
        [Display(Name = "Staff Names")]
        public string StaffNames { get; set; } // Ensure this matches asp-for="StaffNames"

        public string Designation { get; set; }

        public string Department { get; set; }

        public string Bank { get; set; }

        public string AccountName { get; set; }

        public string AccountNumber { get; set; }

        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }
    }
}