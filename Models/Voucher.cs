using System;

namespace FullVoucherSystem.Models
{
    public class Voucher
    {
        public int Id { get; set; }

        // NEW
        public string VoucherType { get; set; }

        public string VoucherNumber { get; set; }

        public string CheckedLocation { get; set; }

        public string Station { get; set; }

        public string AccountCode { get; set; }

        public string Head { get; set; }

        public string SubHead { get; set; }

        public string ClassificationCode { get; set; }

        public string SourceClassificationCode { get; set; }

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        public string Payee { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

       
        public decimal FGN_WHT { get; set; }

        public decimal EDG_WHT { get; set; }

        public decimal StampDuty { get; set; }

        public decimal VAT { get; set; }

        public decimal TotalAmount { get; set; }


        // PREPARER
        public string Voucher_Surname { get; set; }

        public string Voucher_FirstName { get; set; }

        public string Voucher_Designation { get; set; }




     
       
       
        
        
       
   
        
        

        // New properties to store selected percentage strings for editing persistence
        public string VatOption { get; set; }
        public string FgnWhtOption { get; set; }
        public string EdgWhtOption { get; set; }
        public string StampOption { get; set; }

        public decimal OriginalInputAmount { get; set; }
        public bool IsVatInclusive { get; set; }
       
    }
}
