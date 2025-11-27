using Course_management.Dto;
using Course_management.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Course_management.Services
{
    public class PaystackService : IPaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public PaystackService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("Paystack");
            _secretKey = configuration["Paystack:SecretKey"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
        }

        public async Task<TransactionInitializeResponseDto> InitializeTransaction(TransactionInitializeRequestDto request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("transaction/initialize", content);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TransactionInitializeResponseDto>(responseStream);
        }

        public async Task<TransactionVerifyResponseDto> VerifyTransaction(string reference)
        {
            var response = await _httpClient.GetAsync($"transaction/verify/{reference}");
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TransactionVerifyResponseDto>(responseStream);
        }

        public bool VerifySignature(string signature, string body)
        {
            var hash = new HMACSHA512(Encoding.UTF8.GetBytes(_secretKey));
            var computedHash = hash.ComputeHash(Encoding.UTF8.GetBytes(body));
            var signatureBytes = StringToByteArray(signature);

            return CryptographicOperations.FixedTimeEquals(computedHash, signatureBytes);
        }

        private static byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = System.Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}