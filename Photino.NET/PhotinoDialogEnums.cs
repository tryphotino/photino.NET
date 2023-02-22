using System;

namespace PhotinoNET;

public enum PhotinoDialogButtons
{
    Ok,
    OkCancel,
    YesNo,
    YesNoCancel,
    RetryCancel,
    AbortRetryIgnore
}

public enum PhotinoDialogIcon
{
    Info,
    Warning,
    Error,
    Question
}

[Flags]
public enum PhotinoDialogOptions : byte
{
    None = 0,
    MultiSelect = 0x1,
    ForceOverwrite = 0x2,
    DisableCreateFolder = 0x4
}

public enum PhotinoDialogResult
{
    Cancel = -1,
    Ok,
    Yes,
    No,
    Abort,
    Retry,
    Ignore
}