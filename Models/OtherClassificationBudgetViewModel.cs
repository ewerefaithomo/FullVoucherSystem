
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FullVoucherSystem.Models
{
    public class OtherClassificationBudgetViewModel
    {
        public List<OtherClassificationBudgetRow> Items { get; set; }
            = new List<OtherClassificationBudgetRow>();
    }

    public class OtherClassificationBudgetRow
    {
        [Required]
        public string ClassificationCode { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "999999999999999.99",
            ErrorMessage = "Enter a valid, non-negative budget amount.")]
        public decimal? BudgetAmount { get; set; }
    }
}
