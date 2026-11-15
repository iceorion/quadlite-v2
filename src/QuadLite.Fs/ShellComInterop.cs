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
}
