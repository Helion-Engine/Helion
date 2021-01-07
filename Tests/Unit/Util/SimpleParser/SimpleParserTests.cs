using Microsoft.VisualStudio.TestTools.UnitTesting;
using Helion.Util.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unit.Util.SimpleParserTests
{
    [TestClass()]
    public class SimpleParserTests
    {
        [TestMethod()]
        public void GetCurrentLineTest()
        {
            string data = @"Line1
Line2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual(0, parser.GetCurrentLine());
            parser.ConsumeString();
            Assert.AreEqual(1, parser.GetCurrentLine());
        }

        [TestMethod()]
        public void IsDoneTest()
        {
            string data = @"Line1
Line2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.IsFalse(parser.IsDone());
            parser.ConsumeString();
            Assert.IsFalse(parser.IsDone());
            parser.ConsumeString();
            Assert.IsTrue(parser.IsDone());
        }

        [TestMethod()]
        public void PeekTest()
        {
            string data = @"Line1
Line2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.IsTrue(parser.Peek('L'));
            Assert.IsTrue(parser.Peek('l'));

            Assert.IsTrue(parser.Peek("Line1"));
            Assert.IsTrue(parser.Peek("line1"));
        }

        [TestMethod()]
        public void ConsumeStringTest()
        {
            string data = @"Line1 Data1
Line2 Data2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual("Line1", parser.ConsumeString());
            Assert.AreEqual("Data1", parser.ConsumeString());
            Assert.AreEqual(1, parser.GetCurrentLine());
            Assert.AreEqual("Line2", parser.ConsumeString());
            Assert.AreEqual("Data2", parser.ConsumeString());
        }

        [TestMethod()]
        public void ConsumeStringEqualTest()
        {
            string data = @"Line1 Data1
Line2 Data2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            try
            {
                parser.ConsumeString("Line1");
            }
            catch
            {
                Assert.Fail("Consume should be Line1");
            }
        }

        [TestMethod()]
        public void ConsumeIntegerTest()
        {
            string data = "1 2 3 69";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual(1, parser.ConsumeInteger());
            Assert.AreEqual(2, parser.ConsumeInteger());
            Assert.AreEqual(3, parser.ConsumeInteger());
            Assert.AreEqual(69, parser.ConsumeInteger());
        }

        [TestMethod()]
        public void ConsumeBoolTest()
        {
            string data = "true false True False";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.IsTrue(parser.ConsumeBool());
            Assert.IsFalse(parser.ConsumeBool());
            Assert.IsTrue(parser.ConsumeBool());
            Assert.IsFalse(parser.ConsumeBool());
        }

        [TestMethod()]
        public void ConsumeCharTest()
        {
            string data = "t e s t test";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            try
            {
                parser.Consume('T');
                parser.Consume('e');
                parser.Consume('S');
                parser.Consume('t');
            }
            catch
            {
                Assert.Fail("Should not throw");
            }

            bool failed = false;
            try
            {
                parser.Consume('T');
            }
            catch
            {
                failed = true;
            }

            Assert.IsTrue(failed);
        }

        [TestMethod()]
        public void ConsumeLineTest()
        {
            string data = @"Line1 Data1
                Line2 Data2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual(0, parser.GetCurrentLine());
            parser.ConsumeLine();
            Assert.AreEqual(1, parser.GetCurrentLine());
            Assert.AreEqual("Line2", parser.ConsumeString());
        }

        [TestMethod()]
        public void SingleLineCommentTest()
        {
            string data = @"Line1 Data1
                //This is a comment
                Line2 Data2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual("Line1", parser.ConsumeString());
            Assert.AreEqual("Data1", parser.ConsumeString());
            Assert.AreEqual("Line2", parser.ConsumeString());
            Assert.AreEqual("Data2", parser.ConsumeString());
        }

        [TestMethod()]
        public void SingleLineCommentMiddleTest()
        {
            string data = @"Line1 //This is a comment Data1
                Line2 Data2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual("Line1", parser.ConsumeString());
            Assert.AreEqual("Line2", parser.ConsumeString());
            Assert.AreEqual("Data2", parser.ConsumeString());
        }

        [TestMethod()]
        public void CommentMultiLineTest()
        {
            string data = @"Line1 Data1
                /* This is a comment */
                Line2 Data2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual("Line1", parser.ConsumeString());
            Assert.AreEqual("Data1", parser.ConsumeString());
            Assert.AreEqual("Line2", parser.ConsumeString());
            Assert.AreEqual("Data2", parser.ConsumeString());
        }

        [TestMethod()]
        public void CommentMultiLineTest2()
        {
            string data = @"Line1 Data1
                /* This is a comment 
                    that spans
                    multiple lines */
                Line2 Data2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual("Line1", parser.ConsumeString());
            Assert.AreEqual("Data1", parser.ConsumeString());
            Assert.AreEqual("Line2", parser.ConsumeString());
            Assert.AreEqual("Data2", parser.ConsumeString());
        }

        [TestMethod()]
        public void CommentMultiLineTest3()
        {
            string data = @"Line1 Data1
                /*******/
                Line2 Data2";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual("Line1", parser.ConsumeString());
            Assert.AreEqual("Data1", parser.ConsumeString());
            Assert.AreEqual("Line2", parser.ConsumeString());
            Assert.AreEqual("Data2", parser.ConsumeString());
        }

        [TestMethod()]
        public void CommentMultiLineMiddle()
        {
            string data = @"Line1 /*lol comment*/ Data1";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            Assert.AreEqual("Line1", parser.ConsumeString());
            Assert.AreEqual("Data1", parser.ConsumeString());
        }
    }
}