using System;

namespace Photino.NET;

/// <summary>
/// Represents the edges/corners that can be used to resize a window
/// </summary>
public enum PhotinoWindowHitTestCode
{
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}