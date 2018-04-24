using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace UnitTest.Services.Membership
{
    public class MemberRegistrationTest
    {
        const string DEV_MODE = "DEVTEST";

        public string IdentityApiUrl => DEV_MODE == "DEVTEST" ? "http://11.11.5.146:5540/api" : "";
        public object EWillClientId => "mobile.ios";
        public object EWillClientKey => "Xv5LpK8K15";

        public string StdPassword => "Standar123.";
        const string UserIdGlobal = "UnitTest2";

        static string getTokenValidation(string userID) {

            string tokenRegis = "";
            string tokenFromJson = "";
           
            
            using (SqlConnection conn = new SqlConnection("Data Source = 11.11.1.32,1433 ; Initial Catalog = EWILLDb.Notification.Dev; User ID = app-admin; password = Standar123;")) {
                conn.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT content from EmailTrack where CreatedBy = '"+userID+"' ", conn)) {
                     
                        command.CommandType = System.Data.CommandType.Text;
                        command.ExecuteNonQuery();

                        try
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read()) {
                                    tokenRegis = reader.GetString(reader.GetOrdinal("content"));
                                    JObject result = JObject.Parse(tokenRegis);
                                    tokenFromJson = (string)result["UrlConfirmation"];
                                    tokenRegis = tokenFromJson.Replace("http://11.11.5.146:5540/mobi/redirect?token=", "");
                                    tokenRegis = tokenRegis.Replace("&userName=", "");
                                    tokenRegis = tokenRegis.Replace(UserIdGlobal, "");
                                    tokenRegis = Uri.UnescapeDataString(tokenRegis);
                                    return tokenRegis;
                                }
                                return tokenRegis;
                            }
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        
                    }
                    
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return tokenFromJson;
            
        }

        static string getTokenOTPValidation(string phoneNumber)
        {

            string tokenRegis = "";
            string tokenFromJson = "";


            using (SqlConnection conn = new SqlConnection("Data Source = 11.11.1.32,1433 ; Initial Catalog = EWILLDb.Notification.Dev; User ID = app-admin; password = Standar123;"))
            {
                conn.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT top 1 Content from SMSTrack where Content like '%" + phoneNumber + "%' order by CreatedUtc desc", conn))
                    {

                        command.CommandType = System.Data.CommandType.Text;
                        command.ExecuteNonQuery();

                        try
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    tokenRegis = reader.GetString(reader.GetOrdinal("content"));
                                    JObject result = JObject.Parse(tokenRegis);
                                    tokenRegis = (string)result["Token"];
                                    return tokenRegis;
                                }
                                return tokenRegis;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return tokenFromJson;
        }

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
                Username = UserIdGlobal,
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

        [Fact(DisplayName = "Registration-003 : Validation Email successfull ")]
        public async Task Registration003() {

            var data = new ValidationEmailForm
            {
                userName = UserIdGlobal,
                token = getTokenValidation(UserIdGlobal)
            };

            StandardApiResponse<bool> result = null;
            StandardApiResponse<bool> expectedResult = new StandardApiResponse<bool>(true, "1.0", true);

            using (var client = new HttpClient())
            {
                GenHeadersWithBasicAuth(client);

                var response = await client.PostAsync($"{IdentityApiUrl}/account/confirm-email", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
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

       [Fact(DisplayName = "Registration-004 : Validation email username falied")]
        public async Task Registration004() {
            var data = new ValidationEmailForm
            {
                userName = "UnitTest1234",
                token = getTokenValidation(UserIdGlobal)
            };

            StandardApiResponse<bool> result = null;
            StandardApiResponse<bool> expectedResult = new StandardApiResponse<bool>(false, "1.0", false);

            using (var client = new HttpClient())
            {
                GenHeadersWithBasicAuth(client);

                var response = await client.PostAsync($"{IdentityApiUrl}/account/confirm-email", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
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

        [Fact(DisplayName = "Registration-005 : Validation email token falied")]
        public async Task Registration005()
        {
            var data = new ValidationEmailForm
            {
                userName = "UnitTest123",
                token = "OASUHDALSHDIUASHDOUIHASUIDHASUID"
            };

            StandardApiResponse<bool> result = null;
            StandardApiResponse<bool> expectedResult = new StandardApiResponse<bool>(false, "1.0", false);

            using (var client = new HttpClient())
            {
                GenHeadersWithBasicAuth(client);

                var response = await client.PostAsync($"{IdentityApiUrl}/account/confirm-email", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
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

        [Fact(DisplayName = "Registration-006 : Validation OTP token successfully")]
        public async Task Registration006()
        {
            var data = new ValidationEmailForm
            {
                userName = UserIdGlobal,
                token = getTokenOTPValidation("+6289613773993")
            };

            StandardApiResponse<bool> result = null;
            StandardApiResponse<bool> expectedResult = new StandardApiResponse<bool>(true, "1.0", true);

            using (var client = new HttpClient())
            {
                GenHeadersWithBasicAuth(client);

                var response = await client.PostAsync($"{IdentityApiUrl}/account/confirm-phone-number", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
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

        [Fact(DisplayName = "Registration-007 : Failed to confirm phone number (invalid username)")]
        public async Task Registration007() {
            var data = new ValidationEmailForm
            {
                userName = "UnitTest1234",
                token = getTokenOTPValidation("+6289613773993")
            };

            StandardApiResponse<bool> result = null;
            StandardApiResponse<bool> expectedResult = new StandardApiResponse<bool>(false, "1.0", false);

            using (var client = new HttpClient())
            {
                GenHeadersWithBasicAuth(client);

                var response = await client.PostAsync($"{IdentityApiUrl}/account/confirm-phone-number", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
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

        [Fact(DisplayName = "Registration-008 : Failed to confirm phone number (invalid token)")]
        public async Task Registration008()
        {
            var data = new ValidationEmailForm
            {
                userName = UserIdGlobal,
                token = "0000"
            };

            StandardApiResponse<bool> result = null;
            StandardApiResponse<bool> expectedResult = new StandardApiResponse<bool>(false, "1.0", false);

            using (var client = new HttpClient())
            {
                GenHeadersWithBasicAuth(client);

                var response = await client.PostAsync($"{IdentityApiUrl}/account/confirm-phone-number", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
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

        [Fact(DisplayName = "Login-001 : Login Successfully")]
        public async Task Login001()
        {
            var data = new LoginForm
            {
                userName = UserIdGlobal,
                password = "Standar123."
            };

            StandardApiResponse<bool> result = null;
            StandardApiResponse<bool> expectedResult = new StandardApiResponse<bool>(true, "1.0", true);

            using (var client = new HttpClient())
            {
                GenHeadersWithBasicAuth(client);

                var response = await client.PostAsync($"{IdentityApiUrl}/account/login", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
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
