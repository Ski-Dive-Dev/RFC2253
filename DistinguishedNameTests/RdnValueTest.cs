using NUnit.Framework;
using Rfc2253;

namespace DistinguishedNameTests
{
    [TestFixture]
    public class RdnValueTest : RdnValue
    {
        public RdnValueTest() { }

        protected RdnValueTest(string value) : base(value) { /* No additional setup required. */ }

        protected override string NormalizeEscapedChars(string stringToNormalize)
        {
            return base.NormalizeEscapedChars(stringToNormalize);
        }

        protected override string Unquote(string quotedString)
        {
            return base.Unquote(quotedString);
        }

        [TestCase(@"Good\20Dog", ExpectedResult = @"Good Dog")]
        [TestCase(@"Smith\2cJohn", ExpectedResult = @"Smith\,John")]
        [TestCase(@"Smith\2CJohn", ExpectedResult = @"Smith\,John")]
        [TestCase(@"My\FFChar", ExpectedResult = @"My\FFChar")]
        [TestCase(@"My\0DChar", ExpectedResult = @"My\0DChar")]
        [TestCase(@"My\0dChar", ExpectedResult = @"My\0DChar")]
        [TestCase(@"This\+That", ExpectedResult = @"This\+That")]
        [TestCase(@"Literal Back\\Slash", ExpectedResult = @"Literal Back\\Slash")]
        [TestCase(@"Not\&Valid", ExpectedResult = @"Not\&Valid")]
        [TestCase(@"", ExpectedResult = @"")]
        public string ShouldNormalizeEscapeChars(string stringToNormalize)
        {
            var value = new RdnValueTest(stringToNormalize);

            return value.NormalizeEscapedChars(stringToNormalize);
        }



        [TestCase("\"All good boys deserve fudge.\"", ExpectedResult = "All good boys deserve fudge.")]
        [TestCase("\"All good boys + girls deserve fudge.\"", ExpectedResult = "All good boys \\+ girls deserve fudge.")]
        [TestCase("\"Boys, girls + adults deserve fudge.\"", ExpectedResult = "Boys\\, girls \\+ adults deserve fudge.")]
        [TestCase("\"Don't expect to fix \\3cthis\\3e; but this.\"",
            ExpectedResult = "Don't expect to fix \\3cthis\\3e\\; but this.")]
        [TestCase("\"  Leading Spaces\"", ExpectedResult = "\\20 Leading Spaces")]
        [TestCase("\"Trailing Spaces  \"", ExpectedResult = "Trailing Spaces \\20")]
        [TestCase("\"  Leading and Trailing Spaces  \"", ExpectedResult = "\\20 Leading and Trailing Spaces \\20")]
        [TestCase("\"Only opening quote", ExpectedResult = "\"Only opening quote")]
        [TestCase("Only closing quote\"", ExpectedResult = "Only closing quote\"")]
        [TestCase("", ExpectedResult = "")]
        public string ShouldUnquoteString(string stringToUnquote)
        {
            var value = new RdnValueTest(stringToUnquote);

            return value.Unquote(stringToUnquote);
        }
    }
}
