using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;

namespace Cas2Discourse.Services
{
    public class DiscourseSsoProtocol
    {
        private readonly string _secret;
        public DiscourseSsoProtocol(IConfiguration config)
        {
            _secret = config["DiscourseSecret"];
        }

        public string Sign(string sso, string sig, Dictionary<string, string?> identity)
        {
            using HMACSHA256 hmac = new(Encoding.ASCII.GetBytes(_secret));

            byte[] computedHash = hmac.ComputeHash(Encoding.ASCII.GetBytes(sso!));
            if (Convert.ToHexString(computedHash) != sig!.ToUpper())
            {
                throw new ArgumentException("Wrong signature from Discourse: " + sig);
            }

            string inPayload = Encoding.ASCII.GetString(Convert.FromBase64String(sso!));
            var dict = QueryHelpers.ParseQuery(inPayload);

            identity.Add("nonce", dict["nonce"]);

            string outPayload = QueryString.Create(identity).ToString()[1..]; // remove leading '?'
            outPayload = Convert.ToBase64String(Encoding.ASCII.GetBytes(outPayload));

            byte[] outHash = hmac.ComputeHash(Encoding.ASCII.GetBytes(outPayload));
            string outSig = Convert.ToHexString(outHash).ToLower();

            return QueryHelpers.AddQueryString(dict["return_sso_url"], new Dictionary<string, string?>()
            {
                { "sso", outPayload },
                { "sig", outSig }
            });
        }
    }
}
