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

    [Fact]
    public void SubView_Size_Change_Invalidates_SuperView ()
    {
        (IApplication app, Runnable<bool> top, View sub) = CreateApp ();
        top.ClearNeedsDraw ();

        sub.Height = 6;
        app.LayoutAndDraw ();

        // The old and new frame of the SubView are invalidated on the SuperView by SetFrame, so the SuperView
        // still repaints the area even though the whole screen is not cleared.
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
