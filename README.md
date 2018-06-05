# RFC 2253 Distinguished Name Parser

A .NET Core 2.0 solution to parse LDAP (or X.509) Distinguished Names and optionally normalize them so that two Distinguished Names can be compared to one another for equivalency.

Closely follows RFC 2253 (https://www.ietf.org/rfc/rfc2253.txt) for parsing.  Parses both LDAPv2 and LDAPv3, but normalizes output for LDAPv3.

Uses Regular Expressions for parsing, which uses results in more concise code than parsing loops.

Sample NUnit Test Cases:
```csharp
[TestCase(@"CN=Steve Kille,O=Isode Limited,C=GB", 
    ExpectedResult = @"cn=Steve Kille,o=Isode Limited,c=GB")]
[TestCase(@"OU=Sales+CN=J. Smith,O=Widget Inc.,C=US", 
    ExpectedResult = @"ou=Sales+cn=J. Smith,o=Widget Inc.,c=US")]
[TestCase(@"CN=L. Eagle,O=Sue\, Grabbit and Runn,C=GB",
    ExpectedResult = @"cn=L. Eagle,o=Sue\, Grabbit and Runn,c=GB")]
[TestCase(@"CN =Before\0DAfter,O= Test,C  =  GB", ExpectedResult = @"cn=Before\0DAfter,o=Test,c=GB")]
[TestCase(@"1.3.6.1.4.1.1466.0=#04024869,O=Test,C=GB", 
    ExpectedResult = @"1.3.6.1.4.1.1466.0=#04024869,o=Test,c=GB")]
[TestCase(@"SN=Lu\C4\8Di\C4\87", ExpectedResult = @"sn=Lu\C4\8Di\C4\87")]
[TestCase(@"OID.1.3.6.1.4.1.1466.0=Bytes \+ Bytes, O=OID Prefix,C=GB", 
    ExpectedResult = @"1.3.6.1.4.1.1466.0=Bytes \+ Bytes,o=OID Prefix,c=GB")]
[TestCase(@"", ExpectedResult = @"")]
[TestCase(@"CN=""Quoted Last, Quoted First"", O=Space After Comma ; C = Semi's too!", 
    ExpectedResult = @"cn=Quoted Last\, Quoted First,o=Space After Comma,c=Semi's too!")]
[TestCase(@"CN=", ExpectedResult = @"cn=")]
[TestCase(@"CN=,sn=Smith", ExpectedResult = @"cn=,sn=Smith")]
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
[TestCase("\"All good boys deserve fudge.\"", ExpectedResult = "All good boys deserve fudge.")]
[TestCase("\"All good boys + girls deserve fudge.\"", ExpectedResult = "All good boys \\+ girls deserve fudge.")]
[TestCase("\"Boys, girls + adults deserve fudge.\"", ExpectedResult = "Boys\\, girls \\+ adults deserve fudge.")]
[TestCase("\"Don't expect to fix \\3cthis\\3e; but this.\"",
    ExpectedResult = "Don't expect to fix \\3cthis\\3e\\; but this.")]
[TestCase("\"  Leading Spaces\"", ExpectedResult = "\\20\\20Leading Spaces")]
[TestCase("\"Trailing Spaces  \"", ExpectedResult = "Trailing Spaces\\20\\20")]
[TestCase("\"  Leading and Trailing Spaces  \"", ExpectedResult = "\\20\\20Leading and Trailing Spaces\\20\\20")]
[TestCase("\"Only opening quote", ExpectedResult = "\"Only opening quote")]
[TestCase("Only closing quote\"", ExpectedResult = "Only closing quote\"")]
```
