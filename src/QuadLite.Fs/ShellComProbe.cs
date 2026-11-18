using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace QuadLite.Fs
{
    /// <summary>
    /// R6 真机证据批次（阶段 0）：最小 COM 实例化探针。
    ///
    /// 只在显式 --diagnose 时由产品入口调用；不参与 Normal 路径，不创建窗口，
    /// 不枚举目录，不实例化菜单 / 图标宿主，不调用 PerformOperations，不碰任何文件。
    /// 目的只有一个：回答「CI Session 0 里 shell COM 能否实例化」这个决定后续
    /// 所有真机验证形态的问题。结论由 stdout 如实输出，不预先写成「已验证」。
    ///
    /// 零新增 COM 声明：CLSID / IID 全部复用唯一白名单 ShellComInterop.cs 的
    /// ShellIds；本文件只做 CoInitializeEx + Activator.CreateInstance + 释放，
    /// 是 COM 调用点，不是 COM 声明点（分层口径见 tools/com_policy.py）。
    /// </summary>
    public sealed class ShellComProbe
    {
        private const int RpcErrorChangedMode = unchecked((int)0x80010106);

        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();

        /// <summary>以独立 STA 跑探针，返回可机器判定的结构化文本行。</summary>
        public static string RunInstantiationProbe()
        {
            ProbeResult result = new ProbeResult();
            Thread worker = new Thread(delegate() { Execute(result); });
            worker.SetApartmentState(ApartmentState.STA);
            worker.IsBackground = true;
            worker.Start();
            worker.Join();
            return result.ToText();
        }

        private static void Execute(ProbeResult result)
        {
            int coInitHr = -1;
            bool initialized = false;
            try
            {
                // COINIT_APARTMENTTHREADED = 0x2
                coInitHr = CoInitializeEx(IntPtr.Zero, 0x2);
                initialized = coInitHr >= 0 || coInitHr == RpcErrorChangedMode;
                result.CoInitHr = coInitHr;

                if (!initialized)
                {
                    result.Instantiated = false;
                    result.Detail = "CoInitializeEx failed";
                    return;
                }

                object created = null;
                try
                {
                    created = Activator.CreateInstance(
                        Type.GetTypeFromCLSID(new Guid(ShellIds.FileOperationClsid)));
                    result.Instantiated = created != null;
                    result.Detail = created == null ? "CreateInstance returned null" : "ok";
                }
                catch (Exception ex)
                {
                    result.Instantiated = false;
                    result.Detail = ex.GetType().Name;
                }
                finally
                {
                    if (created != null)
                    {
                        try { Marshal.ReleaseComObject(created); }
                        catch (Exception) { result.ReleaseFailed = true; }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Instantiated = false;
                result.Detail = ex.GetType().Name;
            }
            finally
            {
                if (initialized && coInitHr >= 0)
                {
                    CoUninitialize();
                }
            }
        }

        private sealed class ProbeResult
        {
            public int CoInitHr;
            public bool Instantiated;
            public bool ReleaseFailed;
            public string Detail;

            public ProbeResult()
            {
                CoInitHr = -1;
                Instantiated = false;
                ReleaseFailed = false;
                Detail = "";
            }

            public string ToText()
            {
                return "probe-com: coinit-hr=" + CoInitHr.ToString() +
                       " instantiated=" + (Instantiated ? "true" : "false") +
                       " release-failed=" + (ReleaseFailed ? "true" : "false") +
                       " detail=" + Detail +
                       " clsid=" + ShellIds.FileOperationClsid;
            }
        }
    }
}
