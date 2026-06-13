using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using ReSharperPlugin.ResXFileCodeGeneratorEx;

namespace ReSharperPlugin.ResXFileCodeGeneratorEx.UnitTests
{
    /// <summary>
    /// Golden-file tests: the generator output must match, byte-for-byte (modulo line endings,
    /// which git normalizes anyway), the output the original Visual Studio ResXFileCodeGeneratorEx
    /// tool produces. The golden files under <c>golden/</c> were captured by running the upstream
    /// <c>StronglyTypedResourceBuilderEx</c> through System.CodeDom's C# provider. The copyright
    /// year is pinned to 2026 (the year the goldens were captured) so the tests are deterministic.
    /// </summary>
    [TestFixture]
    public class ResXCodeGeneratorTests
    {
        private const int GoldenYear = 2026;

        private static string Resx(params (string name, string value, string? type)[] items)
        {
            var sb = new StringBuilder();
            sb.Append("<root>");
            foreach (var (name, value, type) in items)
            {
                var t = type is null ? "" : $" type=\"{type}\"";
                sb.Append($"<data name=\"{XmlAttr(name)}\"{t}><value>{XmlText(value)}</value></data>");
            }
            sb.Append("</root>");
            return sb.ToString();
        }

        private static string XmlAttr(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace("\"", "&quot;");

        private static string XmlText(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static string Golden(string name)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "golden", name + ".cs.txt");
            return File.ReadAllText(path);
        }

        private static string Norm(string s) => s.Replace("\r\n", "\n");

        private static void AssertMatchesGolden(string goldenName, string actual)
        {
            Assert.That(Norm(actual), Is.EqualTo(Norm(Golden(goldenName))));
        }

        [Test]
        public void Public_WithNamespace_matches_VS_output()
        {
            var resx = Resx(
                ("Greeting", "Hello {0}, welcome to {1}!", null),
                ("AppTitle", "My Application", null),
                ("Farewell", "Goodbye", null),
                ("Invalid Name.Key", "needs sanitising", null),
                ("FourArgs", "{0} {1} {2} {3}", null));

            var code = ResXCodeGenerator.Generate(resx, "My.App", "Resources", isInternal: false, year: GoldenYear);
            AssertMatchesGolden("Public_WithNamespace", code);
        }

        [Test]
        public void Internal_NoNamespace_matches_VS_output()
        {
            var resx = Resx(
                ("Title", "Title", null),
                ("Count", "You have {0} items", null));

            var code = ResXCodeGenerator.Generate(resx, "", "Strings", isInternal: true, year: GoldenYear);
            AssertMatchesGolden("Internal_NoNamespace", code);
        }

        [Test]
        public void NonString_resource_matches_VS_output()
        {
            var resx = Resx(
                ("Logo", "AQID", "System.Byte[], mscorlib"),
                ("Name", "Sample", null));

            var code = ResXCodeGenerator.Generate(resx, "My.App", "Assets", isInternal: false, year: GoldenYear);
            AssertMatchesGolden("NonString", code);
        }

        [Test]
        public void Edge_cases_match_VS_output()
        {
            // multi-line/special chars, escaped braces (no Format method), reordered args,
            // and reserved names (Culture / ResourceManager) which must be skipped.
            var resx = Resx(
                ("Multi", "Line1\nLine2 <b> & \"q\" 'a'", null),
                ("Pct", "100% complete {{literal}}", null),
                ("Reorder", "{1} and {0}", null),
                ("Culture", "should be skipped (reserved)", null),
                ("ResourceManager", "also skipped (reserved)", null));

            var code = ResXCodeGenerator.Generate(resx, "X", "Edge", isInternal: false, year: GoldenYear);
            AssertMatchesGolden("Edge", code);
        }

        // ---- targeted behavioural assertions (independent of the golden files) ----

        [Test]
        public void Format_method_uses_arg_naming_and_suffix()
        {
            var resx = Resx(("Greeting", "Hi {0}", null));
            var code = ResXCodeGenerator.Generate(resx, "N", "R", isInternal: false, year: GoldenYear);
            Assert.That(code, Does.Contain("public static string GreetingFormat(object arg0) {"));
            Assert.That(code, Does.Contain("return string.Format(_resourceCulture, Greeting, arg0);"));
        }

        [Test]
        public void No_placeholders_emits_no_format_method()
        {
            var resx = Resx(("Plain", "no placeholders here", null));
            var code = ResXCodeGenerator.Generate(resx, "N", "R", isInternal: false, year: GoldenYear);
            Assert.That(code, Does.Not.Contain("PlainFormat"));
        }

        [Test]
        public void Noncontiguous_indexes_emit_no_format_method()
        {
            // {0} and {2} but no {1} -> FormatValidator throws -> property but no Format method.
            var resx = Resx(("Gappy", "{0} and {2}", null));
            var code = ResXCodeGenerator.Generate(resx, "N", "R", isInternal: false, year: GoldenYear);
            Assert.That(code, Does.Contain("public static string Gappy {"));
            Assert.That(code, Does.Not.Contain("GappyFormat"));
        }

        [Test]
        public void Metadata_entries_are_skipped()
        {
            var resx = Resx(
                ("Real", "value", null),
                (">>Type.Ref", "System.Drawing.Bitmap", null),
                ("$mimetype", "x", null));
            var code = ResXCodeGenerator.Generate(resx, "N", "R", isInternal: false, year: GoldenYear);
            Assert.That(code, Does.Contain("public static string Real {"));
            Assert.That(code, Does.Not.Contain("Type_Ref"));
            Assert.That(code, Does.Not.Contain("mimetype"));
        }

        [Test]
        public void Internal_flag_controls_accessibility()
        {
            var resx = Resx(("A", "a", null));
            var pub = ResXCodeGenerator.Generate(resx, "N", "R", isInternal: false, year: GoldenYear);
            var intern = ResXCodeGenerator.Generate(resx, "N", "R", isInternal: true, year: GoldenYear);
            Assert.That(pub, Does.Contain("public partial class R {"));
            Assert.That(intern, Does.Contain("internal partial class R {"));
        }
    }
}
