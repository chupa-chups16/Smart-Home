using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHome.Api.Models;

[Table("SensorData")]
public class SensorData
{
    [Key]
    [Column("data_id")]
    public int DataId { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; }

    [Column("value")]
    public float Value { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;
}