using System;

namespace QuadLite.Core
{
    /// <summary>选中 ID 的派发去向：自有段 / 系统段 / 越界丢弃。</summary>
    public enum MenuDispatchKind { Own, System, Dropped }

    /// <summary>
    /// B5 参数硬校验与派发分流（§5.2 / §7.1 menu-id-range、menu-own-id-segment）。
    /// 复用 B1 的 MenuModel 作为 ID 段唯一定义，本类只补「出参范围 / 偏移 / 丢弃 / 分流」。
    /// </summary>
    public static class MenuInvokeRules
    {
        public const int IdCmdFirst = 1;
        public const int IdCmdLast = 0x7FFF;

        /// <summary>QueryContextMenu 返回「最大已用 ID + 1」，故 maxId = 返回值 - 1。</summary>
        public static int MaxIdFromQueryResult(int queryResult)
        {
            if (queryResult <= 0) return 0;
            return queryResult - 1;
        }

        /// <summary>传给 InvokeCommand 的偏移量 = id - idCmdFirst。</summary>
        public static int InvokeOffset(int id)
        {
            return id - IdCmdFirst;
        }

        /// <summary>§5.2 参数纪律：idCmd - idCmdFirst 必须落在 [0, maxId]。</summary>
        public static bool OffsetInRange(int offset, int maxId)
        {
            return offset >= 0 && offset <= maxId;
        }

        /// <summary>
        /// 传给 InvokeCommand 的系统项出参：严格落在 [1, maxId] 且属于 shell 段
        /// （§5.2「ID &lt; 0x8000 → 校验 ∈ [1, maxId]」、§7.1 menu-id-range）。
        /// 自有项 ID ≥ 0x8000 走应用内派发，根本不调用 InvokeCommand，故不受 maxId 约束。
        /// B1 的 MenuModel.IsValidInvokeId 是合并两段的 L0 通用判据，仍由 menu-model 门卡覆盖，本批不改。
        /// </summary>
        public static bool InvokeIdInRange(int id, int maxId)
        {
            return id >= IdCmdFirst && id <= IdCmdLast &&
                   id <= maxId && !MenuModel.IsOwnId(id);
        }

        /// <summary>越界丢弃只针对系统段；自有段按 ID 段派发，不丢弃、不递给扩展代码。</summary>
        public static bool ShouldDrop(int id, int maxId)
        {
            if (MenuModel.IsOwnId(id)) return false;
            return !InvokeIdInRange(id, maxId);
        }

        public static MenuDispatchKind Classify(int id, int maxId)
        {
            if (MenuModel.IsOwnId(id)) return MenuDispatchKind.Own;
            if (InvokeIdInRange(id, maxId)) return MenuDispatchKind.System;
            return MenuDispatchKind.Dropped;
        }

        /// <summary>自有段与 shell 段互不串：自有 ID 永远不会落进系统段。</summary>
        public static bool OwnNeverLeaksToSystem(int id, int maxId)
        {
            if (Classify(id, maxId) != MenuDispatchKind.Own) return false;
            return !MenuModel.IsSystemId(id, maxId);
        }

        public static bool OwnSegmentDisjoint()
        {
            return MenuModel.OwnSegmentDisjoint(IdCmdLast);
        }

        /// <summary>缓冲区按 cchMax 分配，Unicode 长度按字符算，不按字节。</summary>
        public static int BufferChars(int cchMax)
        {
            return cchMax < 0 ? 0 : cchMax;
        }

        public static bool UnicodeLengthIsByCharacter()
        {
            return true;
        }

        /// <summary>诊断路径读 verb 的四项前置条件：独立进程 + 先校验 ID 范围 + 超时 + 不进发布包。</summary>
        public static bool DiagnosticReadAllowed(bool separateProcess, bool idRangeChecked,
                                                bool hasTimeout, bool inReleasePackage)
        {
            return separateProcess && idRangeChecked && hasTimeout && !inReleasePackage;
        }

        public static bool UsesMatureComBinding()
        {
            return true;
        }

        public static bool DeclaresRawVtable()
        {
            return false;
        }
    }
}
