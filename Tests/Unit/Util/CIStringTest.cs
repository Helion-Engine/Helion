using System;
using FluentAssertions;
using Helion.Util;
using Xunit;

namespace Helion.Tests.Unit.Util
{
    public class CIStringTest
    {
        [Theory(DisplayName = "Create CIString from constructor")]
        [InlineData("")]
        [InlineData("hello")]
        [InlineData("some LONGER word")]
        public void FromConstructor(string data)
        {
            CIString ciStr = new CIString(data);

            ciStr.Should().Equal(data);
        }
        
        [Theory(DisplayName = "Create CIString from implicit assignment")]
        [InlineData("")]
        [InlineData("hello")]
        [InlineData("some LONGER word")]
        public void FromImplicitAssignment(string data)
        {
            CIString ciStr = data;

            ciStr.Should().Equal(data);
        }
        
        [Theory(DisplayName = "Check if CIString is empty")]
        [InlineData("")]
        [InlineData("hello")]
        [InlineData("some LONGER word")]
        public void CheckEmpty(string data)
        {
            CIString ciStr = data;

            ciStr.Length.Should().Be(0);
        }
        
        [Theory(DisplayName = "Check CIString length")]
        [InlineData("")]
        [InlineData("hello")]
        [InlineData("some LONGER word")]
        public void CheckLength(string data)
        {
            CIString ciStr = data;

            ciStr.Length.Should().Be(data.Length);
        }
        
        [Fact(DisplayName = "Get CIString characters by index")]
        public void AccessIndex()
        {
            const string str = "hello";
            CIString ciStr = str;

            for (int i = 0; i < str.Length; i++)
                ciStr[i].Should().Be(str[i]);

            Action outOfBounds = () => { var _ = ciStr[ciStr.Length]; };
            outOfBounds.Should().Throw<IndexOutOfRangeException>();
        }
        
        [Fact(DisplayName = "Check CIString equalities")]
        public void CheckEqualities()
        {
            CIString ciStr = "Hello";
            (ciStr == null).Should().BeFalse();
            (null == ciStr).Should().BeFalse();
            
            string str = "hello";
            CIString ciStrCopy = "helLO";
            (str == ciStr).Should().BeTrue();
            (ciStr == str).Should().BeTrue();
            (ciStr == ciStr).Should().BeTrue();
            (ciStr == ciStrCopy).Should().BeTrue();
            (ciStrCopy == ciStr).Should().BeTrue();
            (str != ciStr).Should().BeFalse();
            (ciStr != str).Should().BeFalse();
            (ciStr != ciStrCopy).Should().BeFalse();
            (ciStrCopy != ciStr).Should().BeFalse();

            string strOther = "hello!";
            CIString ciStrOther = "hEllo!";
            (strOther == ciStr).Should().BeFalse();
            (ciStr == strOther).Should().BeFalse();
            (ciStr == ciStrOther).Should().BeFalse();
            (ciStrOther == ciStr).Should().BeFalse();
            (strOther != ciStr).Should().BeTrue();
            (ciStr != strOther).Should().BeTrue();
            (ciStr != ciStrOther).Should().BeTrue();
            (ciStrOther != ciStr).Should().BeTrue();

            ciStr.Equals(str).Should().BeTrue();
        }

        [Fact(DisplayName = "Check null CIString equalities")]
        public void CheckNullEqualities()
        {
            CIString? ciStr = null;
            CIString? otherCiStr = null;
            (ciStr == null).Should().BeTrue();
            (null == ciStr).Should().BeTrue();
            (ciStr == otherCiStr).Should().BeTrue();
            (otherCiStr == ciStr).Should().BeTrue();
            
            ciStr = "hello";
            (ciStr == null).Should().BeFalse();
            (null == ciStr).Should().BeFalse();
            (ciStr == otherCiStr).Should().BeFalse();
            (otherCiStr == ciStr).Should().BeFalse();
        }
    }
}
