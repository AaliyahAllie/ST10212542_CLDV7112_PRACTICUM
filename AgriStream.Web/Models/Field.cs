namespace AgriStream.Web.Models;

public class Field
{
    public int FieldId { get; set; }
    public int FarmId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string CropType { get; set; } = string.Empty;
    public string? SoilType { get; set; }
    public decimal AreaHectares { get; set; }
    public string Status { get; set; } = "Active";

    public Farm? Farm { get; set; }
    public ICollection<IoTDevice> IoTDevices { get; set; } = new List<IoTDevice>();
    public ICollection<DroneMedia> DroneMediaItems { get; set; } = new List<DroneMedia>();
}
