using Microsoft.Data.Sqlite;

namespace ISO11820.Data;

/// <summary>
/// SQLite 数据库操作封装类。
/// 所有 SQL 操作统一由此类管理，外部无需接触连接字符串和 SQL 语句。
/// 每个方法独立创建和释放连接，线程安全。
///
/// 使用方式：
///   var db = new DbHelper(AppGlobal.Instance.DbPath);
///   db.InitializeDatabase();  // 或由 DatabaseInitializer 单独调用
/// </summary>
public class DbHelper : IDisposable
{
    private readonly string _connectionString;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbPath">SQLite 数据库文件完整路径</param>
    public DbHelper(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    // ====================================================================
    // 一、登录
    // ====================================================================

    /// <summary>
    /// Login — 验证用户名和密码。
    /// </summary>
    /// <returns>验证成功返回 true，并输出 userid 和 usertype</returns>
    public bool Login(string username, string pwd, out string userid, out string usertype)
    {
        userid = string.Empty;
        usertype = string.Empty;

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT userid, usertype FROM operators WHERE username = $name AND pwd = $pwd";
        cmd.Parameters.AddWithValue("$name", username);
        cmd.Parameters.AddWithValue("$pwd", pwd);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            userid = reader.GetString(0);
            usertype = reader.GetString(1);
            return true;
        }
        return false;
    }

    // ====================================================================
    // 二、设备
    // ====================================================================

    /// <summary>
    /// GetApparatus — 获取设备信息（当前只有一台设备）。
    /// </summary>
    public Apparatus? GetApparatus()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM apparatus LIMIT 1";

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadApparatus(reader);
        return null;
    }

    /// <summary>
    /// UpdateConstPower — 更新设备的恒功率值。
    /// </summary>
    public void UpdateConstPower(int apparatusId, int power)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE apparatus SET constpower = $power WHERE apparatusid = $id";
        cmd.Parameters.AddWithValue("$power", power);
        cmd.Parameters.AddWithValue("$id", apparatusId);
        cmd.ExecuteNonQuery();
    }

    // ====================================================================
    // 三、样品
    // ====================================================================

    /// <summary>
    /// InsertProduct — 新建样品。
    /// </summary>
    public void InsertProduct(ProductMaster product)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO productmaster (productid, productname, specific, diameter, height, flag)
            VALUES ($pid, $name, $spec, $dia, $height, $flag)";
        cmd.Parameters.AddWithValue("$pid", product.ProductId);
        cmd.Parameters.AddWithValue("$name", product.ProductName);
        cmd.Parameters.AddWithValue("$spec", product.Specific);
        cmd.Parameters.AddWithValue("$dia", product.Diameter);
        cmd.Parameters.AddWithValue("$height", product.Height);
        cmd.Parameters.AddWithValue("$flag", (object?)product.Flag ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// GetProduct — 按样品编号查询样品。
    /// </summary>
    public ProductMaster? GetProduct(string productId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM productmaster WHERE productid = $pid";
        cmd.Parameters.AddWithValue("$pid", productId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadProduct(reader);
        return null;
    }

    /// <summary>
    /// QueryProducts — 查询全部样品列表。
    /// </summary>
    public List<ProductMaster> QueryProducts()
    {
        var list = new List<ProductMaster>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM productmaster ORDER BY productid DESC";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(ReadProduct(reader));
        return list;
    }

    // ====================================================================
    // 四、试验
    // ====================================================================

    /// <summary>
    /// InsertTest — 新建试验记录（统计字段全部填 0）。
    /// </summary>
    public void InsertTest(TestMaster test)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO testmaster (
                productid, testid, testdate, ambtemp, ambhumi,
                according, operator, apparatusid, apparatusname, apparatuschkdate, rptno,
                preweight, postweight, lostweight, lostweight_per,
                totaltesttime, constpower, phenocode, flametime, flameduration,
                maxtf1, maxtf2, maxts, maxtc,
                maxtf1_time, maxtf2_time, maxts_time, maxtc_time,
                finaltf1, finaltf2, finalts, finaltc,
                finaltf1_time, finaltf2_time, finalts_time, finaltc_time,
                deltatf1, deltatf2, deltatf, deltats, deltatc,
                memo, flag
            ) VALUES (
                $pid, $tid, $date, $ambtemp, $ambhumi,
                $according, $op, $appid, $appname, $appchkdate, $rptno,
                $prewt, 0, 0, 0,
                0, 0, '', 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                $memo, $flag
            )";
        cmd.Parameters.AddWithValue("$pid", test.ProductId);
        cmd.Parameters.AddWithValue("$tid", test.TestId);
        cmd.Parameters.AddWithValue("$date", test.TestDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$ambtemp", test.AmbTemp);
        cmd.Parameters.AddWithValue("$ambhumi", test.AmbHumi);
        cmd.Parameters.AddWithValue("$according", test.According);
        cmd.Parameters.AddWithValue("$op", test.Operator);
        cmd.Parameters.AddWithValue("$appid", test.ApparatusId);
        cmd.Parameters.AddWithValue("$appname", test.ApparatusName);
        cmd.Parameters.AddWithValue("$appchkdate", test.ApparatusChkDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$rptno", test.RptNo);
        cmd.Parameters.AddWithValue("$prewt", test.PreWeight);
        cmd.Parameters.AddWithValue("$memo", (object?)test.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$flag", (object?)test.Flag ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// UpdateTestResult — 试验完成后更新统计字段，设置 flag = "10000000"。
    /// </summary>
    public void UpdateTestResult(TestMaster test)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE testmaster SET
                postweight       = $post,
                lostweight       = $lost,
                lostweight_per   = $lostper,
                totaltesttime    = $time,
                constpower       = $cpower,
                phenocode        = $pheno,
                flametime        = $ftime,
                flameduration    = $fdur,
                maxtf1           = $maxtf1,
                maxtf2           = $maxtf2,
                maxts            = $maxts,
                maxtc            = $maxtc,
                maxtf1_time      = $maxtf1t,
                maxtf2_time      = $maxtf2t,
                maxts_time       = $maxtst,
                maxtc_time       = $maxtct,
                finaltf1         = $ftf1,
                finaltf2         = $ftf2,
                finalts          = $fts,
                finaltc          = $ftc,
                finaltf1_time    = $ftf1t,
                finaltf2_time    = $ftf2t,
                finalts_time     = $ftst,
                finaltc_time     = $ftct,
                deltatf1         = $dtf1,
                deltatf2         = $dtf2,
                deltatf          = $dtf,
                deltats          = $dts,
                deltatc          = $dtc,
                memo             = $memo,
                flag             = '10000000'
            WHERE productid = $pid AND testid = $tid";
        cmd.Parameters.AddWithValue("$post", test.PostWeight);
        cmd.Parameters.AddWithValue("$lost", test.LostWeight);
        cmd.Parameters.AddWithValue("$lostper", test.LostWeightPer);
        cmd.Parameters.AddWithValue("$time", test.TotalTestTime);
        cmd.Parameters.AddWithValue("$cpower", test.ConstPower);
        cmd.Parameters.AddWithValue("$pheno", test.PhenoCode);
        cmd.Parameters.AddWithValue("$ftime", test.FlameTime);
        cmd.Parameters.AddWithValue("$fdur", test.FlameDuration);
        cmd.Parameters.AddWithValue("$maxtf1", test.MaxTf1);
        cmd.Parameters.AddWithValue("$maxtf2", test.MaxTf2);
        cmd.Parameters.AddWithValue("$maxts", test.MaxTs);
        cmd.Parameters.AddWithValue("$maxtc", test.MaxTc);
        cmd.Parameters.AddWithValue("$maxtf1t", test.MaxTf1Time);
        cmd.Parameters.AddWithValue("$maxtf2t", test.MaxTf2Time);
        cmd.Parameters.AddWithValue("$maxtst", test.MaxTsTime);
        cmd.Parameters.AddWithValue("$maxtct", test.MaxTcTime);
        cmd.Parameters.AddWithValue("$ftf1", test.FinalTf1);
        cmd.Parameters.AddWithValue("$ftf2", test.FinalTf2);
        cmd.Parameters.AddWithValue("$fts", test.FinalTs);
        cmd.Parameters.AddWithValue("$ftc", test.FinalTc);
        cmd.Parameters.AddWithValue("$ftf1t", test.FinalTf1Time);
        cmd.Parameters.AddWithValue("$ftf2t", test.FinalTf2Time);
        cmd.Parameters.AddWithValue("$ftst", test.FinalTsTime);
        cmd.Parameters.AddWithValue("$ftct", test.FinalTcTime);
        cmd.Parameters.AddWithValue("$dtf1", test.DeltaTf1);
        cmd.Parameters.AddWithValue("$dtf2", test.DeltaTf2);
        cmd.Parameters.AddWithValue("$dtf", test.DeltaTf);
        cmd.Parameters.AddWithValue("$dts", test.DeltaTs);
        cmd.Parameters.AddWithValue("$dtc", test.DeltaTc);
        cmd.Parameters.AddWithValue("$memo", (object?)test.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$pid", test.ProductId);
        cmd.Parameters.AddWithValue("$tid", test.TestId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// GetTest — 按联合主键查询单条试验记录。
    /// </summary>
    public TestMaster? GetTest(string productId, string testId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster WHERE productid = $pid AND testid = $tid";
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadTest(reader);
        return null;
    }

    /// <summary>
    /// QueryTests — 按日期范围和可选样品编号查询试验记录。
    /// </summary>
    public List<TestSummary> QueryTests(DateTime from, DateTime to, string? productId = null)
    {
        var list = new List<TestSummary>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        var sql = @"
            SELECT t.productid, t.testid, t.testdate, t.operator,
                   t.totaltesttime, t.lostweight_per, t.deltatf, t.flag,
                   p.productname
            FROM testmaster t
            LEFT JOIN productmaster p ON t.productid = p.productid
            WHERE t.testdate BETWEEN $from AND $to";

        if (!string.IsNullOrEmpty(productId))
        {
            sql += " AND t.productid LIKE '%' || $pid || '%'";
            cmd.Parameters.AddWithValue("$pid", productId);
        }

        sql += " ORDER BY t.testdate DESC";

        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new TestSummary
            {
                ProductId = reader.GetString(0),
                TestId = reader.GetString(1),
                TestDate = reader.GetDateTime(2),
                Operator = reader.GetString(3),
                TotalTestTime = reader.GetInt32(4),
                LostWeightPer = reader.GetDouble(5),
                DeltaTf = reader.GetDouble(6),
                Flag = reader.IsDBNull(7) ? null : reader.GetString(7),
                ProductName = reader.IsDBNull(8) ? "" : reader.GetString(8)
            });
        }
        return list;
    }

    /// <summary>
    /// HasUnfinishedTest — 检查是否存在已完成但未保存的试验。
    /// 条件：totalTestTime > 0 且 flag != "10000000"
    /// </summary>
    public bool HasUnfinishedTest()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM testmaster
            WHERE totaltesttime > 0 AND (flag IS NULL OR flag != '10000000')";

        var count = (long)(cmd.ExecuteScalar() ?? 0);
        return count > 0;
    }

    // ====================================================================
    // 五、传感器
    // ====================================================================

    /// <summary>
    /// GetSensors — 获取全部传感器配置。
    /// </summary>
    public List<Sensor> GetSensors()
    {
        var list = new List<Sensor>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM sensors ORDER BY sensorid";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(ReadSensor(reader));
        return list;
    }

    /// <summary>
    /// UpdateSensorValue — 更新单个传感器的当前温度值。
    /// 仿真引擎每次采样后调用此方法。
    /// </summary>
    public void UpdateSensorValue(int sensorId, double outputValue, double inputValue)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE sensors SET outputvalue = $oval, inputvalue = $ival WHERE sensorid = $sid";
        cmd.Parameters.AddWithValue("$oval", outputValue);
        cmd.Parameters.AddWithValue("$ival", inputValue);
        cmd.Parameters.AddWithValue("$sid", sensorId);
        cmd.ExecuteNonQuery();
    }

    // ====================================================================
    // 六、校准
    // ====================================================================

    /// <summary>
    /// InsertCalibration — 插入一条校准记录。
    /// </summary>
    public void InsertCalibration(CalibrationRecord record)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO CalibrationRecords (
                Id, CalibrationDate, CalibrationType, ApparatusId, Operator,
                TemperatureData, UniformityResult, MaxDeviation, AverageTemperature,
                PassedCriteria, Remarks, CreatedAt,
                TempA1, TempA2, TempA3, TempB1, TempB2, TempB3, TempC1, TempC2, TempC3,
                TAvg, TAvgAxis1, TAvgAxis2, TAvgAxis3,
                TAvgLevela, TAvgLevelb, TAvgLevelc,
                TDevAxis1, TDevAxis2, TDevAxis3,
                TDevLevela, TDevLevelb, TDevLevelc,
                TAvgDevAxis, TAvgDevLevel,
                CenterTempData, Memo
            ) VALUES (
                $id, $caldate, $type, $appid, $op,
                $tempdata, $uniform, $maxdev, $avgtemp,
                $passed, $remarks, $created,
                $a1, $a2, $a3, $b1, $b2, $b3, $c1, $c2, $c3,
                $tavg, $tax1, $tax2, $tax3,
                $tla, $tlb, $tlc,
                $tdx1, $tdx2, $tdx3,
                $tdla, $tdlb, $tdlc,
                $tadx, $tadl,
                $ctd, $memo
            )";
        cmd.Parameters.AddWithValue("$id", record.Id);
        cmd.Parameters.AddWithValue("$caldate", record.CalibrationDate);
        cmd.Parameters.AddWithValue("$type", record.CalibrationType);
        cmd.Parameters.AddWithValue("$appid", record.ApparatusId);
        cmd.Parameters.AddWithValue("$op", record.Operator);
        cmd.Parameters.AddWithValue("$tempdata", record.TemperatureData);
        cmd.Parameters.AddWithValue("$uniform", (object?)record.UniformityResult ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$maxdev", (object?)record.MaxDeviation ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$avgtemp", (object?)record.AverageTemperature ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$passed", record.PassedCriteria);
        cmd.Parameters.AddWithValue("$remarks", record.Remarks);
        cmd.Parameters.AddWithValue("$created", record.CreatedAt);
        cmd.Parameters.AddWithValue("$a1", (object?)record.TempA1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$a2", (object?)record.TempA2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$a3", (object?)record.TempA3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$b1", (object?)record.TempB1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$b2", (object?)record.TempB2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$b3", (object?)record.TempB3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$c1", (object?)record.TempC1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$c2", (object?)record.TempC2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$c3", (object?)record.TempC3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tavg", (object?)record.TAvg ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tax1", (object?)record.TAvgAxis1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tax2", (object?)record.TAvgAxis2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tax3", (object?)record.TAvgAxis3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tla", (object?)record.TAvgLevela ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tlb", (object?)record.TAvgLevelb ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tlc", (object?)record.TAvgLevelc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdx1", (object?)record.TDevAxis1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdx2", (object?)record.TDevAxis2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdx3", (object?)record.TDevAxis3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdla", (object?)record.TDevLevela ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdlb", (object?)record.TDevLevelb ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tdlc", (object?)record.TDevLevelc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tadx", (object?)record.TAvgDevAxis ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tadl", (object?)record.TAvgDevLevel ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ctd", (object?)record.CenterTempData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$memo", (object?)record.Memo ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// QueryCalibrations — 按日期范围查询校准记录。
    /// </summary>
    public List<CalibrationRecord> QueryCalibrations(DateTime from, DateTime to)
    {
        var list = new List<CalibrationRecord>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT * FROM CalibrationRecords
            WHERE CalibrationDate BETWEEN $from AND $to
            ORDER BY CalibrationDate DESC";
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(ReadCalibration(reader));
        return list;
    }

    // ====================================================================
    // IDisposable
    // ====================================================================

    public void Dispose()
    {
        // SQLite 连接由各方法内的 using 管理，此处无需额外清理
    }

    // ====================================================================
    // 私有读取方法
    // ====================================================================

    private static Apparatus ReadApparatus(SqliteDataReader reader)
    {
        return new Apparatus
        {
            ApparatusId = reader.GetInt32(0),
            InnerNumber = reader.GetString(1),
            ApparatusName = reader.GetString(2),
            CheckDateF = reader.GetDateTime(3),
            CheckDateT = reader.GetDateTime(4),
            PidPort = reader.GetString(5),
            PowerPort = reader.GetString(6),
            ConstPower = reader.IsDBNull(7) ? null : reader.GetInt32(7)
        };
    }

    private static ProductMaster ReadProduct(SqliteDataReader reader)
    {
        return new ProductMaster
        {
            ProductId = reader.GetString(0),
            ProductName = reader.GetString(1),
            Specific = reader.GetString(2),
            Diameter = reader.GetDouble(3),
            Height = reader.GetDouble(4),
            Flag = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }

    private static TestMaster ReadTest(SqliteDataReader reader)
    {
        return new TestMaster
        {
            ProductId = reader.GetString(0),
            TestId = reader.GetString(1),
            TestDate = reader.GetDateTime(2),
            AmbTemp = reader.GetDouble(3),
            AmbHumi = reader.GetDouble(4),
            According = reader.GetString(5),
            Operator = reader.GetString(6),
            ApparatusId = reader.GetString(7),
            ApparatusName = reader.GetString(8),
            ApparatusChkDate = reader.GetDateTime(9),
            RptNo = reader.GetString(10),
            PreWeight = reader.GetDouble(11),
            PostWeight = reader.GetDouble(12),
            LostWeight = reader.GetDouble(13),
            LostWeightPer = reader.GetDouble(14),
            TotalTestTime = reader.GetInt32(15),
            ConstPower = reader.GetInt32(16),
            PhenoCode = reader.GetString(17),
            FlameTime = reader.GetInt32(18),
            FlameDuration = reader.GetInt32(19),
            MaxTf1 = reader.GetDouble(20),
            MaxTf2 = reader.GetDouble(21),
            MaxTs = reader.GetDouble(22),
            MaxTc = reader.GetDouble(23),
            MaxTf1Time = reader.GetInt32(24),
            MaxTf2Time = reader.GetInt32(25),
            MaxTsTime = reader.GetInt32(26),
            MaxTcTime = reader.GetInt32(27),
            FinalTf1 = reader.GetDouble(28),
            FinalTf2 = reader.GetDouble(29),
            FinalTs = reader.GetDouble(30),
            FinalTc = reader.GetDouble(31),
            FinalTf1Time = reader.GetInt32(32),
            FinalTf2Time = reader.GetInt32(33),
            FinalTsTime = reader.GetInt32(34),
            FinalTcTime = reader.GetInt32(35),
            DeltaTf1 = reader.GetDouble(36),
            DeltaTf2 = reader.GetDouble(37),
            DeltaTf = reader.GetDouble(38),
            DeltaTs = reader.GetDouble(39),
            DeltaTc = reader.GetDouble(40),
            Memo = reader.IsDBNull(41) ? null : reader.GetString(41),
            Flag = reader.IsDBNull(42) ? null : reader.GetString(42)
        };
    }

    private static Sensor ReadSensor(SqliteDataReader reader)
    {
        return new Sensor
        {
            SensorId = reader.GetInt32(0),
            SensorName = reader.GetString(1),
            DispName = reader.GetString(2),
            SensorGroup = reader.GetString(3),
            Unit = reader.GetString(4),
            Discription = reader.GetString(5),
            Flag = reader.GetString(6),
            SignalZero = reader.GetDouble(7),
            SignalSpan = reader.GetDouble(8),
            OutputZero = reader.GetDouble(9),
            OutputSpan = reader.GetDouble(10),
            OutputValue = reader.GetDouble(11),
            InputValue = reader.GetDouble(12),
            SignalType = reader.GetInt32(13)
        };
    }

    private static CalibrationRecord ReadCalibration(SqliteDataReader reader)
    {
        return new CalibrationRecord
        {
            Id = reader.GetString(0),
            CalibrationDate = reader.GetString(1),
            CalibrationType = reader.GetString(2),
            ApparatusId = reader.GetInt32(3),
            Operator = reader.GetString(4),
            TemperatureData = reader.GetString(5),
            UniformityResult = reader.IsDBNull(6) ? null : reader.GetDouble(6),
            MaxDeviation = reader.IsDBNull(7) ? null : reader.GetDouble(7),
            AverageTemperature = reader.IsDBNull(8) ? null : reader.GetDouble(8),
            PassedCriteria = reader.GetInt32(9),
            Remarks = reader.GetString(10),
            CreatedAt = reader.GetString(11),
            TempA1 = ReadNullableDouble(reader, 12),
            TempA2 = ReadNullableDouble(reader, 13),
            TempA3 = ReadNullableDouble(reader, 14),
            TempB1 = ReadNullableDouble(reader, 15),
            TempB2 = ReadNullableDouble(reader, 16),
            TempB3 = ReadNullableDouble(reader, 17),
            TempC1 = ReadNullableDouble(reader, 18),
            TempC2 = ReadNullableDouble(reader, 19),
            TempC3 = ReadNullableDouble(reader, 20),
            TAvg = ReadNullableDouble(reader, 21),
            TAvgAxis1 = ReadNullableDouble(reader, 22),
            TAvgAxis2 = ReadNullableDouble(reader, 23),
            TAvgAxis3 = ReadNullableDouble(reader, 24),
            TAvgLevela = ReadNullableDouble(reader, 25),
            TAvgLevelb = ReadNullableDouble(reader, 26),
            TAvgLevelc = ReadNullableDouble(reader, 27),
            TDevAxis1 = ReadNullableDouble(reader, 28),
            TDevAxis2 = ReadNullableDouble(reader, 29),
            TDevAxis3 = ReadNullableDouble(reader, 30),
            TDevLevela = ReadNullableDouble(reader, 31),
            TDevLevelb = ReadNullableDouble(reader, 32),
            TDevLevelc = ReadNullableDouble(reader, 33),
            TAvgDevAxis = ReadNullableDouble(reader, 34),
            TAvgDevLevel = ReadNullableDouble(reader, 35),
            CenterTempData = reader.IsDBNull(36) ? null : reader.GetString(36),
            Memo = reader.IsDBNull(37) ? null : reader.GetString(37)
        };
    }

    private static double? ReadNullableDouble(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
    }
}