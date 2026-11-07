using System;

namespace QuadLite.Core
{
    /// <summary>
    /// §5.13 / §6.4 单实例三段机制：Mutex 判「能不能起」+ run.lock 查「交给谁」+
    /// WM_COPYDATA('QL') 转发路径；诊断模式不参与单实例。
    /// 纯判据，零 I/O；真实 Mutex / 消息转发由 Fs / App 宿主承担，未验证。
    /// </summary>
    public static class SingleInstanceRules
    {
        public const string Marker = "QL";
        public const int MechanismCount = 3;

        public static string[] Mechanisms()
        {
            return new string[] { "mutex", "run.lock", "WM_COPYDATA" };
        }

        public static bool ParticipatesInSingleInstance(CliMode mode)
        {
            return mode == CliMode.Normal;
        }

        public static bool DiagnosticModesBypass()
        {
            return !ParticipatesInSingleInstance(CliMode.SelfTest) &&
                   !ParticipatesInSingleInstance(CliMode.Diagnose) &&
                   !ParticipatesInSingleInstance(CliMode.Shot) &&
                   !ParticipatesInSingleInstance(CliMode.ShellMenuHost);
        }

        public static bool MarkerValid(string marker)
        {
            return marker != null && marker == Marker;
        }

        public static bool CountsMatchDeclaration()
        {
            return Mechanisms().Length == MechanismCount && MarkerValid(Marker);
        }
    }
}
