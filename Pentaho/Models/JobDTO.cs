using System;

public class JobDTO
{
    public string? Name { get; set; }
    public int? RunStatusId { get; set; }
    public DateTime? LastExecution { get; set; }
    public string? LastOutcomeMessage { get; set; }
    public DateTime? EndDateTime { get; set; }
}
