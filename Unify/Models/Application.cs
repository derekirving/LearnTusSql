namespace Unify.Models
{
    public class Application
    {
        public string Id { get; set; }
        public string MasterKey { get; set; }
        public RsaKeys RsaKeys { get; set; }
    }
}