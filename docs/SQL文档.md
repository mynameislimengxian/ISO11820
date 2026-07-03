# ISO 11820 SQL 文档

> 数据库引擎：**SQLite**  
> 数据库文件：`Data\ISO11820.db`（相对 exe 路径，由 `appsettings.json` 的 `Database.SqlitePath` 配置）  
> 程序首次运行时调用 `DbHelper.InitializeDatabase()` 自动执行全部建表语句。

---

## 一、建表 DDL（按依赖顺序）

### 1. operators（操作员表）

```sql
-- ⚠️ 此表无主键约束，密码明文存储
CREATE TABLE IF NOT EXISTS "operators" (
    "userid"    TEXT NOT NULL,
    "username"  TEXT NOT NULL,
    "pwd"       TEXT NOT NULL,
    "usertype"  TEXT NOT NULL
);
```

### 2. apparatus（设备表）

```sql
CREATE TABLE IF NOT EXISTS "apparatus" (
    "apparatusid"   INTEGER NOT NULL CONSTRAINT "PK_apparatus" PRIMARY KEY,
    "innernumber"   TEXT NOT NULL,
    "apparatusname" TEXT NOT NULL,
    "checkdatef"    date NOT NULL,
    "checkdatet"    date NOT NULL,
    "pidport"       TEXT NOT NULL,
    "powerport"     TEXT NOT NULL,
    "constpower"    INTEGER NULL
);
```

### 3. productmaster（样品表）

```sql
CREATE TABLE IF NOT EXISTS "productmaster" (
    "productid"   TEXT NOT NULL CONSTRAINT "PK_productmaster" PRIMARY KEY,
    "productname" TEXT NOT NULL,
    "specific"    TEXT NOT NULL,
    "diameter"    REAL NOT NULL,
    "height"      REAL NOT NULL,
    "flag"        TEXT NULL
);
```

### 4. sensors（传感器配置表）

```sql
CREATE TABLE IF NOT EXISTS "sensors" (
    "sensorid"    INTEGER NOT NULL CONSTRAINT "PK_sensors" PRIMARY KEY,
    "sensorname"  TEXT NOT NULL,
    "dispname"    TEXT NOT NULL,
    "sensorgroup" TEXT NOT NULL,
    "unit"        TEXT NOT NULL,
    "discription" TEXT NOT NULL,
    "flag"        TEXT NOT NULL,
    "signalzero"  REAL NOT NULL,
    "signalspan"  REAL NOT NULL,
    "outputzero"  REAL NOT NULL,
    "outputspan"  REAL NOT NULL,
    "outputvalue" REAL NOT NULL,
    "inputvalue"  REAL NOT NULL,
    "signaltype"  INTEGER NOT NULL
);
```

### 5. testmaster（试验记录表）⭐ 核心表

```sql
-- 主键：(productid, testid) 联合主键
-- 外键：productid → productmaster.productid
CREATE TABLE IF NOT EXISTS "testmaster" (
    "productid"        TEXT NOT NULL,
    "testid"           TEXT NOT NULL,
    "testdate"         date NOT NULL,
    "ambtemp"          REAL NOT NULL,
    "ambhumi"          REAL NOT NULL,
    "according"        TEXT NOT NULL,
    "operator"         TEXT NOT NULL,
    "apparatusid"      TEXT NOT NULL,
    "apparatusname"    TEXT NOT NULL,
    "apparatuschkdate" date NOT NULL,
    "rptno"            TEXT NOT NULL,

    -- 质量数据
    "preweight"        REAL NOT NULL,
    "postweight"       REAL NOT NULL,
    "lostweight"       REAL NOT NULL,
    "lostweight_per"   REAL NOT NULL,

    -- 试验过程
    "totaltesttime"    INTEGER NOT NULL,
    "constpower"       INTEGER NOT NULL,
    "phenocode"        TEXT NOT NULL,
    "flametime"        INTEGER NOT NULL,
    "flameduration"    INTEGER NOT NULL,

    -- 各通道温度最大值
    "maxtf1"           REAL NOT NULL,
    "maxtf2"           REAL NOT NULL,
    "maxts"            REAL NOT NULL,
    "maxtc"            REAL NOT NULL,
    "maxtf1_time"      INTEGER NOT NULL,
    "maxtf2_time"      INTEGER NOT NULL,
    "maxts_time"       INTEGER NOT NULL,
    "maxtc_time"       INTEGER NOT NULL,

    -- 各通道最终值
    "finaltf1"         REAL NOT NULL,
    "finaltf2"         REAL NOT NULL,
    "finalts"          REAL NOT NULL,
    "finaltc"          REAL NOT NULL,
    "finaltf1_time"    INTEGER NOT NULL,
    "finaltf2_time"    INTEGER NOT NULL,
    "finalts_time"     INTEGER NOT NULL,
    "finaltc_time"     INTEGER NOT NULL,

    -- 温升
    "deltatf1"         REAL NOT NULL,
    "deltatf2"         REAL NOT NULL,
    "deltatf"          REAL NOT NULL,
    "deltats"          REAL NOT NULL,
    "deltatc"          REAL NOT NULL,

    -- 备注
    "memo"             TEXT NULL,
    "flag"             TEXT NULL,

    CONSTRAINT "PK_testmaster" PRIMARY KEY ("productid", "testid"),
    CONSTRAINT "FK_testmaster_productmaster" FOREIGN KEY ("productid")
        REFERENCES "productmaster" ("productid")
);
```

### 6. CalibrationRecords（校准记录表）⚠️ 表名大写

```sql
CREATE TABLE IF NOT EXISTS "CalibrationRecords" (
    "Id"                 TEXT NOT NULL CONSTRAINT "PK_CalibrationRecords" PRIMARY KEY,
    "CalibrationDate"    TEXT NOT NULL,
    "CalibrationType"    TEXT NOT NULL,
    "ApparatusId"        INTEGER NOT NULL,
    "Operator"           TEXT NOT NULL,
    "TemperatureData"    TEXT NOT NULL,
    "UniformityResult"   REAL NULL,
    "MaxDeviation"       REAL NULL,
    "AverageTemperature" REAL NULL,
    "PassedCriteria"     INTEGER NOT NULL,
    "Remarks"            TEXT NOT NULL,
    "CreatedAt"          TEXT NOT NULL,

    -- 炉壁9测温点
    "TempA1" REAL NULL, "TempA2" REAL NULL, "TempA3" REAL NULL,
    "TempB1" REAL NULL, "TempB2" REAL NULL, "TempB3" REAL NULL,
    "TempC1" REAL NULL, "TempC2" REAL NULL, "TempC3" REAL NULL,

    -- 计算结果
    "TAvg"        REAL NULL,
    "TAvgAxis1"   REAL NULL, "TAvgAxis2" REAL NULL, "TAvgAxis3" REAL NULL,
    "TAvgLevela"  REAL NULL, "TAvgLevelb" REAL NULL, "TAvgLevelc" REAL NULL,
    "TDevAxis1"   REAL NULL, "TDevAxis2" REAL NULL, "TDevAxis3" REAL NULL,
    "TDevLevela"  REAL NULL, "TDevLevelb" REAL NULL, "TDevLevelc" REAL NULL,
    "TAvgDevAxis" REAL NULL, "TAvgDevLevel" REAL NULL,

    "CenterTempData" TEXT NULL,
    "Memo"           TEXT NULL
);
```

---

## 二、索引

```sql
-- testmaster 查询索引
CREATE INDEX IF NOT EXISTS "IX_Testmaster_Testdate"
    ON "testmaster" ("testdate");

CREATE INDEX IF NOT EXISTS "IX_Testmaster_Operator"
    ON "testmaster" ("operator");

CREATE INDEX IF NOT EXISTS "IX_Testmaster_Testdate_Productid"
    ON "testmaster" ("testdate", "productid");

-- CalibrationRecords 查询索引
CREATE INDEX IF NOT EXISTS "IX_CalibrationRecord_Date"
    ON "CalibrationRecords" ("CalibrationDate");

CREATE INDEX IF NOT EXISTS "IX_CalibrationRecord_Operator"
    ON "CalibrationRecords" ("Operator");
```

---

## 三、初始数据 INSERT

程序首次运行时写入，使用 `WHERE NOT EXISTS` 防止重复插入。

```sql
-- ============================
-- operators（2 条初始账号）
-- ============================
INSERT INTO operators (userid, username, pwd, usertype)
SELECT '1', 'admin', '123456', 'admin'
WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'admin');

INSERT INTO operators (userid, username, pwd, usertype)
SELECT '2', 'experimenter', '123456', 'operator'
WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'experimenter');

-- ============================
-- apparatus（1 条初始设备）
-- ============================
INSERT INTO apparatus (apparatusid, innernumber, apparatusname,
    checkdatef, checkdatet, pidport, powerport, constpower)
SELECT 0, 'FURNACE-01', '一号试验炉',
    date('now'), date('now', '+1 year'), 'COM9', 'COM9', 2048
WHERE NOT EXISTS (SELECT 1 FROM apparatus WHERE apparatusid = 0);

-- ============================
-- sensors（17 条初始通道）
-- ============================
INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 0, 'Sensor0', '炉温1', '采集', '℃', '炉温1', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 0);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 1, 'Sensor1', '炉温2', '采集', '℃', '炉温2', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 1);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 2, 'Sensor2', '表面温度', '采集', '℃', '表面温度', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 2);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 3, 'Sensor3', '中心温度', '采集', '℃', '中心温度', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 3);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 16, 'Sensor16', '校准温度', '校准', '℃', '校准温度', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 16);

-- 备用通道 4~15
INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 4, 'Sensor4', '备用通道5', '采集', '℃', '备用通道5', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 4);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 5, 'Sensor5', '备用通道6', '采集', '℃', '备用通道6', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 5);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 6, 'Sensor6', '备用通道7', '采集', '℃', '备用通道7', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 6);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 7, 'Sensor7', '备用通道8', '采集', '℃', '备用通道8', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 7);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 8, 'Sensor8', '备用通道9', '采集', '℃', '备用通道9', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 8);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 9, 'Sensor9', '备用通道10', '采集', '℃', '备用通道10', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 9);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 10, 'Sensor10', '备用通道11', '采集', '℃', '备用通道11', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 10);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 11, 'Sensor11', '备用通道12', '采集', '℃', '备用通道12', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 11);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 12, 'Sensor12', '备用通道13', '采集', '℃', '备用通道13', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 12);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 13, 'Sensor13', '备用通道14', '采集', '℃', '备用通道14', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 13);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 14, 'Sensor14', '备用通道15', '采集', '℃', '备用通道15', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 14);

INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit,
    discription, flag, signalzero, signalspan, outputzero, outputspan,
    outputvalue, inputvalue, signaltype)
SELECT 15, 'Sensor15', '备用通道16', '采集', '℃', '备用通道16', '启用',
    0, 0, 0, 1000, 0, 0, 4
WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 15);
```

---

## 四、温度时序数据（不入库，存 CSV）

每次试验的逐秒温度数据存为独立 CSV 文件，路径规则：

```
{BaseDirectory}\TestData\{productid}\{testid}\sensor_data.csv
```

默认示例：`D:\ISO11820\TestData\20240613-001\20240613-143000\sensor_data.csv`

CSV 格式（无标题行，每秒一行）：

```csv
0,25.0,24.9,24.5,24.3,25.1
1,30.1,30.0,24.6,24.4,25.0
2,70.2,69.8,24.8,24.5,25.2
...
```

| 列序号 | 列名 | 对应通道 | 说明 |
|--------|------|---------|------|
| 1 | Time | — | 秒序号，从 0 开始 |
| 2 | Temp1 | 炉温1（TF1） | 保留 1 位小数 |
| 3 | Temp2 | 炉温2（TF2） | 保留 1 位小数 |
| 4 | TempSurface | 表面温（TS） | 保留 1 位小数 |
| 5 | TempCenter | 中心温（TC） | 保留 1 位小数 |
| 6 | TempCalibration | 校准温（TCal） | 保留 1 位小数 |

---

## 五、执行顺序总结

`DbHelper.InitializeDatabase()` 中的执行顺序：

```
1. CREATE TABLE IF NOT EXISTS operators
2. CREATE TABLE IF NOT EXISTS apparatus
3. CREATE TABLE IF NOT EXISTS productmaster
4. CREATE TABLE IF NOT EXISTS sensors
5. CREATE TABLE IF NOT EXISTS testmaster        ← 依赖 productmaster
6. CREATE TABLE IF NOT EXISTS CalibrationRecords
7. CREATE INDEX (5 条)
8. INSERT 初始数据（operators 2条 + apparatus 1条 + sensors 17条）
```

> productmaster 和 testmaster 没有初始数据，由用户操作时写入。