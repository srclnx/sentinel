namespace Sentinel.FileMonitor
{
    public enum SubsequentEntryOptions
    {
        /// <summary>
        /// Each line is individually validated, if it fails to match pattern, log it and ignore it.
        /// </summary>
        TreatIndividually = 0,

        /// <summary>
        /// If the next line(s) do not match the pattern, keep appending the lines to the description field.
        /// </summary>
        AppendToDescriptionIfNonmatching = 1
    }
}