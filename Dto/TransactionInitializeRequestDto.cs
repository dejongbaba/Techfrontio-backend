using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Course_management.Dto
{
    public class TransactionInitializeRequestDto
    {
        public string Email { get; set; }
        public string Amount { get; set; }
        public string Callback_url { get; set; }
        public string Subaccount { get; set; }
        public int Transaction_charge { get; set; }
        public string Bearer { get; set; } = "subaccount";
        public Dictionary<string, object> Metadata { get; set; }
    }
}