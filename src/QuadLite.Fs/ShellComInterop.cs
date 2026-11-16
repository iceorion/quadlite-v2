using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using QuadLite.Core;

namespace QuadLite.Fs
{
    /// <summary>
    /// R2 托管 COM 绑定：产品程序集里唯一允许出现 COM 声明的文件。
    ///
    /// 所有 IID / CLSID 与 vtable 方法顺序逐字取自本机 Windows SDK 10.0.19041.0：
    ///   um/ShObjIdl_core.h（IFileOperation / IFileOperationProgressSink / IShellItem）
    ///   um/ShObjIdl_core.idl（CLSID_FileOperation）
    ///   um/shellapi.h（FOF_* flags）与 um/ShObjIdl_core.h（FOFX_ADDUNDORECORD）
    ///
    /// 规格 §5.2 要求「COM 声明使用成熟绑定，不手写 vtable」。零 NuGet + net48 +
    /// 单 exe 约束下 .NET Framework 未提供公开的托管 IFileOperation 绑定，因此本文件
    /// 采用与 SDK 头文件逐字对应的托管声明，是明确的规格偏差，登记在 ADR-v2-012：
    /// 不做函数指针运算、不使用非安全代码块、不改写 vtable，全部方法保留 PreserveSig 的
    /// HRESULT 返回值，参数按 SDK 顺序硬校验。
    ///
    /// 运行期行为一律标「未验证」：CI 只编译本文件，不实例化 COM 对象，也不调用
    /// PerformOperations；真实 shell 搬运是否可用需发布线 H1–H6 在真机取证。
    /// </summary>
    internal static class ShellIds
    {
        public const string FileOperationClsid = "3ad05575-8857-4850-9277-11b85bdb8e09";
        public const string FileOperationIid = "947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8";
        public const string ProgressSinkIid = "04b0f1a7-9490-44bc-96e1-4296a31252e2";
        public const string ShellItemIid = "43826d1e-e718-42ee-bc55-a1e261c37bfe";

        public const int FileOperationMethods = 21;
        public const int ProgressSinkMethods = 16;
        public const int ShellItemMethods = 5;

        // R4：um/ShObjIdl_core.h 的 MIDL_INTERFACE 权威值与 SDK vtable 槽位数
        // （IContextMenu 3 / 2 为 4 / 3 为 5；IShellFolder 为 IUnknown 之后的 10 个）。
        public const string ContextMenuIid = "000214e4-0000-0000-c000-000000000046";
        public const string ContextMenu2Iid = "000214f4-0000-0000-c000-000000000046";
        public const string ContextMenu3Iid = "bcfce0a0-ec17-11d0-8d10-00a0c90f2719";
        public const string ShellFolderIid = "000214e6-0000-0000-c000-000000000046";
        public const int ContextMenuMethods = 3;
        public const int ContextMenu2Methods = 4;
        public const int ContextMenu3Methods = 5;
        public const int ShellFolderMethods = 10;
    }

    /// <summary>SDK 权威 flags；NoUi 阻止系统模态进度框 / 冲突框抽走消息泵。</summary>
    internal static class ShellFlags
    {
        public const uint Silent = 0x0004;
        public const uint NoConfirmation = 0x0010;
        public const uint NoConfirmMkdir = 0x0200;
        public const uint NoErrorUi = 0x0400;
        public const uint AllowUndo = 0x0040;
        public const uint WantNukeWarning = 0x4000;
        public const uint AddUndoRecord = 0x20000000;
        public const uint NoUi = Silent | NoConfirmation | NoErrorUi | NoConfirmMkdir;
    }

    /// <summary>IShellItem：5 个方法，顺序取自 SDK。仅用 GetDisplayName 取真实路径。</summary>
    [ComImport, Guid(ShellIds.ShellItemIid), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItem
    {
        [PreserveSig] int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        [PreserveSig] int GetParent(out IntPtr ppsi);
        [PreserveSig] int GetDisplayName(uint sigdnName, out IntPtr ppszName);
        [PreserveSig] int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        [PreserveSig] int Compare(IntPtr psi, uint hint, out int piOrder);
    }

    /// <summary>IFileOperationProgressSink：16 个方法，顺序取自 SDK。</summary>
    [ComImport, Guid(ShellIds.ProgressSinkIid), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileOperationProgressSink
    {
        [PreserveSig] int StartOperations();
        [PreserveSig] int FinishOperations(int hrResult);
        [PreserveSig] int PreRenameItem(uint dwFlags, IShellItem psiItem,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] int PostRenameItem(uint dwFlags, IShellItem psiItem,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrRename, IShellItem psiNewlyCreated);
        [PreserveSig] int PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] int PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrMove, IShellItem psiNewlyCreated);
        [PreserveSig] int PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] int PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrCopy, IShellItem psiNewlyCreated);
        [PreserveSig] int PreDeleteItem(uint dwFlags, IShellItem psiItem);
        [PreserveSig] int PostDeleteItem(uint dwFlags, IShellItem psiItem, int hrDelete, IShellItem psiNewlyCreated);
        [PreserveSig] int PreNewItem(uint dwFlags, IShellItem psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] int PostNewItem(uint dwFlags, IShellItem psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName,
            [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName,
            uint dwFileAttributes, int hrNew, IShellItem psiNewItem);
        [PreserveSig] int UpdateProgress(uint iWorkTotal, uint iWorkSoFar);
        [PreserveSig] int ResetTimer();
        [PreserveSig] int PauseTimer();
        [PreserveSig] int ResumeTimer();
    }

    /// <summary>IFileOperation：21 个方法，顺序取自 SDK；每次操作新建实例、flags 实例级。</summary>
    [ComImport, Guid(ShellIds.FileOperationIid), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileOperation
    {
        [PreserveSig] int Advise(IFileOperationProgressSink pfops, out uint pdwCookie);
        [PreserveSig] int Unadvise(uint dwCookie);
        [PreserveSig] int SetOperationFlags(uint dwOperationFlags);
        [PreserveSig] int SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);
        [PreserveSig] int SetProgressDialog(IntPtr popd);
        [PreserveSig] int SetProperties(IntPtr pproparray);
        [PreserveSig] int SetOwnerWindow(IntPtr hwndOwner);
        [PreserveSig] int ApplyPropertiesToItem(IShellItem psiItem);
        [PreserveSig] int ApplyPropertiesToItems(IntPtr punkItems);
        [PreserveSig] int RenameItem(IShellItem psiItem,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink pfopsItem);
        [PreserveSig] int RenameItems(IntPtr pUnkItems,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] int MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink pfopsItem);
        [PreserveSig] int MoveItems(IntPtr punkItems, IShellItem psiDestinationFolder);
        [PreserveSig] int CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszCopyName, IFileOperationProgressSink pfopsItem);
        [PreserveSig] int CopyItems(IntPtr punkItems, IShellItem psiDestinationFolder);
        [PreserveSig] int DeleteItem(IShellItem psiItem, IFileOperationProgressSink pfopsItem);
        [PreserveSig] int DeleteItems(IntPtr punkItems);
        [PreserveSig] int NewItem(IShellItem psiDestinationFolder, uint dwFileAttributes,
            [MarshalAs(UnmanagedType.LPWStr)] string pszName,
            [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, IFileOperationProgressSink pfopsItem);
        [PreserveSig] int PerformOperations();
        [PreserveSig] int GetAnyOperationsAborted(out int pfAnyOperationsAborted);
    }

    internal static class ShellNative
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        internal static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
    }

    internal static class ShellItems
    {
        /// <summary>SIGDN_FILESYSPATH = 0x80058000（um/ShObjIdl_core.h）。</summary>
        internal const uint SigdnFilesysPath = 0x80058000;

        internal static IShellItem Create(string path)
        {
            Guid iid = new Guid(ShellIds.ShellItemIid);
            IShellItem item;
            int hr = ShellNative.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out item);
            if (hr < 0 || item == null) throw new InvalidOperationException("shell item unavailable");
            return item;
        }

        /// <summary>PostDeleteItem 用它取回收站中的真实路径；拿不到则返回 null，该步不入撤销栈。</summary>
        internal static string FileSystemPath(IShellItem item)
        {
            if (item == null) return null;
            IntPtr p = IntPtr.Zero;
            int hr = item.GetDisplayName(SigdnFilesysPath, out p);
            if (hr < 0 || p == IntPtr.Zero) return null;
            string text = Marshal.PtrToStringUni(p);
            Marshal.FreeCoTaskMem(p);
            return text;
        }
    }

    /// <summary>
    /// 把 shell 的逐项回调翻译成 Core 的 IProgressSink / OpItem。
    /// CallbackCount 以回调次数为准；越界索引丢弃，不做参数越界推断。
    /// </summary>
    internal sealed class ProgressSinkBridge : IFileOperationProgressSink
    {
        private readonly IProgressSink sink;
        private readonly IReadOnlyList<PathKey> sources;
        private readonly List<OpItem> items;
        private int callbackCount;

        public ProgressSinkBridge(IProgressSink sink, IReadOnlyList<PathKey> sources)
        {
            this.sink = sink;
            this.sources = sources;
            this.items = new List<OpItem>();
        }

        public int CallbackCount { get { return callbackCount; } }
        public List<OpItem> Items { get { return items; } }

        private int Post(int hr, IShellItem newlyCreated, string reason)
        {
            int index = callbackCount;
            callbackCount++;
            if (sources == null || index >= sources.Count) return 0;
            PathKey actual = null;
            string path = ShellItems.FileSystemPath(newlyCreated);
            PathKey normalized;
            if (path != null && PathKey.TryNormalize(path, out normalized)) actual = normalized;
            OpItem item = new OpItem(sources[index], actual, hr, reason);
            items.Add(item);
            if (sink != null) sink.OnItem(index, hr, actual, reason);
            return 0;
        }

        public int StartOperations() { return 0; }
        public int FinishOperations(int hrResult) { return 0; }
        public int PreRenameItem(uint dwFlags, IShellItem psiItem, string pszNewName) { return 0; }
        public int PostRenameItem(uint dwFlags, IShellItem psiItem, string pszNewName,
                                  int hrRename, IShellItem psiNewlyCreated)
        { return Post(hrRename, psiNewlyCreated, "rename"); }

        public int PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder,
                               string pszNewName) { return 0; }
        public int PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder,
                                string pszNewName, int hrMove, IShellItem psiNewlyCreated)
        { return Post(hrMove, psiNewlyCreated, "move"); }

        public int PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder,
                               string pszNewName) { return 0; }
        public int PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder,
                                string pszNewName, int hrCopy, IShellItem psiNewlyCreated)
        { return Post(hrCopy, psiNewlyCreated, "copy"); }

        public int PreDeleteItem(uint dwFlags, IShellItem psiItem) { return 0; }
        public int PostDeleteItem(uint dwFlags, IShellItem psiItem, int hrDelete,
                                  IShellItem psiNewlyCreated)
        { return Post(hrDelete, psiNewlyCreated, "recycle-path"); }

        public int PreNewItem(uint dwFlags, IShellItem psiDestinationFolder, string pszNewName) { return 0; }
        public int PostNewItem(uint dwFlags, IShellItem psiDestinationFolder, string pszNewName,
                               string pszTemplateName, uint dwFileAttributes, int hrNew,
                               IShellItem psiNewItem)
        { return Post(hrNew, psiNewItem, "new-item"); }

        public int UpdateProgress(uint iWorkTotal, uint iWorkSoFar) { return 0; }
        public int ResetTimer() { return 0; }
        public int PauseTimer() { return 0; }
        public int ResumeTimer() { return 0; }
    }

    // ============ R4：系统原生右键菜单 COM 宿主声明（唯一白名单） ============
    // 全部 GUID 与 vtable 槽位顺序逐字取自 Windows SDK 10.0.19041.0 的
    // um/ShObjIdl_core.h；WM_* / TPM_* / MF_* / CMF_* 取自 um/winuser.h 与同头文件。
    // 三个菜单接口各自列出 IUnknown 之后的全部槽位，CLR 按声明顺序映射 vtable；
    // 第三槽只是 SDK 规定的声明占位，产品路径从不调用它（B-04 / menu-no-verb-scan）。
    // 不写函数指针、不改写 vtable、不使用非安全代码块；§5.2「成熟绑定」偏差
    // 继续沿用 ADR-v2-012 的登记。运行期一律标「未验证」。

    /// <summary>SDK 权威 flags：菜单查询、TrackPopupMenuEx、菜单消息与自有项分隔。</summary>
    internal static class MenuFlags
    {
        public const uint CmfNormal = 0x00000000;
        public const uint CmfExplore = 0x00000004;
        public const uint CmfCanRename = 0x00000010;
        public const uint CmfItemMenu = 0x00000080;
        public const uint CmfExtendedVerbs = 0x00000100;
        public const uint TpmReturnCmd = 0x0100;
        public const uint TpmRightButton = 0x0002;
        public const uint TpmLeftAlign = 0x0000;
        public const uint MfSeparator = 0x0800;
        public const uint MfString = 0x0000;
        public const uint WmDrawItem = 0x002B;
        public const uint WmMeasureItem = 0x002C;
        public const uint WmCommand = 0x0111;
        public const uint WmInitMenuPopup = 0x0117;
        public const uint WmMenuSelect = 0x011F;
        public const uint WsOverlapped = 0x00000000;
    }

    /// <summary>IContextMenu：IUnknown 之后 3 个槽位，顺序取自 SDK。</summary>
    [ComImport, Guid(ShellIds.ContextMenuIid), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IContextMenu
    {
        [PreserveSig] int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst,
            uint idCmdLast, uint uFlags);
        [PreserveSig] int InvokeCommand(ref InvokeCommandInfo pici);
        [PreserveSig] int GetCommandString(IntPtr idCmd, uint uType, IntPtr pReserved,
            IntPtr pszName, uint cchMax);
    }

    /// <summary>IContextMenu2：在 3 个槽位之后追加 HandleMenuMsg。</summary>
    [ComImport, Guid(ShellIds.ContextMenu2Iid), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IContextMenu2
    {
        [PreserveSig] int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst,
            uint idCmdLast, uint uFlags);
        [PreserveSig] int InvokeCommand(ref InvokeCommandInfo pici);
        [PreserveSig] int GetCommandString(IntPtr idCmd, uint uType, IntPtr pReserved,
            IntPtr pszName, uint cchMax);
        [PreserveSig] int HandleMenuMsg(uint uMsg, IntPtr wParam, IntPtr lParam);
    }

    /// <summary>IContextMenu3：再追加 HandleMenuMsg2，owner 窗口优先走它。</summary>
    [ComImport, Guid(ShellIds.ContextMenu3Iid), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IContextMenu3
    {
        [PreserveSig] int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst,
            uint idCmdLast, uint uFlags);
        [PreserveSig] int InvokeCommand(ref InvokeCommandInfo pici);
        [PreserveSig] int GetCommandString(IntPtr idCmd, uint uType, IntPtr pReserved,
            IntPtr pszName, uint cchMax);
        [PreserveSig] int HandleMenuMsg(uint uMsg, IntPtr wParam, IntPtr lParam);
        [PreserveSig] int HandleMenuMsg2(uint uMsg, IntPtr wParam, IntPtr lParam, out IntPtr plResult);
    }

    /// <summary>IShellFolder：IUnknown 之后 10 个槽位，供 GetUIObjectOf 取菜单对象。</summary>
    [ComImport, Guid(ShellIds.ShellFolderIid), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellFolder
    {
        [PreserveSig] int ParseDisplayName(IntPtr hwnd, IntPtr pbc,
            [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName,
            out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
        [PreserveSig] int EnumObjects(IntPtr hwnd, uint grfFlags, out IntPtr ppenumIDList);
        [PreserveSig] int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out object ppv);
        [PreserveSig] int BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
        [PreserveSig] int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
        [PreserveSig] int CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
        [PreserveSig] int GetAttributesOf(uint cidl, IntPtr apidl, ref uint rgfInOut);
        [PreserveSig] int GetUIObjectOf(IntPtr hwndOwner, uint cidl,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] apidl,
            ref Guid riid, IntPtr rgfReserved,
            [MarshalAs(UnmanagedType.Interface)] out object ppv);
        [PreserveSig] int GetDisplayNameOf(IntPtr pidl, uint uFlags, out IntPtr pName);
        [PreserveSig] int SetNameOf(IntPtr hwnd, IntPtr pidl,
            [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);
    }

    /// <summary>CMINVOKECOMMANDINFO：SDK 的 9 字段结构，lpVerb 用 MAKEINTRESOURCE 偏移。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct InvokeCommandInfo
    {
        public uint cbSize;
        public uint fMask;
        public IntPtr hwnd;
        public IntPtr lpVerb;
        public IntPtr lpParameters;
        public IntPtr lpDirectory;
        public int nShow;
        public uint dwHotKey;
        public IntPtr hIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point32
    {
        public int x;
        public int y;
    }

    internal delegate IntPtr MenuWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>WNDCLASSEX：窗口过程用委托字段由运行时封送，不取函数指针。</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WndClassEx
    {
        public uint cbSize;
        public uint style;
        [MarshalAs(UnmanagedType.FunctionPtr)] public MenuWndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    /// <summary>菜单会话需要的 Win32 入口，全部集中在本白名单文件。</summary>
    internal static class MenuNative
    {
        [DllImport("shell32.dll")]
        internal static extern int SHGetDesktopFolder(
            [MarshalAs(UnmanagedType.Interface)] out IShellFolder pshf);

        [DllImport("ole32.dll")]
        internal static extern void CoTaskMemFree(IntPtr pv);

        [DllImport("user32.dll")]
        internal static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "AppendMenuW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem,
            [MarshalAs(UnmanagedType.LPWStr)] string lpNewItem);

        [DllImport("user32.dll")]
        internal static extern int TrackPopupMenuEx(IntPtr hMenu, uint fuFlags, int x, int y,
            IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegisterClassExW")]
        internal static extern ushort RegisterClassEx(ref WndClassEx wc);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateWindowExW")]
        internal static extern IntPtr CreateWindowEx(uint dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "UnregisterClassW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterClass(
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        internal static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out Point32 lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
