namespace Beckett.Subscriptions;

public enum ReplayMode
{
    ///<summary>Process all subscriptions whether they are active or replays</summary>
    All,
    ///<summary>Only process active subscriptions and ignore replays</summary>
    ActiveOnly,
    ///<summary>Only process replays and ignore active subscriptions</summary>
    ReplayOnly
}
