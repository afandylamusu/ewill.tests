using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UnitTest.Services.Membership
{
    public class MemberRegistrationTest
    {
        const string DEV_MODE = "DEVTEST";

        public string IdentityApiUrl => DEV_MODE == "DEVTEST" ? "http://11.11.5.146:5540/api" : "";

        public object EWillClientId => "mobile.ios";
        public object EWillClientKey => "Xv5LpK8K15";

        public string StdPassword => "Standar123.";

        static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private void GenHeadersWithBasicAuth(HttpClient client)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Base64Encode($"{EWillClientId}:{EWillClientKey}"));
            client.DefaultRequestHeaders.Add("x-agent", "mobile");
            client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Add("x-version", "1.0");
        }

        public static string GetUniqueKey(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        [Fact(DisplayName = "Registration-001 : Sign up successfull")]
        public async Task Registration001()
        {

            var data = new RegistrationForm {
                Username = GetUniqueKey(10),
                Email = "leonardi.jordan@gmail.com",
                Password = StdPassword,
                Pin = 132456,
                Country = "Indonesia",
                CountryCode = "IDN",
                MobileNumber = "+6289613773993",
                SecurityQuestion = "Who is your President?",
                SecurityAnswer = "Jokowi"
            };


            StandardApiResponse<bool> result = null;
            StandardApiResponse<bool> expectedResult = new StandardApiResponse<bool>(true, "1.0", true);


            using (var client = new HttpClient())
            {
                GenHeadersWithBasicAuth(client);

                var response = await client.PostAsync($"{IdentityApiUrl}/account/register", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    result = JsonConvert.DeserializeObject<StandardApiResponse<bool>>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.BadRequest:
                            Assert.False(true, response.ReasonPhrase + await response.Content.ReadAsStringAsync());
                            break;

                        case System.Net.HttpStatusCode.InternalServerError:
                            Assert.False(true, "Internal code error - please check the source code" + await response.Content.ReadAsStringAsync());
                            break;
                    }
                }
                    
            }

            Assert.Equal(expectedResult.Success, result.Success);
            Assert.Equal(expectedResult.Result, result.Result);
        }

        [Fact(DisplayName = "Registration-002 : Failed to sign up because username already used")]
        public async Task Registration002()
        {
            var data = new RegistrationForm
            {
                Username = "jordan",
                Email = "leonardi.jordan@gmail.com",
                Password = StdPassword,
                Pin = 132456,
                Country = "Indonesia",
                CountryCode = "IDN",
                MobileNumber = "+6289613773993",
                SecurityQuestion = "Who is your President?",
                SecurityAnswer = "Jokowi"
            };


            StandardApiResponse<bool> result = null;
            StandardApiResponse<bool> expectedResult = new StandardApiResponse<bool>(false, "1.0", false);


            using (var client = new HttpClient())
            {
                GenHeadersWithBasicAuth(client);

                var response = await client.PostAsync($"{IdentityApiUrl}/account/register", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    result = JsonConvert.DeserializeObject<StandardApiResponse<bool>>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.BadRequest:
                            Assert.False(true, response.ReasonPhrase + await response.Content.ReadAsStringAsync());
                            break;

                        case System.Net.HttpStatusCode.InternalServerError:
                            Assert.False(true, "Internal code error - please check the source code" + await response.Content.ReadAsStringAsync());
                            break;
                    }
                }

            }

            Assert.Equal(expectedResult.Success, result.Success);
            Assert.Equal(expectedResult.Result, result.Result);
        }


    }
}
