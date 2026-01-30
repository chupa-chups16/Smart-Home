public class AutomationRule
{
    public int Id { get; set; }
    public string Condition { get; set; } = null!;
    public string Action { get; set; } = null!;
    public bool IsActive { get; set; }
}
