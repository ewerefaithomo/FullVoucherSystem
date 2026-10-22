
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FullVoucherSystem.Models
{
    public class IgrFundedCapitalBudgetViewModel
    {
        public List<IgrFundedCapitalBudgetRow> Items { get; set; }
            = new List<IgrFundedCapitalBudgetRow>();
    }

    public class IgrFundedCapitalBudgetRow
    {
        [Required]
        public string ClassificationCode { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "999999999999999.99",
            ErrorMessage = "Enter a valid non-negative budget amount.")]
        public decimal? BudgetAmount { get; set; }
    }
}
