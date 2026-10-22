
using System.Collections.Generic;

namespace FullVoucherSystem.Models
{
    public class StaffAdvanceBudgetViewModel
    {
        public List<StaffAdvanceBudgetRow> Items { get; set; }
            = new List<StaffAdvanceBudgetRow>();
    }

    public class StaffAdvanceBudgetRow
    {
        public string Payee { get; set; } = string.Empty;

        public string? VoteRefNo { get; set; }

        public decimal? BudgetAmount { get; set; }
    }
}
