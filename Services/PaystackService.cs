using Course_management.Dto;
using Course_management.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            async Task<HttpResponseMessage> PostInitialize(TransactionInitializeRequestDto payload)
            {
                var json = JsonSerializer.Serialize(payload, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                return await _httpClient.PostAsync("transaction/initialize", content);
            }

            var response = await PostInitialize(request);
            if (response.StatusCode == HttpStatusCode.NotFound && !string.IsNullOrWhiteSpace(request.Subaccount))
            {
                var fallbackRequest = new TransactionInitializeRequestDto
                {
                    Email = request.Email,
                    Amount = request.Amount,
                    CallbackUrl = request.CallbackUrl,
                    Metadata = request.Metadata
                };
                response = await PostInitialize(fallbackRequest);
            }
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Paystack initialize failed: {(int)response.StatusCode} {response.ReasonPhrase}. {responseBody}");
            }

            var responseStream = await response.Content.ReadAsStreamAsync();
            var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await JsonSerializer.DeserializeAsync<TransactionInitializeResponseDto>(responseStream, deserializeOptions);
        }

        public async Task<TransactionVerifyResponseDto> VerifyTransaction(string reference)
        {
            var response = await _httpClient.GetAsync($"transaction/verify/{reference}");
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await JsonSerializer.DeserializeAsync<TransactionVerifyResponseDto>(responseStream, options);
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
