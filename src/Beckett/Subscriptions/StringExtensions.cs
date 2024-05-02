namespace Beckett.Subscriptions;

internal static class StringExtensions
{
    /**
     * Adapted from https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
     */
    public static int GetDeterministicHashCode(this string input)
    {
        return Math.Abs(GenerateHashCode(input));

        static int GenerateHashCode(string input)
        {
            unchecked
            {
                var hash1 = (5381 << 16) + 5381;
                var hash2 = hash1;

                for (var i = 0; i < input.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ input[i];

                    if (i == input.Length - 1)
                    {
                        break;
                    }

                    hash2 = ((hash2 << 5) + hash2) ^ input[i + 1];
                }

                return hash1 + hash2 * 1566083941;
            }
        }
    }
}
