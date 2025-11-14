namespace ChartMemoryGame
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

#if WINDOWS || MACCATALYST
        Items.Clear();
        Items.Add(new ShellContent { Content = new DesktopView(), Route = "DesktopView" });
#elif IOS || ANDROID
            Items.Clear();
            Items.Add(new ShellContent { Content = new MobileView(), Route = "MobileView" });
#endif

        }
    }
}