# ISO 11820 数据库 ER 图

> 在 VS Code 中按 `Ctrl+Shift+V` 预览，即可看到渲染后的 ER 图。
> 如果预览不渲染，安装插件 "Markdown Preview Mermaid Support"（bierner.markdown-mermaid）。

```mermaid
erDiagram
    operators {
        TEXT userid "NOT NULL 用户ID"
        TEXT username "NOT NULL 登录用户名"
        TEXT pwd "NOT NULL 明文密码"
        TEXT usertype "NOT NULL 角色 admin/operator"
    }

    apparatus {
        INTEGER apparatusid "PK 设备ID"
        TEXT innernumber "NOT NULL 内部编号"
        TEXT apparatusname "NOT NULL 设备名称"
        date checkdatef "NOT NULL 检定开始日期"
        date checkdatet "NOT NULL 检定结束日期"
        TEXT pidport "NOT NULL PID串口"
        TEXT powerport "NOT NULL 功率串口"
        INTEGER constpower "NULL 恒功率值"
    }

    productmaster {
        TEXT productid "PK 样品编号"
        TEXT productname "NOT NULL 样品名称"
        TEXT specific "NOT NULL 规格型号"
        REAL diameter "NOT NULL 直径mm"
        REAL height "NOT NULL 高度mm"
        TEXT flag "NULL 备用"
    }

    testmaster {
        TEXT productid "PK,FK 样品编号"
        TEXT testid "PK 试验标识"
        date testdate "NOT NULL 试验日期"
        REAL ambtemp "NOT NULL 环境温度"
        REAL ambhumi "NOT NULL 环境湿度"
        TEXT according "NOT NULL 试验依据"
        TEXT operator "NOT NULL 操作员"
        TEXT apparatusid "NOT NULL 设备编号"
        TEXT apparatusname "NOT NULL 设备名称"
        date apparatuschkdate "NOT NULL 检定日期"
        TEXT rptno "NOT NULL 报告编号"
        REAL preweight "NOT NULL 试验前质量"
        REAL postweight "NOT NULL 试验后质量"
        REAL lostweight "NOT NULL 失重量"
        REAL lostweight_per "NOT NULL 判定项-失重率"
        INTEGER totaltesttime "NOT NULL 总时长秒"
        INTEGER constpower "NOT NULL 恒功率值"
        TEXT phenocode "NOT NULL 现象编码"
        INTEGER flametime "NOT NULL 火焰时刻"
        INTEGER flameduration "NOT NULL 火焰持续"
        REAL maxtf1 "NOT NULL 炉温1最大值"
        REAL maxtf2 "NOT NULL 炉温2最大值"
        REAL maxts "NOT NULL 表面温最大值"
        REAL maxtc "NOT NULL 中心温最大值"
        INTEGER maxtf1_time "NOT NULL"
        INTEGER maxtf2_time "NOT NULL"
        INTEGER maxts_time "NOT NULL"
        INTEGER maxtc_time "NOT NULL"
        REAL finaltf1 "NOT NULL 最终炉温1"
        REAL finaltf2 "NOT NULL 最终炉温2"
        REAL finalts "NOT NULL 最终表面温"
        REAL finaltc "NOT NULL 最终中心温"
        INTEGER finaltf1_time "NOT NULL"
        INTEGER finaltf2_time "NOT NULL"
        INTEGER finalts_time "NOT NULL"
        INTEGER finaltc_time "NOT NULL"
        REAL deltatf1 "NOT NULL 炉温1温升"
        REAL deltatf2 "NOT NULL 炉温2温升"
        REAL deltatf "NOT NULL 判定项-样品温升"
        REAL deltats "NOT NULL 表面温升"
        REAL deltatc "NOT NULL 中心温升"
        TEXT memo "NULL 备注"
        TEXT flag "NULL 完成标记"
    }

    sensors {
        INTEGER sensorid "PK 通道编号"
        TEXT sensorname "NOT NULL 传感器代号"
        TEXT dispname "NOT NULL 显示名"
        TEXT sensorgroup "NOT NULL 分组"
        TEXT unit "NOT NULL 单位"
        TEXT discription "NOT NULL 描述"
        TEXT flag "NOT NULL 标记"
        REAL signalzero "NOT NULL 信号零点"
        REAL signalspan "NOT NULL 信号量程"
        REAL outputzero "NOT NULL 输出下限"
        REAL outputspan "NOT NULL 输出上限"
        REAL outputvalue "NOT NULL 当前温度值"
        REAL inputvalue "NOT NULL 当前输入值"
        INTEGER signaltype "NOT NULL 信号类型"
    }

    CalibrationRecords {
        TEXT Id "PK GUID"
        TEXT CalibrationDate "NOT NULL 校准日期"
        TEXT CalibrationType "NOT NULL Surface/Center"
        INTEGER ApparatusId "NOT NULL 设备ID"
        TEXT Operator "NOT NULL 操作员"
        TEXT TemperatureData "NOT NULL JSON字符串"
        REAL UniformityResult "NULL 均匀性"
        REAL MaxDeviation "NULL 最大偏差"
        REAL AverageTemperature "NULL 平均温度"
        INTEGER PassedCriteria "NOT NULL 0/1"
        TEXT Remarks "NOT NULL 备注"
        TEXT CreatedAt "NOT NULL 创建时间"
        REAL TempA1 "NULL 炉壁测温点"
        REAL TempA2 "NULL"
        REAL TempA3 "NULL"
        REAL TempB1 "NULL"
        REAL TempB2 "NULL"
        REAL TempB3 "NULL"
        REAL TempC1 "NULL"
        REAL TempC2 "NULL"
        REAL TempC3 "NULL"
        REAL TAvg "NULL 总均温"
        REAL TAvgAxis1 "NULL"
        REAL TAvgAxis2 "NULL"
        REAL TAvgAxis3 "NULL"
        REAL TAvgLevela "NULL"
        REAL TAvgLevelb "NULL"
        REAL TAvgLevelc "NULL"
        REAL TDevAxis1 "NULL"
        REAL TDevAxis2 "NULL"
        REAL TDevAxis3 "NULL"
        REAL TDevLevela "NULL"
        REAL TDevLevelb "NULL"
        REAL TDevLevelc "NULL"
        REAL TAvgDevAxis "NULL"
        REAL TAvgDevLevel "NULL"
        TEXT CenterTempData "NULL"
        TEXT Memo "NULL"
    }

    productmaster ||--o{ testmaster : "productid (FK约束)"
    operators ||--o{ testmaster : "username→operator (逻辑关联)"
    apparatus ||--o{ testmaster : "innernumber→apparatusid (逻辑关联)"
    apparatus ||--o{ CalibrationRecords : "apparatusid→ApparatusId (逻辑关联)"
```

---

## 表关系说明

| 关系 | 类型 | 说明 |
|------|------|------|
| `productmaster` → `testmaster` | **FK 约束** | `productid` 外键，必须先创建样品才能创建试验 |
| `operators` → `testmaster` | 逻辑关联 | `operator` 字段存 username，无数据库约束 |
| `apparatus` → `testmaster` | 逻辑关联 | `apparatusid` 字段存 innernumber，无数据库约束 |
| `apparatus` → `CalibrationRecords` | 逻辑关联 | `ApparatusId` 字段存 apparatusid，无数据库约束 |
| `sensors` | 独立表 | 无外键关联，运行时动态更新 `outputvalue` |

---

## 关键注意事项

- ⚠️ `operators` 表**无主键**，登录按 `username + pwd` 查询
- ⚠️ `CalibrationRecords` 表名**大写开头**，与其他表不同
- ⚠️ `testmaster` 联合主键 `(productid, testid)`，查询/更新必须同时提供
- ⚠️ `testmaster.flag = "10000000"` 表示试验记录已保存
- ⚠️ 温度时序数据**不入库**，存独立 CSV 文件