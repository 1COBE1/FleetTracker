namespace MAUI.Services;

public class StopPickerService
{
    public event Action<double, double, string>? OnLocationPicked;

    public void NotifyLocationPicked(double latitude, double longitude, string address)
        => OnLocationPicked?.Invoke(latitude, longitude, address);
}
