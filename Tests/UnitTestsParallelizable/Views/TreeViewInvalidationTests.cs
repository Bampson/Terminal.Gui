using UnitTests;

namespace ViewsTests;

/// <summary>
///     Tests that moving the <see cref="TreeView{T}"/> selection invalidates only the rows involved, so a large tree
///     does not repaint every cell for every cursor key. Claude Fable 5.1 (AI generated).
/// </summary>
public class TreeViewInvalidationTests : TestDriverBase
{
    private static TreeView CreateTree (IDriver driver, int height, out TreeNode [] nodes)
    {
        nodes =
        [
            new () { Text = "one" },
            new () { Text = "two" },
            new () { Text = "three" },
            new () { Text = "four" }
        ];

        TreeView tree = new () { Driver = driver, MultiSelect = false };
        tree.AddObjects (nodes);
        tree.Frame = new Rectangle (0, 0, 20, height);
        tree.Draw ();
        tree.ClearNeedsDraw ();

        return tree;
    }

    [Fact]
    public void SelectedObject_Change_Invalidates_Old_And_New_Rows_Only ()
    {
        IDriver driver = CreateTestDriver ();
        TreeView tree = CreateTree (driver, 4, out TreeNode [] nodes);
        tree.SelectedObject = nodes [0];
        tree.ClearNeedsDraw ();

        tree.SelectedObject = nodes [2];

        // Row 0 (old) and row 2 (new): the union spans rows 0-2 but not row 3.
        Assert.Equal (new Rectangle (0, 0, 20, 3), tree.NeedsDrawRect);
    }

    [Fact]
    public void AdjustSelection_Invalidates_Adjacent_Rows_Only ()
    {
        IDriver driver = CreateTestDriver ();
        TreeView tree = CreateTree (driver, 4, out TreeNode [] nodes);
        tree.SelectedObject = nodes [1];
        tree.ClearNeedsDraw ();

        tree.AdjustSelection (1);

        Assert.Equal (nodes [2], tree.SelectedObject);
        Assert.Equal (new Rectangle (0, 1, 20, 2), tree.NeedsDrawRect);
    }

    [Fact]
    public void GoTo_Invalidates_Old_And_New_Rows_Only ()
    {
        IDriver driver = CreateTestDriver ();
        TreeView tree = CreateTree (driver, 4, out TreeNode [] nodes);
        tree.SelectedObject = nodes [3];
        tree.ClearNeedsDraw ();

        tree.GoTo (nodes [2]);

        Assert.Equal (new Rectangle (0, 2, 20, 2), tree.NeedsDrawRect);
    }

    [Fact]
    public void Selection_Change_That_Scrolls_Invalidates_Whole_Viewport ()
    {
        IDriver driver = CreateTestDriver ();
        TreeView tree = CreateTree (driver, 2, out TreeNode [] nodes);
        tree.SelectedObject = nodes [0];
        tree.ClearNeedsDraw ();

        tree.GoTo (nodes [3]);

        // Scrolling shows a scrollbar, which narrows the viewport; the whole (narrowed) viewport is dirty.
        Assert.Equal (2, tree.ScrollOffsetVertical);
        Assert.Equal (new Rectangle (Point.Empty, tree.Viewport.Size), tree.NeedsDrawRect);
    }

    [Fact]
    public void MultiSelect_Region_Invalidates_Whole_Viewport ()
    {
        IDriver driver = CreateTestDriver ();
        TreeView tree = CreateTree (driver, 4, out TreeNode [] nodes);
        tree.MultiSelect = true;
        tree.SelectedObject = nodes [0];
        tree.ClearNeedsDraw ();

        tree.AdjustSelection (1, true);

        Assert.Equal (new Rectangle (0, 0, 20, 4), tree.NeedsDrawRect);
    }

    [Fact]
    public void Clearing_MultiSelect_Region_Invalidates_Whole_Viewport ()
    {
        IDriver driver = CreateTestDriver ();
        TreeView tree = CreateTree (driver, 4, out TreeNode [] nodes);
        tree.MultiSelect = true;
        tree.SelectedObject = nodes [0];
        tree.AdjustSelection (1, true);
        tree.ClearNeedsDraw ();

        // A plain move drops the region, so the rows it covered must be repainted too.
        tree.AdjustSelection (1);

        Assert.Equal (new Rectangle (0, 0, 20, 4), tree.NeedsDrawRect);
    }

    // The tests below draw through IApplication.LayoutAndDraw, which resets the driver's clip for every pass.
    // Calling View.Draw twice in a row does not: the first draw excludes the view from the clip, so the second
    // draw cannot change the buffer and a rendering assertion would pass without proving anything.

    private static (IApplication App, Runnable<bool> Top, TreeView Tree, TreeNode [] Nodes) CreateAppWithTree (bool multiSelect)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 6);

        TreeNode [] nodes =
        [
            new () { Text = "one" },
            new () { Text = "two" },
            new () { Text = "three" },
            new () { Text = "four" }
        ];

        Runnable<bool> top = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        TreeView tree = new () { Width = Dim.Fill (), Height = 4, MultiSelect = multiSelect };
        tree.AddObjects (nodes);
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        return (app, top, tree, nodes);
    }

    /// <summary>Gets the attribute of the first cell of <paramref name="text"/> on screen row <paramref name="row"/>.</summary>
    private static Attribute? AttributeOfText (IDriver driver, int row, string text)
    {
        for (var col = 0; col <= driver.Cols - text.Length; col++)
        {
            var matches = true;

            for (var i = 0; i < text.Length; i++)
            {
                if (driver.Contents! [row, col + i].Grapheme != text [i].ToString ())
                {
                    matches = false;

                    break;
                }
            }

            if (matches)
            {
                return driver.Contents! [row, col].Attribute;
            }
        }

        Assert.Fail ($"'{text}' was not found on row {row}.");

        return null;
    }

    [Fact]
    public void Narrowed_Redraw_Moves_Highlight_From_Old_Row_To_New_Row ()
    {
        (IApplication app, Runnable<bool> top, TreeView tree, TreeNode [] nodes) = CreateAppWithTree (false);
        IDriver driver = app.Driver!;
        tree.SelectedObject = nodes [0];
        app.LayoutAndDraw ();

        Attribute normal = tree.GetAttributeForRole (VisualRole.Normal);
        Attribute selected = tree.GetAttributeForRole (tree.HasFocus ? VisualRole.Focus : VisualRole.Active);
        Assert.NotEqual (normal, selected);
        Assert.Equal (selected, AttributeOfText (driver, 0, "one"));
        Assert.Equal (normal, AttributeOfText (driver, 2, "three"));

        tree.SelectedObject = nodes [2];

        // Rows 0 (old) and 2 (new) are dirty; row 3 is not, so this draw pass is narrowed.
        Assert.Equal (new Rectangle (0, 0, tree.Viewport.Width, 3), tree.NeedsDrawRect);
        app.LayoutAndDraw ();

        Assert.Equal (normal, AttributeOfText (driver, 0, "one"));
        Assert.Equal (normal, AttributeOfText (driver, 1, "two"));
        Assert.Equal (selected, AttributeOfText (driver, 2, "three"));
        Assert.Equal (normal, AttributeOfText (driver, 3, "four"));

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Narrowed_Redraw_Draws_Only_Invalidated_Rows ()
    {
        (IApplication app, Runnable<bool> top, TreeView tree, TreeNode [] nodes) = CreateAppWithTree (false);
        tree.SelectedObject = nodes [1];
        app.LayoutAndDraw ();

        List<int> drawnRows = [];
        tree.DrawLine += (_, e) => drawnRows.Add (e.Y);

        tree.AdjustSelection (1);
        app.LayoutAndDraw ();

        Assert.Equal (nodes [2], tree.SelectedObject);
        Assert.Equal ([1, 2], drawnRows);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Full_Redraw_Draws_Every_Row ()
    {
        (IApplication app, Runnable<bool> top, TreeView tree, TreeNode [] nodes) = CreateAppWithTree (false);
        tree.SelectedObject = nodes [1];
        app.LayoutAndDraw ();

        List<int> drawnRows = [];
        tree.DrawLine += (_, e) => drawnRows.Add (e.Y);

        tree.SetNeedsDraw ();
        app.LayoutAndDraw ();

        Assert.Equal ([0, 1, 2, 3], drawnRows);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Clearing_MultiSelect_Region_Removes_Highlight_From_Region_Rows ()
    {
        (IApplication app, Runnable<bool> top, TreeView tree, TreeNode [] nodes) = CreateAppWithTree (true);
        IDriver driver = app.Driver!;
        tree.SelectedObject = nodes [0];
        tree.AdjustSelection (1, true);
        tree.AdjustSelection (1, true);
        app.LayoutAndDraw ();

        Attribute normal = tree.GetAttributeForRole (VisualRole.Normal);
        Attribute selected = tree.GetAttributeForRole (tree.HasFocus ? VisualRole.Focus : VisualRole.Active);
        Assert.Equal (selected, AttributeOfText (driver, 0, "one"));
        Assert.Equal (selected, AttributeOfText (driver, 1, "two"));
        Assert.Equal (selected, AttributeOfText (driver, 2, "three"));
        Assert.Equal (normal, AttributeOfText (driver, 3, "four"));

        // A plain move drops the region and selects row 3 only.
        tree.AdjustSelection (1);
        app.LayoutAndDraw ();

        Assert.Equal (nodes [3], tree.SelectedObject);
        Assert.Equal (normal, AttributeOfText (driver, 0, "one"));
        Assert.Equal (normal, AttributeOfText (driver, 1, "two"));
        Assert.Equal (normal, AttributeOfText (driver, 2, "three"));
        Assert.Equal (selected, AttributeOfText (driver, 3, "four"));

        top.Dispose ();
        app.Dispose ();
    }
}
