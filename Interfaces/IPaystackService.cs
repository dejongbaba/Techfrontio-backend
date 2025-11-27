using Course_management.Dto;
using System.Threading.Tasks;

namespace Course_management.Interfaces
{
    public interface IPaystackService
    {
        Task<TransactionInitializeResponseDto> InitializeTransaction(TransactionInitializeRequestDto request);
        Task<TransactionVerifyResponseDto> VerifyTransaction(string reference);
        bool VerifySignature(string signature, string body);
    }
}