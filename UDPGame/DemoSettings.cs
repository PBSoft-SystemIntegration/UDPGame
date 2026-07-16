namespace UDPGame;

public sealed class DemoSettings
{
    public bool PredictionEnabled { get; set; } = true;
    public bool ReconciliationEnabled { get; set; } = true;
    public bool InterpolationEnabled { get; set; } = true;
    public int SimulatedOneWayLatencyMs { get; set; } = 125;
    public int ServerTickRate { get; set; } = 3;
}
