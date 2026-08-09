using System.Collections.Generic;
using System.Linq;

namespace FieldAchievements.Data;

/// <summary>
/// 追踪的特殊地图类型。
/// </summary>
public enum TrackedMap
{
    CrescentSouth,
    CrescentNorth,
    BozjanSouthernFront,
    Zadnor
}

/// <summary>
/// 地图成就定义。
/// 相同成就的不同数量目标只保留最高数量。
/// </summary>
public sealed record MapAchievementEntry(uint AchievementId, string Name, string Description);

/// <summary>
/// 地图静态数据。
/// TerritoryType 和 Achievement 数据通过 EXDViewer MCP 查询获得。
/// </summary>
public static class MapData
{
    /// <summary>
    /// 各地图对应的 TerritoryType RowId。
    /// </summary>
    public static readonly Dictionary<TrackedMap, uint> TerritoryTypeIds = new()
    {
        [TrackedMap.CrescentSouth] = 1252u,
        [TrackedMap.CrescentNorth] = 1346u,
        [TrackedMap.BozjanSouthernFront] = 920u,
        [TrackedMap.Zadnor] = 975u,
    };

    /// <summary>
    /// 各地图显示名称。
    /// </summary>
    public static readonly Dictionary<TrackedMap, string> MapNames = new()
    {
        [TrackedMap.CrescentSouth] = "蜃景幻界新月岛 南征之章",
        [TrackedMap.CrescentNorth] = "蜃景幻界新月岛 北征之章",
        [TrackedMap.BozjanSouthernFront] = "南方博兹雅战线",
        [TrackedMap.Zadnor] = "扎杜诺尔高原",
    };

    /// <summary>
    /// 各地图简称（用于 Tab 标签等紧凑 UI）。
    /// </summary>
    public static readonly Dictionary<TrackedMap, string> MapShortNames = new()
    {
        [TrackedMap.CrescentSouth] = "南征之章",
        [TrackedMap.CrescentNorth] = "北征之章",
        [TrackedMap.BozjanSouthernFront] = "博兹雅战线",
        [TrackedMap.Zadnor] = "扎杜诺尔",
    };

    /// <summary>
    /// 各地图追踪的成就列表（已按最高数量去重）。
    /// </summary>
    public static readonly Dictionary<TrackedMap, MapAchievementEntry[]> MapAchievements = new()
    {
        [TrackedMap.CrescentSouth] =
        [
            new(3659u, "新月岛南部的记录员", "完成蜃景幻界新月岛南征之章调查笔记中的所有项目。"),
            new(3663u, "君临孤岛4", "在蜃景幻界新月岛南征之章中完成500次紧急遭遇战。"),
            new(3667u, "智者高见4", "在蜃景幻界新月岛南征之章中完成500次危命任务。"),
            new(3674u, "三角区的船医3", "在蜃景幻界新月岛南征之章相关任务中，救助500位除自身外处于无法战斗状态的玩家。"),
            new(3681u, "远海寻宝王4", "累计在蜃景幻界新月岛找到并开启场景内的宝箱5000次。"),
            new(3685u, "魔法罐的指引4", "累计在蜃景幻界新月岛找到并开启撒娇罐宝箱1000次。"),
            new(3689u, "好运胡萝卜的指引4", "累计在蜃景幻界新月岛找到并开启幸福兔宝箱1000次。"),
        ],
        [TrackedMap.CrescentNorth] =
        [
            new(3997u, "新月岛北部的记录员", "完成蜃景幻界新月岛北征之章调查笔记中的所有项目。"),
            new(4001u, "威扬孤岛4", "在蜃景幻界新月岛北征之章中完成500次紧急遭遇战。"),
            new(4005u, "豪者高杰4", "在蜃景幻界新月岛北征之章中完成500次危命任务。"),
            new(4016u, "三角区的名医3", "在蜃景幻界新月岛北征之章相关任务中，救助500位除自身外处于无法战斗状态的玩家。"),
            new(4019u, "幸运寻宝王", "累计在蜃景幻界新月岛找到并开启场景内的宝箱10000次。"),
            new(4020u, "撒娇罐最好的朋友", "累计在蜃景幻界新月岛找到并开启撒娇罐宝箱2000次。"),
            new(4021u, "满肚胡萝卜", "累计在蜃景幻界新月岛找到并开启幸福兔宝箱2000次。"),
        ],
        [TrackedMap.BozjanSouthernFront] =
        [
            new(2620u, "踏遍南方博兹雅战线", "成功探索南方博兹雅战线的所有地区。"),
            new(2670u, "南方博兹雅战线的解放者", "完成任务“圣剑天佑女王”。"),
            new(2671u, "南方博兹雅战线的记录员", "完成南方博兹雅战线战果记录中的所有项目。"),
            new(2675u, "急中生智4", "在博兹雅战线中完成500次紧急遭遇战。"),
            new(2679u, "武装冲突4", "在博兹雅战线中完成500次冲突战。"),
            new(2683u, "加百列的末日", "在南方博兹雅战线中，成功单挑加百列。"),
            new(2684u, "威镇百兽", "在南方博兹雅战线中，成功单挑兽王莱昂。"),
            new(2685u, "烈火金刚", "在南方博兹雅战线中，成功单挑铁胆狱火萨托瓦尔。"),
            new(2686u, "南方博兹雅战线的英雄", "达成“加百列的末日”“威镇百兽”“烈火金刚”三个成就。"),
            new(2693u, "博兹雅的天使3", "在天佑女王相关任务中，救助500位除自身外处于无法战斗状态的玩家。"),
        ],
        [TrackedMap.Zadnor] =
        [
            new(2891u, "踏遍扎杜诺尔高原", "成功探索扎杜诺尔高原的所有地区。"),
            new(2885u, "扎杜诺尔高原的记录员", "完成扎杜诺尔高原记录中的所有项目。"),
            new(2877u, "超级调整兵", "在扎杜诺尔高原中，成功单挑超级调整兵达波格。"),
            new(2878u, "斩将", "在扎杜诺尔高原中，成功单挑梅内纽斯·萨斯·拉那图斯。"),
            new(2879u, "桀骜不驯", "在扎杜诺尔高原中，成功单挑兽王莱昂。"),
            new(2880u, "扎杜诺尔高原的英雄", "达成“超级调整兵”“斩将”“桀骜不驯”三个成就。"),
        ],
    };

    /// <summary>
    /// 所有追踪的成就 ID 集合。
    /// </summary>
    public static readonly HashSet<uint> AllAchievementIds = MapAchievements
        .Values
        .SelectMany(entries => entries.Select(e => e.AchievementId))
        .ToHashSet();

    /// <summary>
    /// 成就 ID 到所属地图的映射。
    /// </summary>
    public static readonly Dictionary<uint, TrackedMap> AchievementToMap = MapAchievements
        .SelectMany(kv => kv.Value.Select(e => (e.AchievementId, kv.Key)))
        .ToDictionary(x => x.AchievementId, x => x.Key);

    /// <summary>
    /// 根据 TerritoryType RowId 解析对应的追踪地图。
    /// </summary>
    public static TrackedMap? ResolveMap(uint territoryType)
    {
        foreach (var (map, id) in TerritoryTypeIds)
        {
            if (id == territoryType)
                return map;
        }
        return null;
    }
}
