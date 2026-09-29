using System.Text;

namespace ViewBaseTests.Layout;

/// <summary>
///     Tests that the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/> and <see cref="View.Height"/>
///     setters only request a full-screen clear (<see cref="IApplication.ClearScreenNextIteration"/>) when a top-level
///     view actually changes. Re-assigning the same value, or changing a SubView, must not repaint the whole screen.
///     Claude Fable 5.1 (AI generated).
/// </summary>
public class SizeSettersClearScreenTests
{
    private static (IApplication App, Runnable<bool> Top, View Sub) CreateApp ()
    {
        Runnable<bool> top = new () { Width = 20, Height = 10 };
        View sub = new () { X = 1, Y = 1, Width = 5, Height = 5 };
        top.Add (sub);

        IApplication app = Application.Create ();
        app.Begin (top);
        app.LayoutAndDraw ();
        app.ClearScreenNextIteration = false;

        return (app, top, sub);
    }

    [Fact]
    public void Height_Set_To_Same_Value_Does_Not_Request_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top, View sub) = CreateApp ();

        top.Height = 10;
        sub.Height = 5;

        Assert.False (app.ClearScreenNextIteration);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Width_Set_To_Same_Value_Does_Not_Request_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top, View sub) = CreateApp ();

        top.Width = 20;
        sub.Width = 5;

        Assert.False (app.ClearScreenNextIteration);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SubView_Size_Change_Does_Not_Request_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top, View sub) = CreateApp ();

        sub.Width = 6;
        sub.Height = 6;

        Assert.False (app.ClearScreenNextIteration);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SubView_Position_Change_Does_Not_Request_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top, View sub) = CreateApp ();

        sub.X = 2;
        sub.Y = 2;

        Assert.False (app.ClearScreenNextIteration);

        top.Dispose ();
        app.Dispose ();
    }

    /// <summary>Creates a view that paints an X in every cell of its viewport, whatever its size.</summary>
    private static View CreateFilledView (int x, int y, int width, int height)
    {
        View view = new () { X = x, Y = y, Width = width, Height = height };
        view.DrawingContent += (_, _) => view.FillRect (view.Viewport with { Location = Point.Empty }, new Rune ('X'));

        return view;
    }

    private static (IApplication App, Runnable<bool> Top) CreateAppWithDriver ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 10);

        Runnable<bool> top = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        return (app, top);
    }

    /// <summary>Asserts that every cell of <paramref name="screenRect"/> shows <paramref name="grapheme"/>.</summary>
    private static void AssertCells (IDriver driver, Rectangle screenRect, string grapheme)
    {
        for (int row = screenRect.Top; row < screenRect.Bottom; row++)
        {
            for (int col = screenRect.Left; col < screenRect.Right; col++)
            {
                string actual = driver.Contents! [row, col].Grapheme;
                Assert.True (grapheme == actual, $"Expected '{grapheme}' at row {row}, col {col} but found '{actual}'.\n{driver.ToString ()}");
            }
        }
    }

    [Fact]
    public void SubView_Shrink_Repaints_Exposed_Area_Without_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top) = CreateAppWithDriver ();
        View sub = CreateFilledView (1, 1, 5, 5);
        top.Add (sub);
        app.Begin (top);
        app.LayoutAndDraw ();
        AssertCells (app.Driver!, new Rectangle (1, 1, 5, 5), "X");

        sub.Height = 3;
        sub.Width = 3;

        // Checked before drawing: LayoutAndDraw consumes the flag, so checking it afterwards proves nothing.
        Assert.False (app.ClearScreenNextIteration);
        app.LayoutAndDraw ();

        AssertCells (app.Driver!, new Rectangle (1, 1, 3, 3), "X");

        // The rows and columns the SubView no longer covers were repainted by the SuperView.
        AssertCells (app.Driver!, new Rectangle (1, 4, 5, 2), " ");
        AssertCells (app.Driver!, new Rectangle (4, 1, 2, 5), " ");

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SubView_Move_Repaints_Old_Area_Without_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top) = CreateAppWithDriver ();
        View sub = CreateFilledView (1, 1, 5, 5);
        top.Add (sub);
        app.Begin (top);
        app.LayoutAndDraw ();
        AssertCells (app.Driver!, new Rectangle (1, 1, 5, 5), "X");

        sub.X = 10;
        sub.Y = 3;

        Assert.False (app.ClearScreenNextIteration);
        app.LayoutAndDraw ();

        AssertCells (app.Driver!, new Rectangle (10, 3, 5, 5), "X");
        AssertCells (app.Driver!, new Rectangle (1, 1, 5, 2), " ");
        AssertCells (app.Driver!, new Rectangle (1, 3, 5, 3), " ");

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SubView_Shrink_Inside_Scrolled_SuperView_Repaints_Exposed_Area ()
    {
        (IApplication app, Runnable<bool> top) = CreateAppWithDriver ();
        View container = new () { Width = 12, Height = 8 };
        container.SetContentSize (new Size (12, 20));
        View sub = CreateFilledView (1, 3, 5, 5);
        container.Add (sub);
        top.Add (container);
        app.Begin (top);
        container.Viewport = container.Viewport with { Y = 2 };
        app.LayoutAndDraw ();

        // Content row 3 is screen row 1 once the container is scrolled down by 2.
        AssertCells (app.Driver!, new Rectangle (1, 1, 5, 5), "X");

        sub.Height = 3;

        Assert.False (app.ClearScreenNextIteration);
        app.LayoutAndDraw ();

        AssertCells (app.Driver!, new Rectangle (1, 1, 5, 3), "X");
        AssertCells (app.Driver!, new Rectangle (1, 4, 5, 2), " ");

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Cancelled_Top_Level_Height_Change_Does_Not_Request_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top, View _) = CreateApp ();
        top.HeightChanging += (_, args) => args.Handled = true;

        top.Height = 11;

        Assert.Equal (Dim.Absolute (10), top.Height);
        Assert.False (app.ClearScreenNextIteration);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Cancelled_Top_Level_Width_Change_Does_Not_Request_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top, View _) = CreateApp ();
        top.WidthChanging += (_, args) => args.Handled = true;

        top.Width = 21;

        Assert.Equal (Dim.Absolute (20), top.Width);
        Assert.False (app.ClearScreenNextIteration);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Top_Level_Height_Change_Requests_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top, View _) = CreateApp ();

        top.Height = 11;

        Assert.True (app.ClearScreenNextIteration);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Top_Level_Width_Change_Requests_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top, View _) = CreateApp ();

        top.Width = 21;

        Assert.True (app.ClearScreenNextIteration);

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Top_Level_Position_Change_Requests_Clear_Screen ()
    {
        (IApplication app, Runnable<bool> top, View _) = CreateApp ();

        top.X = 1;

        Assert.True (app.ClearScreenNextIteration);

        top.Dispose ();
        app.Dispose ();
    }
}
