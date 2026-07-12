namespace EnergyAutomate.Data;

public sealed class ApiRuntimeSettings
{
    public static readonly Guid DefaultId = Guid.Parse("3A7C2146-68C4-4A83-8E6D-D3A18C2E524A");

    public Guid Id { get; set; } = DefaultId;

    public bool IsEnabled { get; set; } = true;

    public bool ApiSettingAutoMode { get; set; }

    public int ApiSettingAvgPower { get; set; } = 200;

    public int ApiSettingAvgPowerHysteresis { get; set; } = 110;

    public int ApiSettingAvgPowerLoadSeconds { get; set; } = 70;

    public int ApiSettingAvgPowerOffset { get; set; } = 50;

    public bool ApiSettingBatteryPriorityMode { get; set; } = false;

    public bool ApiSettingExtentionMode { get; set; } = true;

    public int ApiSettingExtentionAvgPower { get; set; } = 300;

    public TimeSpan ApiSettingExtentionExclusionFrom { get; set; } = new(7, 0, 0);

    public TimeSpan ApiSettingExtentionExclusionUntil { get; set; } = new(18, 0, 0);

    public int ApiSettingMaxPower { get; set; } = 800;

    public int ApiSettingPowerAdjustmentFactor { get; set; } = 50;

    public int ApiSettingPowerAdjustmentWaitCycles { get; set; } = 3;

    public bool ApiSettingRestrictionMode { get; set; } = false;

    public int ApiSettingSocMax { get; set; } = 90;

    public int ApiSettingSocMin { get; set; } = 10;

    public int ApiSettingTimeOffset { get; set; } = DateTimeOffset.Now.Offset.Hours;

    public string ActiveCalculationTemplateKey { get; set; } = "calculation.average-power";

    public string ActiveAdjustmentTemplateKey { get; set; } = "adjustment.auto-mode";

    public string ActiveDistributionTemplateKey { get; set; } = "distribution.equal";

    public string ActiveDistributionManagerTemplateKey { get; set; } = "distribution-manager.default";

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
