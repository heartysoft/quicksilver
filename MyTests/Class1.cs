using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MyTests
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public void MyTest()
        {
            Assert.AreEqual(2, 2);
        }
    }
}
