using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using LakeHub.Options;

namespace LakeHub.Pages;

public class IndexModel : PageModel
{
    public IndexLink[] Links;
    public IndexModel(IOptionsMonitor<IndexOptions> options)
    {
        Links = options.CurrentValue.Links!;
    }
    public void OnGet()
    {
    }
}
