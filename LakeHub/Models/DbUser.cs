using System.ComponentModel.DataAnnotations;

namespace LakeHub.Models;

public class DbUser
{
    //https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types
    public DbUser(string casId, string name)
    {
        CasId = casId;
        Name = name;
    }

    [Key]
    public string CasId { get; set; }

    public string Name { get; set; }

    public int OrgId { get; set; }

    public string? Org { get; set; }

    public string? Identity { get; set; }

    public bool EmailVerified { get; set; }

    public string? Email { get; set; }

    public string? IPAtEmailVerify { get; set; }
}