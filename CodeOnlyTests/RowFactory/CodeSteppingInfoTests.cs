using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using CodeOnlyStoredProcedure.DataTransformation;
using CodeOnlyStoredProcedure.RowFactory;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#if NET40
namespace CodeOnlyTests.Net40.RowFactory
#else
namespace CodeOnlyTests.RowFactory
#endif
{
    [TestClass]
    public class CodeSteppingInfoTests
    {
        [TestClass]
        public class BeginBlock
        {
            [TestMethod]
            public void AppendsOpenBrace_ToSourceCode_AtCurrentIndentation()
            {
                var toTest = new CodeSteppingInfo(typeof(string));
                toTest.StartParseMethod("test");
                var orig = toTest.SourceCode.ToString();

                toTest.BeginBlock();

                toTest.SourceCode.ToString().Should().Be(orig + "            {\r\n");
            }

            [TestMethod]
            public void Indents_SubsequentCode()
            {
                var toTest = new CodeSteppingInfo(typeof(string));
                toTest.StartParseMethod("test");
                var orig = toTest.SourceCode.ToString();

                toTest.BeginBlock();
                toTest.MarkLine("testStatement();");

                toTest.SourceCode.ToString().Should().Be(orig + "            {\r\n                testStatement();\r\n");
            }
        }

        [TestClass]
        public class EndBlock
        {
            [TestMethod]
            public void AppendsCloseBrace_ToSourceCode_AtPreviousIndentation()
            {
                var toTest = new CodeSteppingInfo(typeof(string));
                toTest.StartParseMethod("test");
                var orig = toTest.SourceCode.ToString();

                toTest.EndBlock();

                toTest.SourceCode.ToString().Should().Be(orig + "        }\r\n");
            }

            [TestMethod]
            public void SubsequentCode_IsAt_SameIndentationAsCloseBrace()
            {
                var toTest = new CodeSteppingInfo(typeof(string));
                toTest.StartParseMethod("test");
                var orig = toTest.SourceCode.ToString();

                toTest.EndBlock();
                toTest.MarkLine("testStatement();");

                toTest.SourceCode.ToString().Should().Be(orig + "        }\r\n        testStatement();\r\n");
            }
        }

        [TestClass]
        public class MarkLine
        {
            [TestMethod]
            public void SetsLine_AndColumnInformation_Correctly()
            {
                var toTest = new CodeSteppingInfo(typeof(string));
                toTest.StartParseMethod("test");

                var orig      = toTest.SourceCode.ToString();
                var lineCount = orig.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length;
                var code      = "some_value = AMethodCall(withArgument);";

                var expr = toTest.MarkLine(code);
                expr.StartLine.Should().Be(lineCount, "because it should mark the statement in source code at the next line");
                expr.EndLine.Should().Be(lineCount, "because it should mark the statement in source code to be only one line");
                expr.EndColumn.Should().Be(expr.StartColumn + code.Length, "because the DebugInfo should encompass the entire statement");
            }
        }
    }
}
