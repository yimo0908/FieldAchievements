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
/// 地图静态数据。
/// 成就 ID 与 TerritoryType 经 EXDViewer MCP 查询核实；
/// 成就名称与描述不在此硬编码，运行时按成就 ID 从 Lumina 游戏数据获取。
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
    /// 各地图追踪的成就 ID 列表（名称与描述运行时自 Lumina 获取）。
    /// </summary>
    public static readonly Dictionary<TrackedMap, uint[]> MapAchievements = new()
    {
        [TrackedMap.CrescentSouth] =
        [
            3659u,
            3663u,
            3667u,
            3671u,
            3674u,
            4018u,
            4019u,
            4020u,
            4021u,
        ],
        [TrackedMap.CrescentNorth] =
        [
            3997u,
            4001u,
            4005u,
            4009u,
            4013u,
            4016u,
            4018u,
            4019u,
            4020u,
            4021u,
            4022u,
        ],
        [TrackedMap.BozjanSouthernFront] =
        [
            2671u,
            2675u,
            2679u,
            2682u,
            2683u,
            2684u,
            2685u,
            2686u,
            2690u,
            2693u,
            2764u,
            2767u,
            2882u,
        ],
        [TrackedMap.Zadnor] =
        [
            2876u,
            2877u,
            2878u,
            2879u,
            2880u,
            2882u,
            2885u,
        ],
    };

    /// <summary>
    /// 所有追踪的成就 ID 集合。
    /// </summary>
    public static readonly HashSet<uint> AllAchievementIds = MapAchievements
        .Values
        .SelectMany(ids => ids)
        .ToHashSet();

    /// <summary>
    /// 成就 ID 到所属地图的映射（同一成就可能属于多个地图）。
    /// </summary>
    public static readonly ILookup<uint, TrackedMap> AchievementToMaps = MapAchievements
        .SelectMany(kv => kv.Value.Select(id => (id, kv.Key)))
        .ToLookup(x => x.id, x => x.Key);

    /// <summary>
    /// 基于 buff 层数计算进度的成就规则。
    /// 当玩家在指定地图内时，通过 buff 层数代替成就系统获取实时进度。
    /// </summary>
    public sealed record BuffProgressRule(uint[] BuffIds, uint Max, TrackedMap[] ApplicableMaps);

    /// <summary>
    /// 使用 buff 层数计算进度的成就及其规则。
    /// </summary>
    public static readonly Dictionary<uint, BuffProgressRule> BuffProgressRules = new()
    {
        [4018u] = new([4226u], 24u, [TrackedMap.CrescentSouth, TrackedMap.CrescentNorth]),
        [2882u] = new([2625u, 2626u, 2627u], 30u, [TrackedMap.BozjanSouthernFront, TrackedMap.Zadnor]),
    };

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
