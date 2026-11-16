using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using QuadLite.Core;

namespace QuadLite.Fs
{
    /// <summary>
    /// R4 可静态 / 运行期判定的菜单宿主契约（零 I/O 纯函数）。
    /// 复用 B5 的 Core 规则层，不创建窗口、不枚举目录、不实例化 COM。
    /// 真实 shell 扩展与消息循环是否有效，一律由 IsRuntimeVerified() == false 标注。
    /// </summary>
    public static class MenuHostContractRules
    {
        public static string[] Steps() { return MenuHostRules.Steps(); }

        public static bool StandardSequenceDeclared()
        {
            return MenuHostRules.UsesStandardSequence(Steps()) &&
                   MenuHostRules.DefaultMode() == MenuHostMode.Native;
        }

        public static int DeclaredContextMenuMethods() { return ShellIds.ContextMenuMethods; }
        public static int DeclaredContextMenu2Methods() { return ShellIds.ContextMenu2Methods; }
        public static int DeclaredContextMenu3Methods() { return ShellIds.ContextMenu3Methods; }
        public static int DeclaredShellFolderMethods() { return ShellIds.ShellFolderMethods; }

        public static bool ForwardsAllMessages()
        {
            return MenuMessageRules.ForwardedCount() == MenuMessageRules.MessageCount &&
                   MenuMessageRules.CoversOwnerDraw() &&
                   MenuMessageRules.CoversMenuSelection() &&
                   MenuMessageRules.Handler(MenuMessage.DrawItem) == "HandleMenuMsg2";
        }

        public static bool VerbsNeverQueried()
        {
            return MenuHostRules.VerbScanCount() == 0 &&
                   MenuFusionRules.DeleteMenuCount() == 0 &&
                   MenuHostRules.MenuTreeRewriteCount() == 0;
        }

        public static bool OwnSegmentDisjoint()
        {
            return MenuInvokeRules.OwnSegmentDisjoint() &&
                   MenuFusionRules.OwnGroupStartsAboveShellCeiling();
        }

        public static bool ReleasesBothResources()
        {
            return MenuReleaseRules.OrderIsMenuThenCom(MenuReleaseRules.ReleaseOrder()) &&
                   MenuReleaseRules.NewInstancePerInvocation() &&
                   !MenuReleaseRules.CachesComPointerAcrossSessions();
        }

        public static bool UsesImmutableSnapshot()
        {
            return MenuTargetSnapshotRules.SnapshotsPerInvocation() == 1 &&
                   !MenuTargetSnapshotRules.UsesLiveSelection();
        }

        public static bool RequiresStaThread() { return true; }
        public static bool IsRuntimeVerified() { return false; }
    }

    /// <summary>结构化结果：状态栏只从这里取文案，不由「没有异常」推断成功。</summary>
    public sealed class MenuHostResult
    {
        private readonly bool executed;
        private readonly int selectedId;
        private readonly MenuDispatchKind dispatch;
        private readonly string text;

        public MenuHostResult(bool executed, int selectedId, MenuDispatchKind dispatch, string text)
        {
            this.executed = executed;
            this.selectedId = selectedId;
            this.dispatch = dispatch;
            this.text = text;
        }

        public bool Executed { get { return executed; } }
        public int SelectedId { get { return selectedId; } }
        public MenuDispatchKind Dispatch { get { return dispatch; } }
        public string Text { get { return text; } }
    }

    /// <summary>
    /// R4 系统原生右键菜单宿主（§5.2 标准四步 + IContextMenu2/3 消息转发）。
    ///
    /// 每次右键新建 IContextMenu 实例，在独立 STA 线程里执行：
    ///   GetUIObjectOf → QueryContextMenu → TrackPopupMenuEx → InvokeCommand。
    /// 自建 owner 窗口把五类菜单消息转给 HandleMenuMsg2（无 3 则退回 2）；
    /// 自有项 ID ≥ 0x8000 以一条分隔线独立成组，派发只认打开右键那一刻的
    /// MenuTarget 快照；系统项先按 [1, maxId] 硬校验再算偏移，越界丢弃。
    /// 每会话 try-finally 先撤 HMENU 再释放 COM 与 PIDL，不跨会话缓存指针。
    ///
    /// 默认 Enabled=false（沿用 R2/R3 纪律）：未显式 Enable 时只返回「未执行 /
    /// 未验证」，绝不触碰 COM；CI 与 --selftest 不创建窗口、不实例化 COM。
    /// 真实 shell 扩展、owner-draw 时序、TrackPopupMenuEx 模态循环、崩溃哨兵
    /// 与自绘兜底 UI 的运行期行为全部未验证；M1 / M2 / M3 仍不成立，
    /// 见 docs/adr/ADR-v2-014.md。
    /// </summary>
    public sealed class NativeMenuHost
    {
        private const string OwnerClassName = "QuadLiteNativeMenuOwner";

        // 静态字段保活窗口过程委托；会话内只在同一 STA 线程上读写菜单接口。
        private static readonly MenuWndProc KeepWndProc = new MenuWndProc(WndProc);
        private static IContextMenu2 active2;
        private static IContextMenu3 active3;

        private bool enabled;

        public bool Enabled { get { return enabled; } }
        public void Enable() { enabled = true; }

        /// <summary>唯一的右键入口：关闭时立即返回结构化拒绝，不进入 COM 会话。</summary>
        public MenuHostResult Show(MenuTarget snapshot, string[] ownLabels)
        {
            if (snapshot == null || snapshot.Directory == null)
            {
                return Refused("菜单未执行：目标快照 / 目录为空（未验证）");
            }
            if (!enabled)
            {
                return Refused("菜单未执行：原生菜单宿主默认关闭（未启用 / 未验证）");
            }

            MenuHostResult[] result = new MenuHostResult[1];
            result[0] = Refused("菜单未执行：STA 会话未返回结构化结果（未验证）");
            MenuTarget captured = snapshot;
            string[] labels = ownLabels;
            Thread worker = new Thread(delegate()
            {
                result[0] = RunOnSta(captured, labels);
            });
            worker.SetApartmentState(ApartmentState.STA);
            worker.IsBackground = true;
            worker.Start();
            worker.Join();
            return result[0];
        }

        private static MenuHostResult Refused(string text)
        {
            return new MenuHostResult(false, 0, MenuDispatchKind.Dropped, text);
        }

        private static MenuHostResult RunOnSta(MenuTarget snapshot, string[] ownLabels)
        {
            IntPtr hMenu = IntPtr.Zero;
            IntPtr owner = IntPtr.Zero;
            IntPtr parentPidl = IntPtr.Zero;
            IntPtr hInst = IntPtr.Zero;
            List<IntPtr> children = new List<IntPtr>();
            IShellFolder desktop = null;
            IShellFolder parent = null;
            object parentObject = null;
            object menuObject = null;
            bool classRegistered = false;
            int maxId = 0;
            try
            {
                int hr = MenuNative.SHGetDesktopFolder(out desktop);
                if (hr < 0 || desktop == null)
                {
                    return Refused("菜单未执行：桌面 shell 文件夹不可用（未验证）");
                }

                hInst = Marshal.GetHINSTANCE(typeof(MenuNative).Module);
                WndClassEx wc = new WndClassEx();
                wc.cbSize = (uint)Marshal.SizeOf(typeof(WndClassEx));
                wc.style = 0;
                wc.lpfnWndProc = KeepWndProc;
                wc.cbClsExtra = 0;
                wc.cbWndExtra = 0;
                wc.hInstance = hInst;
                wc.hIcon = IntPtr.Zero;
                wc.hCursor = IntPtr.Zero;
                wc.hbrBackground = IntPtr.Zero;
                wc.lpszMenuName = null;
                wc.lpszClassName = OwnerClassName;
                wc.hIconSm = IntPtr.Zero;
                if (MenuNative.RegisterClassEx(ref wc) == 0)
                {
                    return Refused("菜单未执行：owner 窗口类注册失败（未验证）");
                }
                classRegistered = true;
                owner = MenuNative.CreateWindowEx(0, OwnerClassName, "QuadLite menu owner",
                    MenuFlags.WsOverlapped, 0, 0, 0, 0,
                    IntPtr.Zero, IntPtr.Zero, hInst, IntPtr.Zero);
                if (owner == IntPtr.Zero)
                {
                    return Refused("菜单未执行：owner 窗口创建失败（未验证）");
                }

                uint attrs = 0;
                uint eaten = 0;
                hr = desktop.ParseDisplayName(owner, IntPtr.Zero, snapshot.Directory.Value,
                                              out eaten, out parentPidl, ref attrs);
                if (hr < 0 || parentPidl == IntPtr.Zero)
                {
                    return Refused("菜单未执行：目录 PIDL 不可用（未验证）");
                }
                Guid folderIid = new Guid(ShellIds.ShellFolderIid);
                hr = desktop.BindToObject(parentPidl, IntPtr.Zero, ref folderIid, out parentObject);
                if (hr < 0 || parentObject == null)
                {
                    return Refused("菜单未执行：父 IShellFolder 绑定失败（未验证）");
                }
                parent = parentObject as IShellFolder;
                if (parent == null)
                {
                    return Refused("菜单未执行：父 IShellFolder 转型失败（未验证）");
                }

                for (int i = 0; i < snapshot.Selection.Count; i++)
                {
                    PathKey key = snapshot.Selection[i];
                    if (key == null) continue;
                    uint childEaten = 0;
                    uint childAttrs = 0;
                    IntPtr child = IntPtr.Zero;
                    hr = parent.ParseDisplayName(owner, IntPtr.Zero, key.GetLeafName(),
                                                 out childEaten, out child, ref childAttrs);
                    if (hr < 0 || child == IntPtr.Zero)
                    {
                        return Refused("菜单未执行：选中项 PIDL 不可用（未验证）");
                    }
                    children.Add(child);
                }
                if (children.Count == 0)
                {
                    return Refused("菜单未执行：选择集为空（未验证）");
                }

                // 标准四步之一：GetUIObjectOf(IContextMenu)。每次右键新建实例。
                Guid menuIid = new Guid(ShellIds.ContextMenuIid);
                hr = parent.GetUIObjectOf(owner, (uint)children.Count, children.ToArray(),
                                          ref menuIid, IntPtr.Zero, out menuObject);
                if (hr < 0 || menuObject == null)
                {
                    return Refused("菜单未执行：系统菜单对象不可用（未验证）");
                }
                IContextMenu menu = menuObject as IContextMenu;
                IContextMenu2 menu2 = menuObject as IContextMenu2;
                IContextMenu3 menu3 = menuObject as IContextMenu3;
                if (menu == null)
                {
                    return Refused("菜单未执行：菜单接口转型失败（未验证）");
                }

                hMenu = MenuNative.CreatePopupMenu();
                if (hMenu == IntPtr.Zero)
                {
                    return Refused("菜单未执行：弹出菜单句柄不可用（未验证）");
                }

                // 标准四步之二：QueryContextMenu(idCmdFirst=1, idCmdLast=0x7FFF, CMF_*)。
                uint queryFlags = MenuFlags.CmfExplore | MenuFlags.CmfCanRename |
                                  MenuFlags.CmfItemMenu;
                int query = menu.QueryContextMenu(hMenu, 0,
                    (uint)MenuInvokeRules.IdCmdFirst, (uint)MenuInvokeRules.IdCmdLast, queryFlags);
                if (query < 0)
                {
                    return Refused("菜单未执行：系统菜单填充失败（未验证）");
                }
                maxId = MenuInvokeRules.MaxIdFromQueryResult(query & 0xFFFF);

                // 自有项：源头消重后以一条分隔线独立成组，ID ≥ 0x8000。
                string[] own = MenuFusionRules.Resolve(null, ownLabels);
                if (own.Length > 0)
                {
                    MenuNative.AppendMenu(hMenu, MenuFlags.MfSeparator, IntPtr.Zero, null);
                    for (int i = 0; i < own.Length; i++)
                    {
                        int ownId = MenuModel.OwnIdBase + i;
                        MenuNative.AppendMenu(hMenu, MenuFlags.MfString,
                                              new IntPtr(ownId), own[i]);
                    }
                }

                // 标准四步之三：TrackPopupMenuEx（owner 窗口转发五类菜单消息）。
                active2 = menu2;
                active3 = menu3;
                Point32 cursor = new Point32();
                if (!MenuNative.GetCursorPos(out cursor))
                {
                    return Refused("菜单未执行：光标位置不可用（未验证）");
                }
                MenuNative.SetForegroundWindow(owner);
                int selected = MenuNative.TrackPopupMenuEx(hMenu,
                    MenuFlags.TpmReturnCmd | MenuFlags.TpmRightButton,
                    cursor.x, cursor.y, owner, IntPtr.Zero);
                if (selected == 0)
                {
                    return new MenuHostResult(true, 0, MenuDispatchKind.Dropped,
                        "菜单已正常关闭（ESC / 点外部，运行期未验证）");
                }

                MenuDispatchKind dispatch = MenuInvokeRules.Classify(selected, maxId);
                if (dispatch == MenuDispatchKind.Own)
                {
                    if (!MenuTargetSnapshotRules.DispatchUsesSameSnapshot(snapshot, snapshot))
                    {
                        return Refused("菜单未执行：目标快照不一致（未验证）");
                    }
                    return new MenuHostResult(true, selected, dispatch,
                        "自有项已派发：id=" + selected.ToString() + "（运行期未验证）");
                }
                if (dispatch == MenuDispatchKind.Dropped ||
                    !MenuInvokeRules.InvokeIdInRange(selected, maxId))
                {
                    return new MenuHostResult(true, selected, MenuDispatchKind.Dropped,
                        "系统项越界丢弃：id=" + selected.ToString() +
                        " maxId=" + maxId.ToString());
                }

                // 标准四步之四：InvokeCommand(id - idCmdFirst)，出参先过硬校验。
                int offset = MenuInvokeRules.InvokeOffset(selected);
                InvokeCommandInfo ici = new InvokeCommandInfo();
                ici.cbSize = (uint)Marshal.SizeOf(typeof(InvokeCommandInfo));
                ici.fMask = 0;
                ici.hwnd = owner;
                ici.lpVerb = new IntPtr(offset);
                ici.lpParameters = IntPtr.Zero;
                ici.lpDirectory = IntPtr.Zero;
                ici.nShow = 1;
                ici.dwHotKey = 0;
                ici.hIcon = IntPtr.Zero;
                int invokeHr = menu.InvokeCommand(ref ici);
                return new MenuHostResult(true, selected, MenuDispatchKind.System,
                    "系统项已调用：offset=" + offset.ToString() +
                    " hr=" + invokeHr.ToString() + "（运行期未验证）");
            }
            catch (Exception)
            {
                return Refused("菜单未执行：宿主会话异常（未验证）");
            }
            finally
            {
                active2 = null;
                active3 = null;
                // 释放顺序：HMENU → owner 窗口 / 窗口类 → PIDL → COM（每会话 try-finally）。
                if (hMenu != IntPtr.Zero)
                {
                    try { MenuNative.DestroyMenu(hMenu); } catch (Exception) { }
                }
                if (owner != IntPtr.Zero)
                {
                    try { MenuNative.DestroyWindow(owner); } catch (Exception) { }
                }
                if (classRegistered)
                {
                    try { MenuNative.UnregisterClass(OwnerClassName, hInst); } catch (Exception) { }
                }
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] != IntPtr.Zero)
                    {
                        try { MenuNative.CoTaskMemFree(children[i]); } catch (Exception) { }
                    }
                }
                if (parentPidl != IntPtr.Zero)
                {
                    try { MenuNative.CoTaskMemFree(parentPidl); } catch (Exception) { }
                }
                if (menuObject != null)
                {
                    try { Marshal.ReleaseComObject(menuObject); } catch (Exception) { }
                }
                if (parentObject != null)
                {
                    try { Marshal.ReleaseComObject(parentObject); } catch (Exception) { }
                }
                if (desktop != null)
                {
                    try { Marshal.ReleaseComObject(desktop); } catch (Exception) { }
                }
            }
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            MenuMessage kind = MenuMessage.Command;
            if (!TryMapMessage(msg, out kind) || !MenuMessageRules.MustForward(kind))
            {
                return MenuNative.DefWindowProc(hWnd, msg, wParam, lParam);
            }
            IContextMenu3 cm3 = active3;
            if (cm3 != null)
            {
                IntPtr handled = IntPtr.Zero;
                int hr = cm3.HandleMenuMsg2(msg, wParam, lParam, out handled);
                if (hr >= 0) return handled;
            }
            IContextMenu2 cm2 = active2;
            if (cm2 != null) cm2.HandleMenuMsg(msg, wParam, lParam);
            return IntPtr.Zero;
        }

        private static bool TryMapMessage(uint msg, out MenuMessage kind)
        {
            kind = MenuMessage.Command;
            if (msg == MenuFlags.WmInitMenuPopup) { kind = MenuMessage.InitMenuPopup; return true; }
            if (msg == MenuFlags.WmDrawItem) { kind = MenuMessage.DrawItem; return true; }
            if (msg == MenuFlags.WmMeasureItem) { kind = MenuMessage.MeasureItem; return true; }
            if (msg == MenuFlags.WmMenuSelect) { kind = MenuMessage.MenuSelect; return true; }
            if (msg == MenuFlags.WmCommand) { kind = MenuMessage.Command; return true; }
            return false;
        }
    }
}
