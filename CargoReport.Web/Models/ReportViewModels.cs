using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CargoReport.Web.Models
{
    public class Report1ViewModel
    {
        public string GsaCode { get; set; }
        public string GsaName { get; set; }
        public string AgentCode { get; set; }
        public string AgentName { get; set; }
        public decimal Amount { get; set; }
        public string BankName { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string RequestBy { get; set; }
        public DateTime RequestOn { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime ApprovedOn { get; set; }
        public string ApprovedRemarks { get; set; }
        public string PaymentReferenceNo { get; set; }
        public string PaymentRefId { get; set; }
    }
}