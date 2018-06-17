using System;
using CabinetManager.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetManagerTest.Tests {
    [TestClass]
    public class DosDateTimeTest {
        [TestMethod]
        public void DosLocalDateTimeTest() {
            var dateNow = DateTime.Now;

            DosDateTime.DateTimeToDosDateTime(dateNow, out ushort wFatDate, out ushort wFatTime);
            var outDateTime = DosDateTime.DosDateTimeToDateTime(wFatDate, wFatTime);

            Assert.AreEqual(dateNow.Date, outDateTime.Date);
            Assert.AreEqual(dateNow.Hour, outDateTime.Hour);
            Assert.AreEqual(dateNow.Minute, outDateTime.Minute);
            Assert.IsTrue(Math.Abs(dateNow.Second - outDateTime.Second) < 2);
        }

        [TestMethod]
        public void DosUtcDateTimeTest() {
            var dateNow = DateTime.Now;

            DosDateTime.DateTimeToDosDateTimeUtc(dateNow, out ushort wFatDate, out ushort wFatTime);
            var outDateTime = DosDateTime.DosDateTimeToDateTimeUtc(wFatDate, wFatTime);

            Assert.AreEqual(dateNow.Date, outDateTime.Date);
            Assert.AreEqual(dateNow.Hour, outDateTime.Hour);
            Assert.AreEqual(dateNow.Minute, outDateTime.Minute);
            Assert.IsTrue(Math.Abs(dateNow.Second - outDateTime.Second) < 2);
        }
    }
}