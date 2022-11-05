using System.ComponentModel.DataAnnotations;

namespace LakeHub.Areas.Auth.Models
{
    public class CasEmailForm
    {
        public bool VerifyCodeSent { get; set; } = false;

        [DataType(DataType.EmailAddress)]
        public string InputEmail { get; set; } = string.Empty;

        public int? InputVerifyCode { get; set; }
    }
}
