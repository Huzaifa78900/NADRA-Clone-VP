namespace NadraApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) => Report(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => Report(e.ExceptionObject as Exception);

        try { Db.Init(); }
        catch (Exception ex) { Report(ex); return; }

        using (var splash = new SplashForm()) splash.ShowDialog();
        Application.Run(new MainForm());
    }

    public static void Log(Exception ex)
    {
        try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "error.log"), $"[{DateTime.Now}] {ex}\r\n\r\n"); } catch { }
    }

    static void Report(Exception ex)
    {
        if (ex == null) return;
        Log(ex);
        MessageBox.Show("An unexpected problem occurred, but your data is safe.\n\n" + ex.Message + "\n\n(Details saved to error.log)",
            "NADRA", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
