namespace MAUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Detail pages — not in TabBar, navigated to programmatically
        Routing.RegisterRoute("tripdetails", typeof(Views.TripDetailsPage));
        Routing.RegisterRoute("stoppicker", typeof(Views.StopPickerPage));
        Routing.RegisterRoute("tripreplay", typeof(Views.TripReplayPage));
    }
}
