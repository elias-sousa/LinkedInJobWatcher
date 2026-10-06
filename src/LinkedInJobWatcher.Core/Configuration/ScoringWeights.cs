namespace LinkedInJobWatcher.Core.Configuration;

public sealed class ScoringWeights
{
    public int DotNetFocus { get; set; } = 35;
    public int Remote { get; set; } = 25;
    public int Senior { get; set; } = 20;
    public int StackItemEach { get; set; } = 2;
    public int StackMax { get; set; } = 10;
    public int Finance { get; set; } = 5;
    public int DirectContact { get; set; } = 5;

    public int OnSitePenalty { get; set; } = -30;
    public int HybridOutsideRioPenalty { get; set; } = -30;
    public int HybridInRioPenalty { get; set; } = -10;
    public int JuniorLevelPenalty { get; set; } = -30;
    public int NonDotNetFocusPenalty { get; set; } = -25;
}
