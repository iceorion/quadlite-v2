using System;

namespace QuadLite.Core
{
    /// <summary>需要由 owner 窗口转发的菜单消息（§5.2 / §7.1 menu-msg-forward）。</summary>
    public enum MenuMessage
    {
        InitMenuPopup, DrawItem, MeasureItem, MenuSelect, Command
    }

    /// <summary>
    /// 消息转发规则：TrackPopupMenuEx 期间由自建 owner 窗口把消息交给 IContextMenu2/3。
    /// 覆盖 owner-draw（绘制 / 量测）与菜单选择（弹出 / 高亮 / 命令），零 I/O。
    /// </summary>
    public static class MenuMessageRules
    {
        public const int MessageCount = 5;

        /// <summary>五类消息一律转发；未知消息不转发、不吞给扩展代码。</summary>
        public static bool MustForward(MenuMessage message)
        {
            return message >= MenuMessage.InitMenuPopup && message <= MenuMessage.Command;
        }

        public static bool MustForwardUnknown(int rawCode)
        {
            return false;
        }

        public static string Handler(MenuMessage message)
        {
            return "HandleMenuMsg2";
        }

        public static bool CoversOwnerDraw()
        {
            return MustForward(MenuMessage.DrawItem) && MustForward(MenuMessage.MeasureItem);
        }

        public static bool CoversMenuSelection()
        {
            return MustForward(MenuMessage.InitMenuPopup) && MustForward(MenuMessage.MenuSelect);
        }

        public static bool CoversCommand()
        {
            return MustForward(MenuMessage.Command);
        }

        public static int ForwardedCount()
        {
            int n = 0;
            for (int i = 0; i < MessageCount; i++)
            {
                if (MustForward((MenuMessage)i)) n++;
            }
            return n;
        }

        public static bool UsesOwnerWindow()
        {
            return true;
        }

        public static bool ForwardsToContextMenu2Or3()
        {
            return true;
        }

        /// <summary>转发发生在菜单会话内；会话结束不再持有扩展接口。</summary>
        public static bool ForwardsOnlyDuringTracking()
        {
            return true;
        }
    }
}
