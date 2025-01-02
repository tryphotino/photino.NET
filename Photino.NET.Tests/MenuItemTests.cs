using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NUnit.Framework;

namespace Photino.NET.Tests;

[TestFixture]
[Platform("win")]
public sealed class MenuItemTests
{
    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void Add_ShouldThrow_WhenMenuItemHasParent()
    {
        // Arrange

        using var menuItem1 = new MenuItem(new MenuItemOptions { Label = "Item 1" });
        using var menuItem2 = new MenuItem(new MenuItemOptions { Label = "Item 2" });
        using var menuItem3 = new MenuItem(new MenuItemOptions { Label = "Item 3" });
        menuItem1.Add(menuItem3);

        // Act

        var act = () =>
        {
            menuItem2.Add(menuItem3);
        };

        // Assert

        act.Should().Throw<Exception>();
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void Add_ShouldThrow_WhenMenuItemIsSelf()
    {
        // Arrange

        using var menuItem = new MenuItem(new MenuItemOptions { Label = "Item 1" });

        // Act

        var act = () =>
        {
            menuItem.Add(menuItem);
        };

        // Assert

        act.Should().Throw<Exception>();
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void Add_ShouldThrow_WhenMenuSeparatorHasParent()
    {
        // Arrange

        var window = new PhotinoWindow();
        using var menu = new Menu(window);
        using var menuItem = new MenuItem(new MenuItemOptions { Label = "Item 1" });
        using var menuSeparator = new MenuSeparator();
        menu.Add(menuSeparator);

        // Act

        var act = () =>
        {
            menuItem.Add(menuSeparator);
        };

        // Assert

        act.Should().Throw<Exception>();
    }

    [Test]
    public void MenuItem_ShouldInitialize_WhenImageIsNull()
    {
        // Act & Assert

        _ = new MenuItem(new MenuItemOptions { Label = "Item 1" });
    }

    [Test]
    public void MenuItem_ShouldInitialize_WhenLabelIsNull()
    {
        // Act & Assert

        _ = new MenuItem(new MenuItemOptions());
    }
}