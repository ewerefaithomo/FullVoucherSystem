using System;
using System.ComponentModel.DataAnnotations;

namespace FullVoucherSystem.Models
{
    public class QueryInformation
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; }
        public string QueryText { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
