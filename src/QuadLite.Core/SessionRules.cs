using System;

namespace QuadLite.Core
{
    public enum SessionReject { None, FutureVersion, BadSchema, CorruptField }

    /// <summary>
    /// 会话读取规则（§5.8 / §6.4 / FR-07）：兼容 schemaVersion 1；数值越界按字段归 0
    /// （不是 clamp）；>2 整份拒读 future-version 并回落默认；单条坏项只丢该条。
    /// 原子写 / 备份 / 文件读写属 Fs 层，本类型只做纯数值判定，零 I/O。
    /// </summary>
    public static class SessionRules
    {
        public const int CurrentSchema = 2;
        public const int MinSchema = 1;

        public static bool IsSchemaSupported(int version)
        {
            return version == MinSchema || version == CurrentSchema;
        }

        public static SessionReject RejectFor(int version)
        {
            if (version > CurrentSchema) return SessionReject.FutureVersion;
            if (version < MinSchema) return SessionReject.BadSchema;
            return SessionReject.None;
        }

        public static bool FutureVersionRejectsWholeFile(SessionReject reject)
        {
            return reject == SessionReject.FutureVersion;
        }

        public static bool IsV1Compatible(int version)
        {
            return version == MinSchema;
        }

        /// <summary>按 §6.4 字面：越界归 0，不做 clamp。</summary>
        public static int NormalizeInt(int raw, int min, int max)
        {
            if (raw < min || raw > max) return 0;
            return raw;
        }

        public static LayoutMode NormalizeLayout(int raw)
        {
            if (!LayoutMath.IsKnownMode(raw)) return LayoutMode.One;
            return (LayoutMode)raw;
        }

        public static double NormalizeRatio(double raw)
        {
            if (double.IsNaN(raw) || raw < 0.0 || raw > 1.0) return 0.0;
            return raw;
        }

        public static bool NormalizeBool(int raw)
        {
            return raw == 1;
        }

        /// <summary>单条坏项不整份拒读：丢一条后其余字段照常生效。</summary>
        public static int RemainingAfterBadItem(int total, int badIndex)
        {
            if (total <= 0 || badIndex < 0 || badIndex >= total) return -1;
            return total - 1;
        }

        public static bool BadItemIsolated(int total, int badIndex)
        {
            return RemainingAfterBadItem(total, badIndex) == total - 1;
        }

        /// <summary>session.json 的规范字段；缺字段按默认值补齐，不整份拒读。</summary>
        public static string[] RequiredFields()
        {
            return new string[]
            {
                "schemaVersion", "layout", "splitRatios", "panes",
                "showHidden", "showSystem", "showExt", "showThumbs"
            };
        }
    }
}
