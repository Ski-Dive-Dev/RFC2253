using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rfc2253
{
    public class RdnValue : Rfc2253Base, IAttributeComponent, INormalizable
    {
        public string Value { get; private set; }

        public bool IsQuoted { get; private set; }
        public bool IsHexString { get; private set; }
        public bool IsMultiValued => (MultiValues?.Length ?? 0) != 0;
        public IRelativeDistinguishedName[] MultiValues { get; private set; } = new RelativeDistinguishedName[0];

        private const string special = @",=+<>#;";
        private static readonly byte[] asciiCodesForSpecialChars = new byte[]
        {
            0x2c,   // ,
            0x3d,   // =
            0x2b,   // +
            0x3c,   // <
            0x3e,   // >
            0x23,   // #
            0x3b    // ;
        };

        protected RdnValue() { /* Disable default constructor for public use. */ }
        protected RdnValue(string value) => Value = value;

        public static IAttributeComponent Create(string rdnValue, bool isQuoted = false, bool isHexString = false,
            bool isCaseSensitive = true, IRelativeDistinguishedName[] multiValues = null)
        {
            return (IAttributeComponent)new RdnValue()
            {
                Value = rdnValue ?? throw new ArgumentNullException(nameof(rdnValue)),
                IsHexString = isHexString,
                IsQuoted = isQuoted,
                IsCaseSensitive = isCaseSensitive,
                MultiValues = multiValues ?? new RelativeDistinguishedName[0]
            };
        }


        /// <summary>
        /// Iterates through the <see cref="RelativeDistinguishedName"/> objects in <see cref="MultiValues"/>,
        /// gets a normalized version of each Attribute Type and Value, and returns them concatenated with '+'.
        /// </summary>
        public string GetNormalizedMultiValue(bool convertToNormalized)
        {
            try
            {
                var normalizedString = new StringBuilder();
                const char rdnComponentDelimiter = '+';
                foreach (var rdn in MultiValues)
                {
                    normalizedString.Append(rdn.GetAsNormalized(convertToNormalized));
                    normalizedString.Append(rdnComponentDelimiter);
                }

                var positionOfLastAddedDelimiter = normalizedString.Length - 1;
                normalizedString.Remove(positionOfLastAddedDelimiter, length: 1);

                if (convertToNormalized)
                {
                    Value = normalizedString.ToString();
                    IsNormalized = true;
                }
                return normalizedString.ToString();
            }
            catch (Exception ex)
            {
                var message = $"An error occurred while normalizing an RDN Multi-Value, {Value}.";
                throw new Exception(message, ex);
            }
        }


        /// <summary>
        /// Returns the RDN value as a normalized string, but does not normalize the internal data structure.
        /// </summary>
        public override string GetAsNormalized() => GetAsNormalized(convertToNormalized: false);


        /// <summary>
        /// Normalizes the internal data structure of the RDN value.
        /// </summary>
        public override void Normalize() => GetAsNormalized(convertToNormalized: true);


        /// <summary>
        /// Gets the RDN value as a normalized string and optionally normalizes the data structure.
        /// </summary>
        public override string GetAsNormalized(bool convertToNormalized)
        {
            try
            {
                var normalizedValue = Value;
                if (IsMultiValued)
                {
                    normalizedValue = GetNormalizedMultiValue(convertToNormalized);
                }
                else if (IsHexString)
                {
                    normalizedValue = Value[0] == '#'
                        ? Value
                        : "#" + Value;                                          // Ensure Hex String is Prefixed
                }
                else
                {
                    if (IsQuoted)
                    {
                        normalizedValue = Unquote(Value);
                    }

                    if (!IsCaseSensitive)
                    {
                        normalizedValue = ConvertToLowerCase(normalizedValue);
                    }

                    normalizedValue = NormalizeEscapedChars(normalizedValue);
                }

                if (convertToNormalized)
                {
                    Value = normalizedValue;
                    IsNormalized = true;
                }

                return normalizedValue;
            }
            catch (Exception ex)
            {
                var message = $"An error occurred while normalizing an RDN Value, {Value}.";
                throw new Exception(message, ex);
            }
        }


        /// <summary>
        /// Removes enclosing quote marks from the given string and escapes RFC2253 "special" characters.  It
        /// escapes spaces immediately following the opening quote and immediately preceding the closing quote.
        /// Does not perform any other normalization.
        /// </summary>
        protected virtual string Unquote(string quotedString)
        {
            if (!quotedString.StartsWith("\"") || !quotedString.EndsWith("\"")) return quotedString;

            const string escapedHexCodeForSpace = @"\20";

            const int guessAtMaxNumberOfEscapedChars = 10;
            var escapedAndUnquoted =
                new StringBuilder(capacity: quotedString.Length + guessAtMaxNumberOfEscapedChars);

            const string charsThatMustBeEscaped = special;
            const int positionPastFirstQuoteChar = 1;
            const int lengthOfTwoQuoteChars = 2;
            var firstNonSpaceCharPastOpeningQuoteFound = false;
            for (var i = positionPastFirstQuoteChar; i <= quotedString.Length - lengthOfTwoQuoteChars; i++)
            {
                var thisChar = quotedString.Substring(i, 1);
                if (firstNonSpaceCharPastOpeningQuoteFound
                    || (firstNonSpaceCharPastOpeningQuoteFound = thisChar !=" "))
                {
                    escapedAndUnquoted.Append(charsThatMustBeEscaped.Contains(thisChar)
                        ? "\\" + thisChar
                        : thisChar);
                }
                else
                {
                    escapedAndUnquoted.Append(escapedHexCodeForSpace);
                }
            }

            var lengthWithTrailingSpaces = escapedAndUnquoted.Length;
            var trimmedEscapedAndUnquotedString = escapedAndUnquoted.ToString().TrimEnd();
            var numTrailingSpaces = lengthWithTrailingSpaces - trimmedEscapedAndUnquotedString.Length;
            var escapedSpaces = String.Concat(Enumerable.Repeat(escapedHexCodeForSpace, numTrailingSpaces));

            return trimmedEscapedAndUnquotedString + escapedSpaces;
        }


        /// <summary>
        /// Returns the given string with superfluously escaped characters un-escaped and hex-escaped "special"
        /// characters escaped per RFC 2253.  Changes the casing of existing escaped hex digits to uppercase.  Does
        /// not <i>add</i> escaping as it expects the given string to be a valid RFC 2253 Attribute Value.
        /// </summary>
        protected virtual string NormalizeEscapedChars(string stringToNormalize)
        {
            const string rfc2253EscapedHexDigitsRegexPattern = @"\\[0-9A-Fa-f]{2}";
            var r = new Regex(rfc2253EscapedHexDigitsRegexPattern, RegexOptions.Compiled);

            var collectionOfEscapedHexPairs = r.Matches(stringToNormalize);
            var sourceStringIdx = 0;
            var normalized = new StringBuilder(capacity: stringToNormalize.Length);
            var replacement = String.Empty;

            foreach (Match m in collectionOfEscapedHexPairs)
            {
                const int positionAfterBackslash = 1;
                var hexDigits = m.Value.Substring(startIndex: positionAfterBackslash);
                var asciiValue = Byte.Parse(hexDigits, System.Globalization.NumberStyles.AllowHexSpecifier);

                var isSpecialChar = IsRfc2253SpecialChar(asciiValue);
                if (isSpecialChar || !IsNonPrintableAscii(asciiValue))
                {
                    replacement = isSpecialChar
                        ? @"\" + Convert.ToChar(asciiValue)         // Normalize '\hh' => '\c' e.g.: '\2b' => '\+'
                        : Convert.ToChar(asciiValue).ToString();    // Convert   '\hh' => 'c'  e.g.: '\40' => 'A'

                    AppendReplacementAndAdvanceSource();
                }
                else if (String.Compare(hexDigits, hexDigits.ToUpper(), ignoreCase: false) != 0)
                {
                    replacement = @"\" + hexDigits.ToUpper();
                    AppendReplacementAndAdvanceSource();
                }

                void AppendReplacementAndAdvanceSource()                        // Keepin' it DRY ;)
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


        /// <summary>
        /// Assuming 7-bit ASCII, returns <see langword=""="true"/> if given ASCII value is non-printable.  Does
        /// not consider RFC 2253 "special" characters as requiring escaping.
        /// </summary>
        protected virtual bool IsNonPrintableAscii(byte asciiValue) =>
            (Char.IsControl((char)asciiValue) || asciiValue >= 0x7f);


        /// <summary>
        /// Returns <see langword="true"/> if the given ASCII value represents one of the seven "special"
        /// characters defined in RFC 2253.
        /// </summary>
        protected virtual bool IsRfc2253SpecialChar(byte asciiValue) => asciiCodesForSpecialChars.Contains(asciiValue);


        /// <summary>
        /// Returns the attribute value as a string representation of its current state, which may or may not be
        /// normalized.
        /// </summary>
        public override string ToString() => Value;
    }
}
