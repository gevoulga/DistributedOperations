namespace Distops.Core.Test.Samples;

public record DistopDto
{
    public Guid InviteBatchId { get; set; } = Guid.NewGuid();
    public string ThreadMri { get; set; } = "19:meeting_NWMzYzVlNGYtNjg5MC00ZjFiLWIzY2UtMjY4ZDg3MmRmYzMw@thread.v2";
}