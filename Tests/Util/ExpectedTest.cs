using System;
using Helion.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Util
{
    [TestClass]
    public class ExpectedTest
    {
        [TestMethod]
        public void CreateValueExpected()
        {
            Version version = new Version(1, 2);
            Expected<Version> expected = version;
            
            Assert.IsTrue(expected);
            Assert.AreSame(version, expected.Value);
            Assert.IsNotNull(expected.Error);
        }
        
        [TestMethod]
        public void CreateErrorExpected()
        {
            Expected<Version> expected = "oh no";
            
            Assert.IsFalse(expected);
            Assert.AreEqual("oh no", expected.Error);
            Assert.IsNull(expected.Value);
        }
        
        [TestMethod]
        public void CreateErrorExpectedFromMakeError()
        {
            Expected<Version> expected = Expected<Version>.MakeError("oh no!");
            
            Assert.IsFalse(expected);
            Assert.AreEqual("oh no!", expected.Error);
            Assert.IsNull(expected.Value);
        }
        
        [TestMethod]
        public void CanMapAnExpectedValueToAnotherValue()
        {
            Expected<Version> expected = new Version(1, 2);

            Assert.AreEqual(new Version(2, 1), expected.Map(ToVersionExpected).Value);
            Assert.AreEqual("none", expected.Map(ToEmptyExpected).Error);
            
            Expected<Version> ToVersionExpected(Version v) => new Version(v.Minor, v.Major);
            Expected<Version> ToEmptyExpected(Version v) => Expected<Version>.MakeError("none");
        }
        
        [TestMethod]
        public void CanMakeToAValue()
        {
            // Invoke the first argument function.
            Expected<Version> expected = new Version(1, 2);
            bool invokedValueFunc = false;
            expected.Then(ToTrue, ToFalse);
            Assert.IsTrue(invokedValueFunc);

            // Invoke the second argument function.
            expected = "oh no";
            invokedValueFunc = true;
            expected.Then(ToTrue, ToFalse);
            Assert.IsFalse(invokedValueFunc);
            
            // On error, if no error func is provided then nothing is done.
            invokedValueFunc = true;
            expected.Then(ToTrue);
            Assert.IsTrue(invokedValueFunc);

            void ToTrue(Version v) => invokedValueFunc = true;
            void ToFalse(string error) => invokedValueFunc = false;
        }

        [TestMethod]
        public void CanGetValueOr()
        {
            // Invoke the first argument function.
            Expected<Version> expected = new Version(1, 2);
            Expected<Version> missingExpected = "oh no";
            Version badVersion = new Version(0, 0);

            Assert.AreEqual(new Version(1, 2), expected.ValueOr(() => badVersion));
            Assert.AreSame(badVersion, missingExpected.ValueOr(() => badVersion));
        }
    }
}