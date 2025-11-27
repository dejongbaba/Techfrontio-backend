using System;
using System.Collections.Generic;

namespace Course_management.Dto
{
    public class TransactionVerifyResponseDto
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public VerifyData Data { get; set; }

        public class VerifyData
        {
            public long Amount { get; set; }
            public string Currency { get; set; }
            public string Status { get; set; }
            public string Reference { get; set; }
            public CustomerData Customer { get; set; }
            public Dictionary<string, object> Metadata { get; set; }
        }

        public class CustomerData
        {
            public string Email { get; set; }
        }
    }
}