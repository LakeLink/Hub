namespace LakeHub.Options;

public class IndexLink
{
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImgPath { get; set; } = string.Empty;
    public bool NeedCAS { get; set; } = false;
    public bool NeedOAuth2 { get; set; } = false;
}