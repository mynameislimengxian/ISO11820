namespace ISO11820.Data;

// ========================================================================
// 1. operators — 操作员表
// ⚠️ 无主键约束，密码明文存储
// ========================================================================
public class Operator
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Pwd { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty; // "admin" | "operator"
}

// ========================================================================
// 2. apparatus — 设备表
// ========================================================================
public class Apparatus
{
    public int ApparatusId { get; set; }
    public string InnerNumber { get; set; } = string.Empty;
    public string ApparatusName { get; set; } = string.Empty;
    public DateTime CheckDateF { get; set; }
    public DateTime CheckDateT { get; set; }
    public string PidPort { get; set; } = string.Empty;
    public string PowerPort { get; set; } = string.Empty;
    public int? ConstPower { get; set; }
}

// ========================================================================
// 3. productmaster — 样品表
// ========================================================================
public class ProductMaster
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Specific { get; set; } = string.Empty;
    public double Diameter { get; set; }
    public double Height { get; set; }
    public string? Flag { get; set; }
}

// ========================================================================
// 4. testmaster — 试验记录表（核心）
// 联合主键：(ProductId, TestId)
// 外键：ProductId → productmaster.ProductId
// ========================================================================
public class TestMaster
{
    // ===== 基本信息 =====
    public string ProductId { get; set; } = string.Empty;       // PK, FK
    public string TestId { get; set; } = string.Empty;          // PK
    public DateTime TestDate { get; set; }
    public double AmbTemp { get; set; }
    public double AmbHumi { get; set; }
    public string According { get; set; } = "ISO 11820:2022";
    public string Operator { get; set; } = string.Empty;
    public string ApparatusId { get; set; } = string.Empty;
    public string ApparatusName { get; set; } = string.Empty;
    public DateTime ApparatusChkDate { get; set; }
    public string RptNo { get; set; } = string.Empty;

    // ===== 质量数据 =====
    public double PreWeight { get; set; }
    public double PostWeight { get; set; }
    public double LostWeight { get; set; }
    public double LostWeightPer { get; set; }       // 判定项：失重率%

    // ===== 试验过程 =====
    public int TotalTestTime { get; set; }
    public int ConstPower { get; set; }
    public string PhenoCode { get; set; } = string.Empty;
    public int FlameTime { get; set; }
    public int FlameDuration { get; set; }

    // ===== 各通道温度最大值 =====
    public double MaxTf1 { get; set; }
    public double MaxTf2 { get; set; }
    public double MaxTs { get; set; }
    public double MaxTc { get; set; }
    public int MaxTf1Time { get; set; }
    public int MaxTf2Time { get; set; }
    public int MaxTsTime { get; set; }
    public int MaxTcTime { get; set; }

    // ===== 各通道最终值 =====
    public double FinalTf1 { get; set; }
    public double FinalTf2 { get; set; }
    public double FinalTs { get; set; }
    public double FinalTc { get; set; }
    public int FinalTf1Time { get; set; }
    public int FinalTf2Time { get; set; }
    public int FinalTsTime { get; set; }
    public int FinalTcTime { get; set; }

    // ===== 温升 =====
    public double DeltaTf1 { get; set; }
    public double DeltaTf2 { get; set; }
    public double DeltaTf { get; set; }             // 判定项：样品温升
    public double DeltaTs { get; set; }
    public double DeltaTc { get; set; }

    // ===== 备注 =====
    public string? Memo { get; set; }
    public string? Flag { get; set; }               // "10000000" = 已保存

    /// <summary>是否已保存试验记录</summary>
    public bool IsSaved => Flag == "10000000";

    /// <summary>是否已完成但未保存</summary>
    public bool IsFinishedButUnsaved => TotalTestTime > 0 && !IsSaved;
}

// ========================================================================
// 5. sensors — 传感器配置表
// ========================================================================
public class Sensor
{
    public int SensorId { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public string DispName { get; set; } = string.Empty;
    public string SensorGroup { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Discription { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public double SignalZero { get; set; }
    public double SignalSpan { get; set; }
    public double OutputZero { get; set; }
    public double OutputSpan { get; set; }
    public double OutputValue { get; set; }     // 运行时更新
    public double InputValue { get; set; }      // 运行时更新
    public int SignalType { get; set; }         // 4 = 数字量（仿真）
}

// ========================================================================
// 6. CalibrationRecords — 校准记录表（⚠️ 表名大写）
// ========================================================================
public class CalibrationRecord
{
    // ===== 基本信息 =====
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CalibrationDate { get; set; } = string.Empty;
    public string CalibrationType { get; set; } = string.Empty; // "Surface" | "Center"
    public int ApparatusId { get; set; }
    public string Operator { get; set; } = string.Empty;

    /// <summary>JSON 字符串，需手动序列化/反序列化</summary>
    public string TemperatureData { get; set; } = "[]";

    public double? UniformityResult { get; set; }
    public double? MaxDeviation { get; set; }
    public double? AverageTemperature { get; set; }
    public int PassedCriteria { get; set; }     // 0=未通过, 1=通过
    public string Remarks { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;

    // ===== 炉壁 9 测温点 =====
    public double? TempA1 { get; set; }
    public double? TempA2 { get; set; }
    public double? TempA3 { get; set; }
    public double? TempB1 { get; set; }
    public double? TempB2 { get; set; }
    public double? TempB3 { get; set; }
    public double? TempC1 { get; set; }
    public double? TempC2 { get; set; }
    public double? TempC3 { get; set; }

    // ===== 计算结果 =====
    public double? TAvg { get; set; }
    public double? TAvgAxis1 { get; set; }
    public double? TAvgAxis2 { get; set; }
    public double? TAvgAxis3 { get; set; }
    public double? TAvgLevela { get; set; }
    public double? TAvgLevelb { get; set; }
    public double? TAvgLevelc { get; set; }
    public double? TDevAxis1 { get; set; }
    public double? TDevAxis2 { get; set; }
    public double? TDevAxis3 { get; set; }
    public double? TDevLevela { get; set; }
    public double? TDevLevelb { get; set; }
    public double? TDevLevelc { get; set; }
    public double? TAvgDevAxis { get; set; }
    public double? TAvgDevLevel { get; set; }

    public string? CenterTempData { get; set; }
    public string? Memo { get; set; }
}

// ========================================================================
// 辅助 DTO：试验查询结果摘要（用于列表展示）
// ========================================================================
public class TestSummary
{
    public string ProductId { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int TotalTestTime { get; set; }
    public double LostWeightPer { get; set; }
    public double DeltaTf { get; set; }
    public string? Flag { get; set; }
}