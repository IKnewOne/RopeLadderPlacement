namespace RopeLadderPlacement;

public class ModConfig {
    public static string ConfigName = "LadderUtilities.json";
    public static ModConfig Instance { get; set; } = new ModConfig();

    public bool CanPlaceLadderWithRightClick = true;
    public bool CanCollectLadderTopdown = false;
}
