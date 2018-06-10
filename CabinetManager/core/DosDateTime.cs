using System;

namespace CabinetManager.core {

    public static class DosDateTime {

        public static DateTime DosDateTimeToDateTime(ushort wFatDate, ushort wFatTime) {
            // for the date part :
            // 16  =  7 +           4 +     5
            // bits : Y Y Y Y Y Y Y M M M M D D D D D
            // for the time part : 
            // 16  =  5 +       6 +         5
            // bits : H H H H H M M M M M M S S S S S
            // for the seconds, we can only store numbers up to 31 (in 5 bits), hence why the seconds /2, we only have a precision of 2s
            return new DateTime(((wFatDate >> 9) & 0b1111_111) + 1980, (wFatDate >> 5) & 0b1111, wFatDate & 0b1111_1, (wFatTime >> 11) & 0b1111_1, (wFatTime >> 5) & 0b1111_11, (wFatTime & 0b1111_1) * 2);
        }

        public static void DateTimeToDosDateTime(DateTime time, out ushort wFatDate, out ushort wFatTime) {
            wFatDate = (ushort) (((time.Year - 1980) << 9) + (time.Month << 5) + time.Day);
            wFatTime = (ushort) ((time.Hour << 11) + (time.Minute << 5) + time.Second / 2);
        }

    }
}