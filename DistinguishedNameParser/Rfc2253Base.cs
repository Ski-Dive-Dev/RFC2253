using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Rfc2253
{
    public abstract class Rfc2253Base : INormalizable
    {
        public virtual bool IsCaseSensitive { get; protected set; }

        public bool IsNormalized { get; protected set; }

        /// <summary>
        /// Returns the RDN as a normalized string, but does not normalize the internal data structure.
        /// </summary>
        public virtual string GetAsNormalized() => GetAsNormalized(convertToNormalized: false);


        /// <summary>
        /// Normalizes the internal data structure of the RDN.  Subsequent calls to <see cref="ToString"/> will
        /// return the normalized string.
        /// </summary>
        public virtual void Normalize() => GetAsNormalized(convertToNormalized: true);


        /// <summary>
        /// Gets the RDN as a normalized string and optionally normalizes the data structure.
        /// </summary>
        public abstract string GetAsNormalized(bool convertToNormalized);


        /// <summary>
        /// Converts all non-escaped characters (e.g., hex digits) to lower case.
        /// </summary>
        protected virtual string ConvertToLowerCase(string stringToNormalize)
        {
            const string notEscapedUpperCaseLettersRegexPattern = @"(?<!\\[A-Za-z0-9]|\\)([A-Z]+)";
            var r = new Regex(notEscapedUpperCaseLettersRegexPattern, RegexOptions.Compiled);
            var upperCaseLettersCollection = r.Matches(stringToNormalize);

            var sourceStringIdx = 0;
            var normalized = new StringBuilder(capacity: stringToNormalize.Length);
            var replacement = String.Empty;

            foreach (Match m in upperCaseLettersCollection)
            {
                replacement = m.Value.ToLower();
                AppendReplacementAndAdvanceSource();

                void AppendReplacementAndAdvanceSource()                    // (see RdnValue.NormalizeEscapedChars)
                {
                    var numCharsProcessedButNotYetAppended = m.Index - sourceStringIdx;
                    normalized.Append(stringToNormalize.Substring(sourceStringIdx,
                        numCharsProcessedButNotYetAppended) + replacement);
                    sourceStringIdx += numCharsProcessedButNotYetAppended + m.Length;
                }
            }

            var numCharsNotYetAppended = stringToNormalize.Length - sourceStringIdx;
            if (numCharsNotYetAppended > 0)
            {
                normalized.Append(stringToNormalize.Substring(sourceStringIdx, numCharsNotYetAppended));
            }

            return normalized.ToString();
        }
    }
}
