using System;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;
namespace DockerSyntaxChecker
{
    public static class Checker
    {
        public static bool Check(string content, out string outputMessage)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                outputMessage = "File does not contain instructions";
                return false;
            }
            bool isFirstFrom = false;
            var lines = content.Split(new []{"\n"}, StringSplitOptions.None);
            var index = 0;
            foreach (var line in lines)
            {
                //TODO: handle line continuation
                //TODO: handle empty lines
                var normalizedLine = line.TrimStart();
                if (normalizedLine.Length == 0)
                {
                    index++;
                    continue;
                }
                var instructionType = GetInstructionType(normalizedLine);
                if (instructionType == InstructionType.Unknown)
                {
                    outputMessage = $"Line {index} is not a valid docker instruction";
                    return false;
                }
                if (instructionType == InstructionType.FROM)
                {
                    isFirstFrom = true;
                }
                if (IsInstructionForbiddenBeforeFROM(instructionType) && !isFirstFrom)
                {
                    outputMessage = "FROM may be only preceeded by ARG, Parser directive or comment";
                    return false;
                }
                index++;
            }
            outputMessage = "Dockerfile is valid";
            return true;
        }
        public static bool IsInstructionForbiddenBeforeFROM(InstructionType instructionType)
        {
            var validInstructionBeforeFrom = new InstructionType[] {InstructionType.SYNTAX, InstructionType.ESCAPE, InstructionType.ARG, InstructionType.COMMENT};
            return !Array.Exists(validInstructionBeforeFrom, inst => inst == instructionType);
        }
        public static InstructionType GetInstructionType(string line)
        {
            if (IsFrom(line))
                return InstructionType.FROM;
            if(IsSyntaxParserDirective(line))
                return InstructionType.SYNTAX;
            if (IsEscapeParserDirective(line))
                return InstructionType.ESCAPE;
            if (IsArg(line))
                return InstructionType.ARG;
            if (IsComment(line))
                return InstructionType.COMMENT;
            if (IsEnv(line))
                return InstructionType.ENV;
            if (IsOnbuild(line))
                return InstructionType.ONBUILD;
            if (IsStopSignal(line))
                return InstructionType.STOPSIGNAL;
            if (IsUser(line))
                return InstructionType.USER;
            if (IsVolume(line))
                return InstructionType.VOLUME;
            if (IsWorkdir(line))
                return InstructionType.WORKDIR;
            return InstructionType.Unknown;
        }
        public static bool IsSyntaxParserDirective(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx = new Regex(@"^\#\s*syntax\s*\=.+",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rx.IsMatch(line);
        }
        public static bool IsEscapeParserDirective(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx = new Regex(@"^\#\s*escape\s*\=\s*[\\|\`]",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rx.IsMatch(line);
        }
        public static bool IsParserDirective(string line)
        {
            return IsEscapeParserDirective(line) || IsSyntaxParserDirective(line);
        }
        //TODO: line continuation
        public static bool IsComment(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx = new Regex(@"^\#.*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rx.IsMatch(line) && !IsParserDirective(line) && !line.EndsWith("\\");
        }
        public static bool IsArg(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx;
            //TODO: try to make it better
            if(line.Contains("="))
                rx = new Regex(@"^arg\s+\S+\=\S+",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
            else
            {
                rx = new Regex(@"^arg\s+\S+",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            return rx.IsMatch(line);
        }
        // Although the doc says that env can defined multiple key values on the same line
        // In reality docker is only checking the syntax is correct for the first key value pair
        // Its not specified in the doc but when using syntax with = spaces around it are not allowed
        //TODO: case with ENV \\=value should be an error
        public static bool IsEnv(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx;
            if (line.Contains("="))
            {
                rx = new Regex(@"^env\s+\S+\=\S+.*",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            else
            {
                rx = new Regex(@"^env\s+\S+\s+\S.*",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            return rx.IsMatch(line);
        }
        // Docker build does not seem to care about the syntax proposed for Volume
        // For example Volume [\" does not cause a syntax error
        // TODO: handle case of line continuation
        public static bool IsVolume(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx = new Regex(@"^volume\s+\S+.*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rx.IsMatch(line);
        }
        public static bool IsUser(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx = new Regex(@"^user\s+\S+.*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rx.IsMatch(line);
        }
        public static bool IsWorkdir(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx = new Regex(@"^workdir\s+\S+.*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rx.IsMatch(line);
        }
        public static bool IsFrom(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx = new Regex(@"^from\s+\S+\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rx.IsMatch(line);
        }
        //TODO: Chaining onbuild is not allowed
        public static bool IsOnbuild(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx = new Regex(@"^onbuild\s+\S+.*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rx.IsMatch(line);
        }
        // Today the doc is inconsistent with the compiler
        // The value of stopsignal can be a signed number contrary to the doc
        public static bool IsStopSignal(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            Regex rx = new Regex(@"^stopsignal\s+(SIGABRT|SIGIOT|SIGALRM|SIGVTALRM|SIGPROF|SIGBUS|SIGCHLD|SIGCONT|SIGFPE|SIGHUP|SIGILL|SIGINT|SIGKILL|SIGPIPE|SIGPOLL|SIGRTMIN|SIGRTMAX|SIGQUIT|SIGSEGV|SIGSTOP|SIGSYS|SIGTERM|SIGTSTP|SIGTTIN|SIGTTOU|SIGTRAP|SIGURG|SIGUSR1|SIGUSR2|SIGXCPU|SIGXFSZ|SIGWINCH|-?[0-9]+)\s*$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return rx.IsMatch(line);
        }
    }
}
