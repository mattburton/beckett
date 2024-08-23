namespace Beckett.Dashboard;

public class DashboardOptions
{
    /// <summary>
    /// Control the availability of the message store functionality of the dashboard. If disabled it will be hidden from
    /// view and inaccessible via direct access. This is useful if you are using a third-party message or event store
    /// that provides its own dashboard or other means to interact with messages, and you don't need that functionality
    /// from Beckett.
    /// </summary>
    public bool MessageStoreEnabled { get; set; } = true;
}
