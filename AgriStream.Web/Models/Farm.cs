namespace AgriStream.Web.Models;

public class Farm
{
    public int FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal TotalHectares { get; set; }
    public int OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? OwnerUser { get; set; }
    public ICollection<Field> Fields { get; set; } = new List<Field>();
}
