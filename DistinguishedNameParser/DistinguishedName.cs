using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SkiDiveCode.Ldap.Rfc2253
{
    public class DistinguishedName : Rfc2253Base, INormalizable
    {
        // Note: RFC 2253 limits characters to the ASCII set (non-ASCII multi-byte characters need to be escaped as
        // hex digits)

        #region Rfc2253 Section 3
        // Note: Parses both LDAPv3 as well as LDAPv2 (per RFC 2253).  However, per RFC 2253, Distinguished Names
        // should not OUTPUT LDAPv2 syntax.

        private const string backslash = @"\\";         // Regex backslash needs to be escaped to match \ literally
        private const string octothorpe = @"\#";     // Regex Comment char needs to be escaped to match # literally
        private const string quotation = "\"";
        private const string digit = @"\d";
        private const string alpha = @"[A-Za-z]";
        private const string hexChar = @"[0-9A-Fa-f]";
        private const string hexPair = hexChar + hexChar;
        private static readonly string hexString = $@"(?:{hexPair})+";
        private const string special = @",=+<>#;";

        private static readonly string pair = $@"{backslash}([{special}{backslash}{quotation}]|{hexPair})";
        // yields: \\([,=+<>#;\\"]|([0-9A-Fa-f][0-9A-Fa-f]))
        // correctly parses: abc,\,,==\==++\++<<\<<>>\\x>>\>>""\""\55ab"\abb\AF\AX\\a

        private static readonly string stringChar = $@"[^{special}{backslash}{quotation}]";
        private static readonly string quoteChar = $@"[^{backslash}{quotation}]";

        // Reorganized from the RFC since the Regex parser is "greedy":
        private static readonly string stringRegexPattern =
            $@"(?<hexString>{octothorpe}{hexString})" +
            $@"|{quotation}(?<quoted>(?:{quoteChar}|{pair})*){quotation}" +     // Quoted only from LDAPv2
            $@"|(?:{stringChar}|{pair})*";

        private static readonly string attributeValue = stringRegexPattern;
        private static readonly string oid =
            $@"(?:oid\.|OID\.)?(?<oid>{digit}+(?:\.{digit}+)*)";                // OID prefix only from LDAPv2

        private static readonly string keyChar = $@"{alpha}|{digit}|-";

        // There seems to be an error in RFC 2253 - it describes at least one keychar (1*keychar) follows ALPHA.
        // Requiring at least two characters for the attributeType would cause "L=", "O=" and "C=" to fail.
        private static readonly string attributeType = 
            $@"{oid}|{alpha}(?:{keyChar})*";                                    // Reordered for greedy Regex

        private static readonly string attributeTypeAndValue =
            $@"(?<attributeType>{attributeType})" +
            $@"[ ]*=[ ]*" +                                                     // Spaces [ ]* only from LDAPv2
            $@"(?<attributeValue>{attributeValue})";

        private static readonly string name_Component = $@"{attributeTypeAndValue}" +
            $@"(?<subComponents>(?:[ ]*\+[ ]*{attributeTypeAndValue})+)?";      // Spaces [ ]* only from LDAPv2

        // RFC 1779: 'Semicolon (";") may be used an an alternate separator.  The separators may be mixed, but
        // this usage is discouraged.'
        private const string rdnDelimiters = @",;";                             // ";" only from LDAPv2
        private static readonly string name = $@"(?<nameComponent>{name_Component})";
        private static readonly string nextName =
            $@"[ ]*[{rdnDelimiters}][ ]*{name_Component}";                      // Spaces [ ]* only from LDAPv2

        private static readonly string subName = attributeTypeAndValue;
        private static readonly string nextSubNameAndType =
            $@"[ ]*\+[ ]*{attributeTypeAndValue}";                              // Spaces [ ]* only from LDAPv2

        // *Note: RFC 2253 specifically calls for space (' ' ASCII 32) characters, not just any whitespace char,
        // but RFC 1779 defines ( <CR> ) *( " " ) as <optional-space>.

        #endregion


        /// <summary>
        /// The Relative Distinguished Names, in the order originally parsed, that comprise the Distinguished
        /// Name.
        /// </summary>
        public IRelativeDistinguishedName[] Rdns { get; private set; }

        protected DistinguishedName() { /* Disable default constructor for public use. */ }

        private DistinguishedName(string distinguishedName) => this.Rdns = Parse(distinguishedName).ToArray();

        public static DistinguishedName Create(string distiguishedName)
        {
            return new DistinguishedName(distiguishedName ?? 
                throw new ArgumentNullException(nameof(distiguishedName)));
        }


        /// <summary>
        /// Parses the given Distinguished Name as a collection of Relative Distinguished Names.
        /// </summary>
        protected virtual IList<IRelativeDistinguishedName> Parse(string distinguishedName)
        {
            if (distinguishedName == null) throw new ArgumentNullException(nameof(distinguishedName));

            return Parse(firstRdnPattern: name, remainingRdnsPattern: nextName, nameToParse: distinguishedName);
        }


        /// <summary>
        /// Parses a given string that is a multi-valued Relative Distinguished Name, with each value delimited by
        /// '+' symbols.
        /// </summary>
        private IList<IRelativeDistinguishedName> ParseMultiValueRdn(string multiValueRdn)
        {
            return Parse(firstRdnPattern: subName, remainingRdnsPattern: nextSubNameAndType,
                nameToParse: multiValueRdn);
        }


        /// <summary>
        /// Uses the first given regular expression to parse the first Relative Distinguished Name within a
        /// Distinguished Name, and the second given regular expression to parse the remaining Relative
        /// Distinguished Names within the given <paramref name="nameToParse"/>.
        /// </summary>
        private IList<IRelativeDistinguishedName> Parse(string firstRdnPattern, string remainingRdnsPattern,
            string nameToParse)
        {
            const int guessAtMaxNumRdns = 10;
            var rdns = new List<IRelativeDistinguishedName>(guessAtMaxNumRdns);

            if (String.IsNullOrEmpty(nameToParse))
            {
                rdns.Add(RelativeDistinguishedName.Default);
                return rdns;
            }

            var r = new Regex(firstRdnPattern, RegexOptions.Compiled);
            var m = r.Match(nameToParse);

            if (!m.Success) throw new Exception(
                $"A Distinguished Name ('{nameToParse}') had an error and could not be parsed.");

            var rdn = CreateRdnFromRegexMatch(m);
            rdns.Add(rdn);

            var parser = new Regex(remainingRdnsPattern, RegexOptions.Compiled);
            ParseMore(parser: parser,
                remainingSubstringOfDNToParse: nameToParse.Substring(startIndex: m.Index + m.Length),
                rdns: rdns);

            return rdns;
        }


        /// <summary>
        /// Uses the given <see cref="Regex"/> object to parse the given substring, with the resultant matches
        /// added to the given collection of Relative Distinguished Name objects.
        /// </summary>
        private void ParseMore(Regex parser, string remainingSubstringOfDNToParse,
            IList<IRelativeDistinguishedName> rdns)
        {
            var mc = parser.Matches(remainingSubstringOfDNToParse);
            foreach (Match m in mc)
            {
                var rdn = CreateRdnFromRegexMatch(m);
                rdns.Add(rdn);
            }
        }


        /// <summary>
        /// Given a Regex <see cref="Match"/> object from a parsed Relative Distinguished Name, creates and returns
        /// a <see cref="IRelativeDistinguishedName"/> based on that parsed information.
        /// </summary>
        private IRelativeDistinguishedName CreateRdnFromRegexMatch(Match regexMatchOfRdn)
        {
            IAttributeComponent rdnType;
            IAttributeComponent rdnValue;

            var thisIsAMultiValueRdn = (regexMatchOfRdn.Groups["subComponents"].Success);
            if (thisIsAMultiValueRdn)
            {
                rdnType = RdnType.Create(multipleValuesPrefix, isOid: regexMatchOfRdn.Groups["oid"].Success);

                rdnValue = RdnValue.Create(rdnValue: regexMatchOfRdn.Groups["nameComponent"].Value,
                    multiValues: ParseMultiValueRdn(regexMatchOfRdn.Groups["nameComponent"].Value).ToArray(),
                    isQuoted: regexMatchOfRdn.Groups["quoted"].Success,
                    isHexString: regexMatchOfRdn.Groups["hexString"].Success);
            }
            else
            {
                rdnType = RdnType.Create(regexMatchOfRdn.Groups["attributeType"].Value.Trim(),
                    isOid: regexMatchOfRdn.Groups["oid"].Success);

                rdnValue = RdnValue.Create(regexMatchOfRdn.Groups["attributeValue"].Value.Trim(),
                    isQuoted: regexMatchOfRdn.Groups["quoted"].Success,
                    isHexString: regexMatchOfRdn.Groups["hexString"].Success);
            }
            var rdn = RelativeDistinguishedName.Create(rdnType, rdnValue);
            return rdn;
        }


        /// <summary>
        /// Gets the Distinguished Name as a normalized string and optionally normalizes the data structure.
        /// </summary>
        public override string GetAsNormalized(bool convertToNormalized)
        {
            const char rdnDelimiter = ',';

            try
            {
                const int guessAtAverageLengthOfEachRdn = 50;
                int guessAtMaxStringLength = guessAtAverageLengthOfEachRdn * Rdns.Length;
                var normalizedString = new StringBuilder(guessAtMaxStringLength);

                foreach (var rdn in Rdns)
                {
                    normalizedString.Append(rdnDelimiter);
                    normalizedString.Append(rdn.GetAsNormalized(convertToNormalized));
                }

                const int positionPastFirstDelimiter = 1;
                return normalizedString.ToString().Substring(positionPastFirstDelimiter);
            }
            catch (Exception ex)
            {
                var message = $"An error occurred while normalizing a Distinguished Name, '{this}'.";
                throw new Exception(message, ex);
            }
        }


        /// <summary>
        /// Returns <see langword="true"/> if both objects, as normalized Distinguished Names, are equal.
        /// </summary>
        public static bool operator ==(DistinguishedName obj1, DistinguishedName obj2)
            => (obj1.GetAsNormalized() == obj2.GetAsNormalized());


        /// <summary>
        /// Returns <see langword="true"/> if both objects, as normalized Distinguished Names, are <i>not</i>
        /// equal.
        /// </summary>
        public static bool operator !=(DistinguishedName obj1, DistinguishedName obj2)
            => (obj1.GetAsNormalized() != obj2.GetAsNormalized());


        public override bool Equals(object obj)
        {
            var name = obj as DistinguishedName;
            return name != null &&
                   EqualityComparer<IRelativeDistinguishedName[]>.Default.Equals(Rdns, name.Rdns);
        }

        public override int GetHashCode()
        {
            return -342129204 + EqualityComparer<IRelativeDistinguishedName[]>.Default.GetHashCode(Rdns);
        }


        /// <summary>
        /// Returns the Distinguished Name as a string representation of its current state, which may or may not be
        /// normalized.
        /// </summary>
        /// <remarks>
        /// If the internal data structure has already been normalized, <see cref="ToString"/> is more performant
        /// than <see cref="GetAsNormalized"/>.
        /// </remarks>
        public override string ToString()
        {
            const char rdnDelimiter = ',';
            const int guessAtAverageLengthOfEachRdn = 50;

            var dn = new StringBuilder(capacity: Rdns.Length * guessAtAverageLengthOfEachRdn);
            foreach (var rdn in Rdns)
            {
                dn.Append(rdnDelimiter);
                dn.Append(rdn.ToString());
            }
         
            const int positionPastFirstDelimiter = 1;
            return dn.ToString().Substring(positionPastFirstDelimiter);
        }
    }
}
