using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FullVoucherSystem.Models
{
    public class FinalLedger
    {
        [Key]
        public int Id { get; set; }

        public string VoucherNumber { get; set; }

        public string CheckedLocation { get; set; }

        public string Station { get; set; }

        public string Head { get; set; }

        public string SubHead { get; set; }

        public string ClassificationCode { get; set; }

        public string SourceClassificationCode { get; set; }

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        public string Payee { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

        public decimal? FGN_WHT { get; set; }

        public decimal? EDG_WHT { get; set; }

        public decimal? VAT { get; set; }

        public decimal? StampDuty { get; set; }

        public decimal? TotalAmount { get; set; }

        public string QueryText { get; set; }

        public string Voucher_FirstName { get; set; }

        public string Voucher_Surname { get; set; }

        public string Voucher_Designation { get; set; }

        public string Bill_FirstName { get; set; }

        public string Bill_Surname { get; set; }

        public string Bill_Designation { get; set; }

        public DateTime? CertifiedDate { get; set; }
       // public DateTime? ProcessedDate { get; set; }
       
       // [NotMapped]
        public DateTime? ProcessedDate { get; set; }
        public string? Batch_No { get; set; }

        public string FundingAccount { get; set; }
    }
}