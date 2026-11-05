using System;
using System.Collections.Generic;
using System.Text;
using QuadLite.Core;

namespace QuadLite.Fs
{
    /// <summary>
    /// 剪贴板数据对象：内部表示与系统格式一一对应，不另建待办列表。
    /// 与系统的实际读写隔离在注入的 transport 里（成熟绑定接入前标「未验证」）。
    /// </summary>
    public sealed class FileClipboard : IClipboard
    {
        private readonly IClipboardTransport transport;

        public FileClipboard(IClipboardTransport transport)
        {
            this.transport = transport;
        }

        public ClipboardData Read()
        {
            if (transport == null) return null;
            ClipboardData data = transport.Read();
            return ClipboardRules.IsValid(data) ? data : null;
        }

        public bool Write(ClipboardData data)
        {
            if (!ClipboardRules.IsValid(data)) return false;
            if (transport == null) return false;
            return transport.Write(data);
        }

        /// <summary>CF_HDROP 的路径列表编码；供 transport 与测试共用，避免两套真相。</summary>
        public static string Encode(ClipboardData data)
        {
            if (data == null) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Items.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(data.Items[i].Value);
            }
            return sb.ToString();
        }

        public static ClipboardData Decode(string payload, DropEffect effect)
        {
            List<PathKey> items = new List<PathKey>();
            if (!string.IsNullOrEmpty(payload))
            {
                string[] lines = payload.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    PathKey key;
                    if (PathKey.TryNormalize(line, out key)) items.Add(key);
                }
            }
            return new ClipboardData(items, effect);
        }
    }
}
