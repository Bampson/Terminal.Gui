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

    [Fact]
    public void Narrowed_Redraw_Repaints_Selection_And_Leaves_Other_Rows_Intact ()
    {
        IDriver driver = CreateTestDriver ();
        TreeView tree = CreateTree (driver, 4, out TreeNode [] nodes);
        tree.SelectedObject = nodes [0];
        tree.Draw ();
        tree.ClearNeedsDraw ();

        tree.SelectedObject = nodes [2];
        tree.Draw ();

        string screen = driver.ToString () ?? "";
        Assert.Contains ("one", screen);
        Assert.Contains ("two", screen);
        Assert.Contains ("three", screen);
        Assert.Contains ("four", screen);
        Assert.Equal (nodes [2], tree.GetObjectOnRow (2));
    }
}
