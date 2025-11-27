namespace Course_management.Dto
{
    public class TransactionInitializeResponseDto
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public TransactionData Data { get; set; }

        public class TransactionData
        {
            public string Authorization_url { get; set; }
            public string Access_code { get; set; }
            public string Reference { get; set; }
        }
    }
}