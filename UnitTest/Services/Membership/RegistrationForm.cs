namespace UnitTest.Services.Membership
{
    internal class RegistrationForm
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int Pin { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string SecurityQuestion { get; set; }
        public string SecurityAnswer { get; set; }
        public string MobileNumber { get; internal set; }
    }
}