using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LakeHub.Areas.Auth.Models
{
    public class CasSignInForm
    {
        public string InputCASId { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string InputPassword { get; set; } = string.Empty;

        [Display(Name = "Trust this device")]
        public bool TrustThisDevice { get; set; } = true;
    }
}
