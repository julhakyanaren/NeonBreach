using System;

public static class StaticEvents
{
    public static event Action PauseOpened;
    public static event Action PauseClosed;

    public static void RaisePauseOpened()
    {
        if (PauseOpened == null)
        {
            return;
        }

        PauseOpened();
    }

    public static void RaisePauseClosed()
    {
        if (PauseClosed == null)
        {
            return;
        }

        PauseClosed();
    }
}
