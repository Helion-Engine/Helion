using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions
{
    public class IEnumerableExtensions
    {
        [Fact(DisplayName = "Check if an enumerable set is empty")]
        public void CheckEmptyIEnumerable()
        {
            SomeEnumerable e = new(null);
            e.Empty().Should().BeTrue();
        }
        
        [Fact(DisplayName = "Check if a non-empty enumerable is empty")]
        public void CheckNonEmptyIEnumerable()
        {
            SomeEnumerable e = new(1);
            e.Empty().Should().BeFalse();
        }

        private class SomeEnumerable : IEnumerable<int>
        {
            private readonly int? m_value;

            public SomeEnumerable(int? value)
            {
                m_value = value;
            }
            
            public IEnumerator<int> GetEnumerator()
            {
                if (m_value != null)
                    yield return m_value.Value;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
