using System.Text.RegularExpressions;
using DockerSyntaxChecker;
using Microsoft.SqlServer.Server;
using NUnit.Framework;
namespace DockerSyntaxCheckerTest
{
    [TestFixture]
    public class CheckerTest
    {
        [TestCase("")]
        [TestCase("    ")]
        public void CheckerReturnsFalseWhenContentIsEmpty(string input)
        {
            Assert.IsFalse(Checker.Check(input, out var mesage));
        }
        [TestCase("", ExpectedResult= false)]
        [TestCase("abcd", ExpectedResult= false)]
        [TestCase("#", ExpectedResult= false)]
        [TestCase("# syntax", ExpectedResult= false)]
        [TestCase("# syntax=", ExpectedResult= false)]
        [TestCase("# syntax=value", ExpectedResult= true)]
        [TestCase("# syntax =value", ExpectedResult= true)]
        [TestCase("# syntax= value", ExpectedResult= true)]
        [TestCase("# syntax = value", ExpectedResult= true)]
        [TestCase("# syNTax=value", ExpectedResult= true)]
        [TestCase("# =docker/dockerfile", ExpectedResult = false)]
        [TestCase("# syntax=docker/dockerfile", ExpectedResult = true)]
        [TestCase("# syntax=docker/dockerfile:1.0", ExpectedResult = true)]
        [TestCase("# syntax=docker.io/docker/dockerfile:1", ExpectedResult = true)]
        [TestCase("# syntax=docker/dockerfile:1.0.0-experimental", ExpectedResult = true)]
        [TestCase("# syntax=example.com/user/repo:tag@sha256:abcdef", ExpectedResult = true)]
        public bool CheckerCorrectlyIdentifiesSyntaxParserDirectives(string input)
        {
            return Checker.IsSyntaxParserDirective(input);
        }
        [TestCase("", ExpectedResult = false)]
        [TestCase("abcd", ExpectedResult = false)]
        [TestCase("#", ExpectedResult = false)]
        [TestCase("# escape", ExpectedResult = false)]
        [TestCase("# escape=", ExpectedResult = false)]
        [TestCase("# escape=\\", ExpectedResult = true)]
        [TestCase("# escape =\\", ExpectedResult = true)]
        [TestCase("# escape= \\", ExpectedResult = true)]
        [TestCase("# escape = \\", ExpectedResult = true)]
        [TestCase("# esCAPe=\\", ExpectedResult = true)]
        [TestCase("# escape=`", ExpectedResult = true)]
        public bool CheckerCorrectlyIdentifiesEscapeParserDirectives(string input)
        {
            return Checker.IsEscapeParserDirective(input);
        }
        [TestCase("# escape= \\", ExpectedResult = false)]
        [TestCase("# comment with line continuation \\", ExpectedResult = false)]
        [TestCase("# syntax=docker/dockerfile:1.0.0-experimental", ExpectedResult = false)]
        [TestCase("# escape=", ExpectedResult = true)]
        [TestCase("#abcde", ExpectedResult = true)]
        [TestCase("# abcde=ijkl", ExpectedResult = true)]
        [TestCase("# abcde=ijkl.?!/@", ExpectedResult = true)]
        public bool CheckerCorrectlyIdentifiesComments(string input)
        {
            return Checker.IsComment(input);
        }
        [TestCase("# hello", ExpectedResult = false)]
        [TestCase("arg", ExpectedResult = false)]
        [TestCase("arg b=a", ExpectedResult = true)]
        [TestCase("arg b =a", ExpectedResult = false)]
        [TestCase("arg b = a", ExpectedResult = false)]
        [TestCase("arg b= a", ExpectedResult = false)]
        [TestCase("arg  b=a", ExpectedResult = true)]
        [TestCase("arg  b", ExpectedResult = true)]
        public bool CheckerCorrectlyIdentifiesArg(string input)
        {
            return Checker.IsArg(input);
        }
        [TestCase("ENV ", ExpectedResult = false)]
        [TestCase("ENV key value", ExpectedResult = true)]
        [TestCase("EnV key value", ExpectedResult = true)]
        [TestCase("ENV key", ExpectedResult = false)]
        [TestCase("ENV key ", ExpectedResult = false)]
        [TestCase("ENV key  ", ExpectedResult = false)]
        [TestCase("ENV key  value with spaces", ExpectedResult = true)]
        [TestCase("ENV key=value", ExpectedResult = true)]
        [TestCase("EnV key=value", ExpectedResult = true)]
        [TestCase("EnV key  =value", ExpectedResult = false)]
        [TestCase("EnV key=  value", ExpectedResult = false)]
        [TestCase("ENV key", ExpectedResult = false)]
        [TestCase("ENV key=", ExpectedResult = false)]
        [TestCase("ENV =key", ExpectedResult = false)]
        [TestCase("ENV key=value with spaces key2=", ExpectedResult = true)]
        [TestCase("ENV \\=value", ExpectedResult = false)]
        public bool CheckerCorrectlyIdentifiesEnv(string input)
        {
            return Checker.IsEnv(input);
        }
        [TestCase("Volume ", ExpectedResult = false)]
        [TestCase("Volume   ", ExpectedResult = false)]
        [TestCase("Volume  a", ExpectedResult = true)]
        [TestCase("Volume  [", ExpectedResult = true)]
        [TestCase("Volume  [ a b ]", ExpectedResult = true)]
        [TestCase("VoLuMe  [ a b ]", ExpectedResult = true)]
        public bool CheckerCorrectlyIdentifiesVolume(string input)
        {
            return Checker.IsVolume(input);
        }
        [TestCase("user ", ExpectedResult = false)]
        [TestCase("user   ", ExpectedResult = false)]
        [TestCase("user user", ExpectedResult = true)]
        [TestCase("USer user", ExpectedResult = true)]
        [TestCase("user hey now", ExpectedResult = true)]
        public bool CheckerCorrectlyIdentifiesUser(string input)
        {
            return Checker.IsUser(input);
        }
        [TestCase("workdir ", ExpectedResult = false)]
        [TestCase("workdir   ", ExpectedResult = false)]
        [TestCase("workdir dir", ExpectedResult = true)]
        [TestCase("woRkDir dir", ExpectedResult = true)]
        [TestCase("workdir dir1 dir2", ExpectedResult = true)]
        public bool CheckerCorrectlyIdentifiesWorkdir(string input)
        {
            return Checker.IsWorkdir(input);
        }
        [TestCase("from ", ExpectedResult = false)]
        [TestCase("from   ", ExpectedResult = false)]
        [TestCase("from img", ExpectedResult = true)]
        [TestCase("froM img", ExpectedResult = true)]
        [TestCase("from img1 img2", ExpectedResult = false)]
        public bool CheckerCorrectlyIdentifiesFrom(string input)
        {
            return Checker.IsFrom(input);
        }
        [TestCase("onbuild ", ExpectedResult = false)]
        [TestCase("onbuild   ", ExpectedResult = false)]
        [TestCase("onbuild inst", ExpectedResult = true)]
        [TestCase("oNbUild inst", ExpectedResult = true)]
        [TestCase("onbuild inst1 inst2", ExpectedResult = true)]
        public bool CheckerCorrectlyIdentifiesOnbuild(string input)
        {
            return Checker.IsOnbuild(input);
        }
        [TestCase("stopsignal ", ExpectedResult = false)]
        [TestCase("stopsignal 140", ExpectedResult = true)]
        [TestCase("stopsignal 1 SIGIOT", ExpectedResult = false)]
        [TestCase("stopsignal SIGIOT", ExpectedResult = true)]
        [TestCase("stopsignal SIGIOT SIGCONT", ExpectedResult = false)]
        [TestCase("stopsignal -12", ExpectedResult = true)]
        public bool CheckerCorrectlyIdentifiesStopsignal(string input)
        {
            return Checker.IsStopSignal(input);
        }
    }
}
