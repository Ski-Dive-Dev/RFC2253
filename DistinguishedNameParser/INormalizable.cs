namespace SkiDiveCode.Ldap.Rfc2253
{
    public interface INormalizable
    {
        /// <summary>
        /// Returns <see langword=""="true"/> if the object is in a "normalized" state.
        /// </summary>
        bool IsNormalized { get; }

        /// <summary>
        /// Returns the object as a normalized string, but does not normalize the internal data structure.
        /// </summary>
        string GetAsNormalized();

        /// <summary>
        /// Gets the object as a normalized string and optionally normalizes the data structure.
        /// </summary>
        string GetAsNormalized(bool convertToNormalized);

        /// <summary>
        /// Normalizes the internal data structure; subsequent calls to <see cref="ToString"/> will return the
        /// normalized string.
        /// </summary>
        void Normalize();

        /// <summary>
        /// Returns the object as a string representation of its current state, which may or may not be normalized.
        /// </summary>
        string ToString();
    }
}