
using System;
using System.ComponentModel.DataAnnotations;

namespace FullVoucherSystem.Models
{
    public class IgrFundedCapitalBudget
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string ClassificationCode { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "999999999999999.99")]
        public decimal BudgetAmount { get; set; }

        public DateTime DateUpdated { get; set; } = DateTime.Now;
    }
}
