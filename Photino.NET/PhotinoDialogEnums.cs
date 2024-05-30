using System;

namespace Photino.NET;

/// <summary>
/// Represents the types of buttons that can be displayed in a Photino-dialog.
/// </summary>
public enum PhotinoDialogButtons
{
    /// <summary>
    /// Represents a dialog with an OK button.
    /// </summary>
    Ok,

    /// <summary>
    /// Represents a dialog with OK and Cancel buttons.
    /// </summary>
    OkCancel,

    /// <summary>
    /// Represents a dialog with Yes and No buttons.
    /// </summary>
    YesNo,

    /// <summary>
    /// Represents a dialog with Yes, No, and Cancel buttons.
    /// </summary>
    YesNoCancel,

    /// <summary>
    /// Represents a dialog with Retry and Cancel buttons.
    /// </summary>
    RetryCancel,

    /// <summary>
    /// Represents a dialog with Abort, Retry, and Ignore buttons.
    /// </summary>
    AbortRetryIgnore
}

/// <summary> 
/// Represents the icons that can be used in a Photino dialog. 
/// </summary> 
public enum PhotinoDialogIcon
{
    /// <summary> 
    /// An information icon. 
    /// </summary> 
    Info,

    /// <summary> 
    /// A warning icon. 
    /// </summary>
    Warning,

    /// <summary> 
    /// An error icon. 
    /// </summary> 
    Error,

    /// <summary> 
    /// A question icon. 
    /// </summary> 
    Question
}

/// <summary>
/// Defines options for a dialog
/// </summary>
[Flags]
public enum PhotinoDialogOptions : byte
{
    /// <summary>
    /// Represents no options for the dialog.
    /// </summary>
    None = 0,

    /// <summary>
    /// Enables multi-selection in the dialog.
    /// </summary>
    MultiSelect = 0x1,

    /// <summary>
    /// Forces an overwrite of existing files without prompting the user in the dialog.
    /// </summary>
    ForceOverwrite = 0x2,

    /// <summary>
    /// Disables the capability of creating folders via the dialog.
    /// </summary>
    DisableCreateFolder = 0x4
}

/// <summary>
/// Represents the result of a dialog interaction.
/// </summary>
public enum PhotinoDialogResult
{
    /// <summary>
    /// Represents the "Cancel" result of a dialog.
    /// </summary>
    Cancel = -1,

    /// <summary>
    /// Represents the "Ok" result of a dialog.
    /// </summary>
    Ok,

    /// <summary>
    /// Represents the "Yes" result of a dialog.
    /// </summary>
    Yes,

    /// <summary>
    /// Represents the "No" result of a dialog.
    /// </summary>
    No,

    /// <summary>
    /// Represents the "Abort" result of a dialog.
    /// </summary>
    Abort,

    /// <summary>
    /// Represents the "Retry" result of a dialog.
    /// </summary>
    Retry,

    /// <summary>
    /// Represents the "Ignore" result of a dialog.
    /// </summary>
    Ignore
}