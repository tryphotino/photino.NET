using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Photino.NET.Tests;

[TestFixture]
[Platform("win")]
public sealed class MenuTests
{
    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void Add_ShouldThrow_WhenMenuItemHasParent()
    {
        // Arrange

        var window = new PhotinoWindow();
        using var menu1 = new Menu(window);
        using var menu2 = new Menu(window);
        using var menuItem = new MenuItem(new MenuItemOptions { Label = "Item 1" });
        menu1.Add(menuItem);

        // Act

        var act = () =>
        {
            menu2.Add(menuItem);
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
        using var menu1 = new Menu(window);
        using var menu2 = new Menu(window);
        using var menuSeparator = new MenuSeparator();
        menu1.Add(menuSeparator);

        // Act

        var act = () =>
        {
            menu2.Add(menuSeparator);
        };

        // Assert

        act.Should().Throw<Exception>();
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void Dispose_ShouldDisposeAllDescendants()
    {
        // Arrange

        var window = new PhotinoWindow();
        var menu = new Menu(window);

        MenuItem[] menuItems =
        [
            new MenuItem(new MenuItemOptions { Label = "Item 1" }),
            new MenuItem(new MenuItemOptions { Label = "Item 2" }),
            new MenuItem(new MenuItemOptions { Label = "Item 3" }),
            new MenuItem(new MenuItemOptions { Label = "Item 4" })
        ];

        menu.Add(menuItems[0]);
        menu.Add(menuItems[1]);
        menuItems[1].Add(menuItems[2]);
        menuItems[1].Add(menuItems[3]);

        // Act

        menu.Dispose();

        // Assert

        using var undisposedMenuItem = new MenuItem(new MenuItemOptions { Label = "Item 5" });

        foreach (var menuItem in menuItems)
        {
            var add = () =>
            {
                menuItem.Add(undisposedMenuItem);
            };

            add.Should().Throw<ObjectDisposedException>();
        }
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public async Task Show_ShouldThrow_WhenPhotinoHasNotInitialized()
    {
        // Arrange

        var window = new PhotinoWindow();
        using var menu = new Menu(window);

        // Act

        var act = async () =>
        {
            await menu.Show(0, 0);
        };

        // Assert

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}