using System;

namespace WhereAmIGoing
{
    // Pure string formatting, kept free of game types so it can be unit tested outside Unity.
    internal static class SeerText
    {
        public static string AppendStage(string displayName, string stageName)
        {
            return displayName + " (" + stageName + ")";
        }

        // Turns "Dream <nobr>(2 Lunar)</nobr>" into "Dream of Sky Meadow <nobr>(2 Lunar)</nobr>".
        // If the prompt doesn't start with the label (another mod or a game update changed it),
        // the stage is appended in brackets so the information still shows.
        public static string InsertStageIntoContext(string context, string contextLabel, string stageName)
        {
            if (!string.IsNullOrEmpty(contextLabel) && context.StartsWith(contextLabel, StringComparison.Ordinal))
            {
                return contextLabel + " of " + stageName + context.Substring(contextLabel.Length);
            }
            return context + " [" + stageName + "]";
        }
    }
}
