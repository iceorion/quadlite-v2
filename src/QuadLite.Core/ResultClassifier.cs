using System;

namespace QuadLite.Core
{
    /// <summary>把结构化结果与预检映射成 §5.12 的错误分类；分母只用 CallbackCount。</summary>
    public static class ResultClassifier
    {
        public static ErrorClass FromHr(int hr)
        {
            int win32 = hr & 0xFFFF;
            if (win32 == 5) return ErrorClass.AccessDenied;
            if (win32 == 32) return ErrorClass.SharingViolation;
            if (win32 == 80) return ErrorClass.NameConflict;
            if (win32 == 206) return ErrorClass.PathTooLong;
            return ErrorClass.PartialSuccess;
        }

        public static ErrorClass Classify(OpRecord record, PreflightResult preflight)
        {
            if (record == null) return ErrorClass.ShellUnavailable;
            if (record.Cancelled) return ErrorClass.Cancelled;
            if (preflight != null)
            {
                if (preflight.IsReadOnly) return ErrorClass.ShellUnavailable;
                if (preflight.IsLocked) return ErrorClass.SharingViolation;
                if (preflight.HasConflict) return ErrorClass.NameConflict;
                if (preflight.Error != ErrorClass.None) return preflight.Error;
            }
            int success = OpRecordRules.SuccessCount(record);
            int denominator = OpRecordRules.Denominator(record);
            if (denominator > 0 && success < denominator)
            {
                for (int i = 0; i < record.Items.Count; i++)
                {
                    if (!OpRecordRules.IsSuccess(record.Items[i]))
                    {
                        return FromHr(record.Items[i].Hr);
                    }
                }
                return ErrorClass.PartialSuccess;
            }
            return ErrorClass.None;
        }

        public static bool IsPartialSuccess(OpRecord record)
        {
            if (record == null) return false;
            int success = OpRecordRules.SuccessCount(record);
            int denominator = OpRecordRules.Denominator(record);
            return denominator > 0 && success > 0 && success < denominator;
        }
    }
}
