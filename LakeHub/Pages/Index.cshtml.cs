using LakeHub.Options;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

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
