namespace Photino.NET;

/// <summary>
/// Provides data for a JavaScript alert dialog request.
/// </summary>
public class InputDialogEventArgs : EventArgs
{
    internal InputDialogEventArgs(PhotinoWindow window, string message)
    {
        Window = window;
        Message = message ?? string.Empty;
    }

    /// <summary>
    /// Gets the Photino window that requested the dialog.
    /// </summary>
    public PhotinoWindow Window { get; }

    /// <summary>
    /// Gets the message displayed by the JavaScript dialog.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets whether the dialog request was handled by the event handler.
    /// </summary>
    public bool Handled { get; private set; }

    internal bool Dismissed { get; private set; }

    /// <summary>
    /// Dismisses the requested dialog.
    /// </summary>
    public void DismissDialog()
    {
        Handled = true;
        Dismissed = true;
    }

    protected void MarkHandled()
    {
        Handled = true;
        Dismissed = false;
    }
}

/// <summary>
/// Provides data for a JavaScript confirm dialog request.
/// </summary>
public sealed class ConfirmInputDialogEventArgs : InputDialogEventArgs
{
    internal ConfirmInputDialogEventArgs(PhotinoWindow window, string message)
        : base(window, message)
    {
    }

    internal bool Confirmation { get; private set; }

    /// <summary>
    /// Sets the confirmation result returned to JavaScript.
    /// </summary>
    /// <param name="confirmation">The value returned by the JavaScript confirm dialog.</param>
    public void SetDialogConfirmation(bool confirmation)
    {
        Confirmation = confirmation;
        MarkHandled();
    }
}

/// <summary>
/// Provides data for a JavaScript prompt dialog request.
/// </summary>
public sealed class PromptInputDialogEventArgs : InputDialogEventArgs
{
    internal PromptInputDialogEventArgs(PhotinoWindow window, string message, string defaultInput)
        : base(window, message)
    {
        DefaultInput = defaultInput ?? string.Empty;
    }

    /// <summary>
    /// Gets the default prompt input supplied by JavaScript.
    /// </summary>
    public string DefaultInput { get; }

    internal string Input { get; private set; }

    /// <summary>
    /// Sets the prompt input returned to JavaScript.
    /// </summary>
    /// <param name="input">The input value returned by the JavaScript prompt dialog.</param>
    public void SetDialogInput(string input)
    {
        Input = input ?? string.Empty;
        MarkHandled();
    }
}
