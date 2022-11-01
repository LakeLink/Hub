namespace LakeHub.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
public class PastebinModel : PageModel
{
    [BindProperty]
    public string TextValue { get; set; } = string.Empty;
    [BindProperty]
    public string Language { get; set; } = string.Empty;

}