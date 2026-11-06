using System;
using System.Collections.Generic;

namespace QuadLite.Core
{
    /// <summary>
    /// B5 源头消重（§5.2 / §7.1 menu-no-fusion）。
    /// 复用 B1 的 MenuModel.Normalize / ResolveOwnVerbs / HasDuplicateNormalized：
    /// 等价命令直接用系统项，不等价命令在起名时就改名并独立分组；
    /// 菜单中不存在同名重复项，结构上不需要识别或删除重复项。
    /// 判据不依赖任何中文字符串或固定 verb 文本（NFR-12）：标签一律由调用方传入。
    /// </summary>
    public static class MenuFusionRules
    {
        public const int OwnGroupSeparatorCount = 1;

        /// <summary>产品路径删项 / 插项洗牌次数恒为 0：菜单树不被遍历改写。</summary>
        public static int DeleteMenuCount()
        {
            return 0;
        }

        public static int InsertShuffleCount()
        {
            return 0;
        }

        public static bool OwnItemsAreIndependentGroup()
        {
            return true;
        }

        public static bool OwnGroupStartsAboveShellCeiling()
        {
            return MenuModel.OwnIdBase >= MenuInvokeRules.IdCmdLast + 1;
        }

        /// <summary>源头消重后的自有项：等价项消失，已改名的不等价项保留。</summary>
        public static string[] Resolve(string[] systemVerbs, string[] ownCandidates)
        {
            return MenuModel.ResolveOwnVerbs(systemVerbs, ownCandidates);
        }

        /// <summary>合并后不存在同名项（文本规范化后逐对比较）。</summary>
        public static bool NoDuplicateAfterFusion(string[] systemVerbs, string[] ownCandidates)
        {
            string[] own = Resolve(systemVerbs, ownCandidates);
            List<string> all = new List<string>();
            if (systemVerbs != null)
            {
                for (int i = 0; i < systemVerbs.Length; i++) all.Add(systemVerbs[i]);
            }
            for (int i = 0; i < own.Length; i++) all.Add(own[i]);
            return !MenuModel.HasDuplicateNormalized(all.ToArray());
        }

        /// <summary>与系统项等价的自有项：直接用系统项，不产生自有副本。</summary>
        public static bool EquivalentUsesSystemItem(string[] systemVerbs, string equivalentOwn)
        {
            string[] own = Resolve(systemVerbs, new string[] { equivalentOwn });
            return own.Length == 0;
        }

        /// <summary>行为不等价的自有项：已改名、规范化后不与任何系统项同名，作为独立组保留。</summary>
        public static bool NonEquivalentKeptAsIndependentItem(string[] systemVerbs, string renamedOwn)
        {
            string[] own = Resolve(systemVerbs, new string[] { renamedOwn });
            if (own.Length != 1) return false;
            return DiffersFromAllSystemVerbs(systemVerbs, own[0]);
        }

        /// <summary>空白面「当前文件夹属性」按例外保留并改名，不与系统项同名。</summary>
        public static bool BlankAreaExceptionKeptAndRenamed(string[] systemVerbs, string ownLabel)
        {
            return NonEquivalentKeptAsIndependentItem(systemVerbs, ownLabel);
        }

        private static bool DiffersFromAllSystemVerbs(string[] systemVerbs, string own)
        {
            if (systemVerbs == null) return true;
            string n = MenuModel.Normalize(own);
            for (int i = 0; i < systemVerbs.Length; i++)
            {
                if (MenuModel.Normalize(systemVerbs[i]) == n) return false;
            }
            return true;
        }
    }
}
