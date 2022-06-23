using Distops.Core.EnumClass;
using FluentAssertions;

namespace Distops.Core.Test
{
    [TestFixture]
    public class EnumClassTests
    {
        public abstract class T2 :
            IEnumClass<T2, T2T1, T2T2>
        {
        }

        public class T2T1 : T2
        {
        }

        public class T2T2 : T2
        {
        }

        public abstract class T3 :
            IEnumClass<T3, T3T1, T3T2, T3T3>
        {
        }

        public class T3T1 : T3
        {
        }

        public class T3T2 : T3
        {
        }

        public class T3T3 : T3
        {
        }

        public abstract class T4 :
            IEnumClass<T4, T4T1, T4T2, T4T3, T4T4>
        {
        }

        public class T4T1 : T4
        {
        }

        public class T4T2 : T4
        {
        }

        public class T4T3 : T4
        {
        }

        public class T4T4 : T4
        {
        }

        public abstract class T5 :
            IEnumClass<T5, T5T1, T5T2, T5T3, T5T4, T5T5>
        {
        }

        public class T5T1 : T5
        {
        }

        public class T5T2 : T5
        {
        }

        public class T5T3 : T5
        {
        }

        public class T5T4 : T5
        {
        }

        public class T5T5 : T5
        {
        }

        [Test]
        public void T1_Test()
        {
            var tt = new T2T1();
            var t = tt.Switch(
                tt1 => 1,
                tt2 => 2);
            t.Should().Be(1);
        }

        [Test]
        public void T2_Test()
        {
            var tt = new T2T2();
            var t = tt.Switch(
                tt1 => 1,
                tt2 => 2);
            t.Should().Be(2);
        }

        [Test]
        public void T3_Test()
        {
            var tt = new T3T3();
            var t = tt.Switch(
                tt1 => 1,
                tt2 => 2,
                tt3 => 3);
            t.Should().Be(3);
        }

        [Test]
        public void T4_Test()
        {
            var tt = new T4T4();
            var t = tt.Switch(
                tt1 => 1,
                tt2 => 2,
                tt3 => 3,
                tt4 => 4);
            t.Should().Be(4);
        }

        [Test]
        public void T5_Test()
        {
            var tt = new T5T5();
            var t = tt.Switch(
                tt1 => 1,
                tt2 => 2,
                tt3 => 3,
                tt4 => 4,
                tt5 => 5);
            t.Should().Be(5);
        }
    }
}