using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Course_management.Dto
{
    public class TransactionInitializeRequestDto
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
        [JsonPropertyName("callback_url")]
        public string CallbackUrl { get; set; }
        [JsonPropertyName("subaccount")]
        public string Subaccount { get; set; }
        [JsonPropertyName("transaction_charge")]
        public int? TransactionCharge { get; set; }
        [JsonPropertyName("bearer")]
        public string Bearer { get; set; }
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; }
    }
}
