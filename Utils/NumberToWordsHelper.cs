using System;

namespace EduBridge.Utils
{
    public static class NumberToWordsHelper
    {
        private static readonly string[] Ones = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        private static readonly string[] Groups = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ", "tỷ tỷ" };

        public static string ConvertToWords(decimal number)
        {
            if (number == 0) return "Không đồng chẵn";

            long integerPart = (long)Math.Floor(number);
            string words = ConvertIntegerToWords(integerPart);

            words = words.Substring(0, 1).ToUpper() + words.Substring(1).Trim();
            return words + " đồng chẵn";
        }

        private static string ConvertIntegerToWords(long number)
        {
            if (number == 0) return "không";

            string words = "";
            int groupIndex = 0;

            while (number > 0)
            {
                int groupValue = (int)(number % 1000);
                if (groupValue > 0)
                {
                    string groupWords = ConvertGroupToWords(groupValue, number > 999);
                    words = groupWords + " " + Groups[groupIndex] + " " + words;
                }
                number /= 1000;
                groupIndex++;
            }

            return words.Trim().Replace("  ", " ");
        }

        private static string ConvertGroupToWords(int number, bool hasHundreds)
        {
            int hundreds = number / 100;
            int tens = (number % 100) / 10;
            int ones = number % 10;

            string words = "";

            if (hundreds > 0 || hasHundreds)
            {
                words += Ones[hundreds] + " trăm ";
            }

            if (tens > 1)
            {
                words += Ones[tens] + " mươi ";
                if (ones == 1) words += "mốt";
                else if (ones == 5) words += "lăm";
                else if (ones > 0) words += Ones[ones];
            }
            else if (tens == 1)
            {
                words += "mười ";
                if (ones == 5) words += "lăm";
                else if (ones > 0) words += Ones[ones];
            }
            else // tens == 0
            {
                if (ones > 0)
                {
                    if (hundreds > 0 || hasHundreds) words += "lẻ ";
                    words += Ones[ones];
                }
            }

            return words.Trim();
        }
    }
}
