using System;
using System.Runtime.InteropServices;
using CabinetManager.core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetManagerTest {

    [TestClass]
    public class DosDateTimeTest {

        [TestMethod]
        public void TestMethod1() {
            var dateNow = DateTime.Now;
            
            DosDateTime.DateTimeToDosDateTime(dateNow, out ushort wFatDate2, out ushort wFatTime2);
            var outDateTime2 = DosDateTime.DosDateTimeToDateTime(wFatDate2, wFatTime2);

            Assert.AreEqual(dateNow.Date, outDateTime2.Date);
            Assert.AreEqual(dateNow.Hour, outDateTime2.Hour);
            Assert.AreEqual(dateNow.Minute, outDateTime2.Minute);
            Assert.IsTrue(Math.Abs(dateNow.Second - outDateTime2.Second) < 2);
        }
    }
}