namespace Beckett.Subscriptions;

public enum SubscriptionStatus
{
    Unknown,
    Uninitialized,
    Active,
    Paused,
    Replay,
    Backfill
}
