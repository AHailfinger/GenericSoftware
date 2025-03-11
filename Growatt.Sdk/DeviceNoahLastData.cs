/// <summary>
/// Represents the last data of a Noah device.
/// </summary>
public class DeviceNoahLastData
{
    /// <summary>
    /// Device number.
    /// </summary>
    public string deviceSn { get; set; }

    /// <summary>
    /// Data logger serial number.
    /// </summary>
    public string datalogSn { get; set; }

    /// <summary>
    /// Time.
    /// </summary>
    public long time { get; set; }

    /// <summary>
    /// Whether it is retransmitted data.
    /// </summary>
    public int isAgain { get; set; }

    /// <summary>
    /// 1: Normal, 4: Fault, 5: Heating.
    /// </summary>
    public int status { get; set; }

    /// <summary>
    /// BIT0: PV1 overvoltage protection, BIT1: PV1 overcurrent protection, BIT2: PV1 overtemperature protection, BIT3: Reserved, BIT4: PV2 overvoltage protection, BIT5: PV2 overcurrent protection.
    /// </summary>
    public int mpptProtectStatus { get; set; }

    /// <summary>
    /// BIT0: Communication with BMS failed, BIT1: Communication with MPPT failed.
    /// </summary>
    public int pdWarnStatus { get; set; }

    /// <summary>
    /// BUCK output power.
    /// </summary>
    public float pac { get; set; }

    /// <summary>
    /// Daily power generation.
    /// </summary>
    public float eacToday { get; set; }

    /// <summary>
    /// Monthly power generation.
    /// </summary>
    public float eacMonth { get; set; }

    /// <summary>
    /// Annual power generation.
    /// </summary>
    public float eacYear { get; set; }

    /// <summary>
    /// Total power generation.
    /// </summary>
    public float eacTotal { get; set; }

    /// <summary>
    /// Photovoltaic power (W).
    /// </summary>
    public float ppv { get; set; }

    /// <summary>
    /// Current time period working mode.
    /// </summary>
    public int workMode { get; set; }

    /// <summary>
    /// BIT0: Charging, BIT1: Discharging, if neither, display standby.
    /// </summary>
    public int totalBatteryPackChargingStatus { get; set; }

    /// <summary>
    /// Total battery charging/discharging power.
    /// </summary>
    public int totalBatteryPackChargingPower { get; set; }

    /// <summary>
    /// Number of parallel battery packs.
    /// </summary>
    public int batteryPackageQuantity { get; set; }

    /// <summary>
    /// Total battery pack SOC (State of Charge) percentage.
    /// </summary>
    public int totalBatteryPackSoc { get; set; }

    /// <summary>
    /// Heating status, BIT0: Battery pack 1 is heating, BIT1: Battery pack 2 is heating, BIT2: Battery pack 3 is heating, BIT3: Battery pack 4 is heating.
    /// </summary>
    public int heatingStatus { get; set; }

    /// <summary>
    /// Fault status, BIT0: Battery pack 1 fault, BIT1: Battery pack 2 fault, BIT2: Battery pack 3 fault, BIT3: Battery pack 4 fault.
    /// </summary>
    public int faultStatus { get; set; }

    /// <summary>
    /// Battery pack 1—SN.
    /// </summary>
    public string battery1SerialNum { get; set; }

    /// <summary>
    /// Battery pack 1_SOC.
    /// </summary>
    public int battery1Soc { get; set; }

    /// <summary>
    /// Battery pack 1 temperature.
    /// </summary>
    public float battery1Temp { get; set; }

    /// <summary>
    /// Battery pack 1 warning status, BIT0: Low voltage warning, BIT1: High voltage warning, BIT2: Low charging temperature warning, BIT3: High charging temperature warning, BIT4: Low discharging temperature warning, BIT5: High discharging temperature warning, BIT6: Charging overcurrent warning, BIT7: Discharging overcurrent warning, BIT8~BIT15: Reserved.
    /// </summary>
    public int battery1WarnStatus { get; set; }

    /// <summary>
    /// Battery pack 1 protection status, BIT0: Low voltage protection, BIT1: High voltage protection, BIT2: Low charging temperature protection, BIT3: High charging temperature protection, BIT4: Low discharging temperature protection, BIT5: High discharging temperature protection, BIT6: Charging overcurrent protection, BIT7: Discharging overcurrent protection, BIT8: Battery error, BIT9: NTC disconnection, BIT10: Voltage sampling line disconnection, BIT11~BIT15: Reserved.
    /// </summary>
    public int battery1ProtectStatus { get; set; }

    /// <summary>
    /// Battery pack 2—SN.
    /// </summary>
    public string battery2SerialNum { get; set; }

    /// <summary>
    /// Battery pack 2_SOC.
    /// </summary>
    public int battery2Soc { get; set; }

    /// <summary>
    /// Battery pack 2 temperature.
    /// </summary>
    public float battery2Temp { get; set; }

    /// <summary>
    /// Battery pack 2 warning status, BIT0: Low voltage warning, BIT1: High voltage warning, BIT2: Low charging temperature warning, BIT3: High charging temperature warning, BIT4: Low discharging temperature warning, BIT5: High discharging temperature warning, BIT6: Charging overcurrent warning, BIT7: Discharging overcurrent warning, BIT8~BIT15: Reserved.
    /// </summary>
    public int battery2WarnStatus { get; set; }

    /// <summary>
    /// Battery pack 2 protection status, BIT0: Low voltage protection, BIT1: High voltage protection, BIT2: Low charging temperature protection, BIT3: High charging temperature protection, BIT4: Low discharging temperature protection, BIT5: High discharging temperature protection, BIT6: Charging overcurrent protection, BIT7: Discharging overcurrent protection, BIT8: Battery error, BIT9: NTC disconnection, BIT10: Voltage sampling line disconnection, BIT11~BIT15: Reserved.
    /// </summary>
    public int battery2ProtectStatus { get; set; }

    /// <summary>
    /// Battery pack 3—SN.
    /// </summary>
    public string battery3SerialNum { get; set; }

    /// <summary>
    /// Battery pack 3_SOC.
    /// </summary>
    public int battery3Soc { get; set; }

    /// <summary>
    /// Battery pack 3 temperature.
    /// </summary>
    public float battery3Temp { get; set; }

    /// <summary>
    /// Battery pack 3 warning status, BIT0: Low voltage warning, BIT1: High voltage warning, BIT2: Low charging temperature warning, BIT3: High charging temperature warning, BIT4: Low discharging temperature warning, BIT5: High discharging temperature warning, BIT6: Charging overcurrent warning, BIT7: Discharging overcurrent warning, BIT8~BIT15: Reserved.
    /// </summary>
    public int battery3WarnStatus { get; set; }

    /// <summary>
    /// Battery pack 3 protection status, BIT0: Low voltage protection, BIT1: High voltage protection, BIT2: Low charging temperature protection, BIT3: High charging temperature protection, BIT4: Low discharging temperature protection, BIT5: High discharging temperature protection, BIT6: Charging overcurrent protection, BIT7: Discharging overcurrent protection, BIT8: Battery error, BIT9: NTC disconnection, BIT10: Voltage sampling line disconnection, BIT11~BIT15: Reserved.
    /// </summary>
    public int battery3ProtectStatus { get; set; }

    /// <summary>
    /// Battery pack 4—SN.
    /// </summary>
    public string battery4SerialNum { get; set; }

    /// <summary>
    /// Battery pack 4_SOC.
    /// </summary>
    public int battery4Soc { get; set; }

    /// <summary>
    /// Battery pack 4 temperature.
    /// </summary>
    public float battery4Temp { get; set; }

    /// <summary>
    /// Battery pack 4 warning status, BIT0: Low voltage warning, BIT1: High voltage warning, BIT2: Low charging temperature warning, BIT3: High charging temperature warning, BIT4: Low discharging temperature warning, BIT5: High discharging temperature warning, BIT6: Charging overcurrent warning, BIT7: Discharging overcurrent warning, BIT8~BIT15: Reserved.
    /// </summary>
    public int battery4WarnStatus { get; set; }

    /// <summary>
    /// Battery pack 4 protection status, BIT0: Low voltage protection, BIT1: High voltage protection, BIT2: Low charging temperature protection, BIT3: High charging temperature protection, BIT4: Low discharging temperature protection, BIT5: High discharging temperature protection, BIT6: Charging overcurrent protection, BIT7: Discharging overcurrent protection, BIT8: Battery error, BIT9: NTC disconnection, BIT10: Voltage sampling line disconnection, BIT11~BIT15: Reserved.
    /// </summary>
    public int battery4ProtectStatus { get; set; }

    /// <summary>
    /// Settable time period.
    /// </summary>
    public int settableTimePeriod { get; set; }

    /// <summary>
    /// AC couple warning status.
    /// </summary>
    public int acCoupleWarnStatus { get; set; }

    /// <summary>
    /// AC couple protection status.
    /// </summary>
    public int acCoupleProtectStatus { get; set; }

    /// <summary>
    /// CT flag.
    /// </summary>
    public int ctFlag { get; set; }

    /// <summary>
    /// Total household load.
    /// </summary>
    public float totalHouseholdLoad { get; set; }

    /// <summary>
    /// Household load apart from Groplug.
    /// </summary>
    public float householdLoadApartFromGroplug { get; set; }

    /// <summary>
    /// On/Off grid.
    /// </summary>
    public int onOffGrid { get; set; }

    /// <summary>
    /// CT self power.
    /// </summary>
    public float ctSelfPower { get; set; }

    /// <summary>
    /// Charge SOC limit.
    /// </summary>
    public int chargeSocLimit { get; set; }

    /// <summary>
    /// Discharge SOC limit.
    /// </summary>
    public int dischargeSocLimit { get; set; }

    /// <summary>
    /// PV1 voltage.
    /// </summary>
    public float pv1Voltage { get; set; }

    /// <summary>
    /// PV1 current.
    /// </summary>
    public float pv1Current { get; set; }

    /// <summary>
    /// PV1 temperature.
    /// </summary>
    public float pv1Temp { get; set; }

    /// <summary>
    /// PV2 voltage.
    /// </summary>
    public float pv2Voltage { get; set; }

    /// <summary>
    /// PV2 current.
    /// </summary>
    public float pv2Current { get; set; }

    /// <summary>
    /// PV2 temperature.
    /// </summary>
    public float pv2Temp { get; set; }

    /// <summary>
    /// System temperature.
    /// </summary>
    public float systemTemp { get; set; }

    /// <summary>
    /// Max cell voltage.
    /// </summary>
    public float maxCellVoltage { get; set; }

    /// <summary>
    /// Min cell voltage.
    /// </summary>
    public float minCellVoltage { get; set; }

    /// <summary>
    /// Battery cycles.
    /// </summary>
    public int batteryCycles { get; set; }

    /// <summary>
    /// Battery SOH (State of Health).
    /// </summary>
    public int batterySoh { get; set; }

    /// <summary>
    /// PV3 voltage.
    /// </summary>
    public float pv3Voltage { get; set; }

    /// <summary>
    /// PV3 current.
    /// </summary>
    public float pv3Current { get; set; }

    /// <summary>
    /// PV3 temperature.
    /// </summary>
    public float pv3Temp { get; set; }

    /// <summary>
    /// PV4 voltage.
    /// </summary>
    public float pv4Voltage { get; set; }

    /// <summary>
    /// PV4 current.
    /// </summary>
    public float pv4Current { get; set; }

    /// <summary>
    /// PV4 temperature.
    /// </summary>
    public float pv4Temp { get; set; }

    /// <summary>
    /// Battery pack 1 temperature in Fahrenheit.
    /// </summary>
    public float battery1TempF { get; set; }

    /// <summary>
    /// Battery pack 2 temperature in Fahrenheit.
    /// </summary>
    public float battery2TempF { get; set; }

    /// <summary>
    /// Battery pack 3 temperature in Fahrenheit.
    /// </summary>
    public float battery3TempF { get; set; }

    /// <summary>
    /// Battery pack 4 temperature in Fahrenheit.
    /// </summary>
    public float battery4TempF { get; set; }

    /// <summary>
    /// Time string.
    /// </summary>
    public string timeStr { get; set; }
}
