
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FullVoucherSystem.Models
{
    [Table("StaffAdvanceBudget", Schema = "dbo")]
    public class StaffAdvanceBudget
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        public string Payee { get; set; } = string.Empty;

        [StringLength(100)]
        public string? VoteRefNo { get; set; } = "VRN";

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BudgetAmount { get; set; } = 0;

        public DateTime DateUpdated { get; set; }
    }
}
