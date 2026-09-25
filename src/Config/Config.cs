namespace Config
{
    public class AppSettings
    {
        public string Currency {get; set;} ="";
        public string[] Categories {get; set;} = [];
        public JWT JWTSettings {get; set;} = new();
    }
    public class JWT
    {
        public string Issuer {get; set;} ="";
        public string Audience {get; set;} ="";
        public string KEY {get; set;} ="";
    }
}