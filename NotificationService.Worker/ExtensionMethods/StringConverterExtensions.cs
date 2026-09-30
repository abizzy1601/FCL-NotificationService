using System.Text.RegularExpressions;

namespace NotificationService.Worker.ExtensionMethods
{
    public static class StringConverterExtensions
    {
        public static string ConvertToInternationalFormat(this string phoneNumber)
        {
            if (Regex.IsMatch(phoneNumber, "^0\\d{10}$"))
            {
                return "+234" + phoneNumber.Substring(1);
            }

            if (Regex.IsMatch(phoneNumber, "^\\+234\\d{10}$"))
            {
                return phoneNumber;
            }

            throw new ArgumentException("Invalid Phone number format.");
        }

        /// <summary>
        /// Handles loan DB phone formats such as "08012345678" or "08012345678, Lagos".
        /// Strips everything after the first comma, removes non-digit characters,
        /// prepends a leading zero for bare 10-digit numbers, then normalises to E.164.
        /// </summary>
        public static string ExtractAndNormalizePhone(this string rawPhone)
        {
            if (string.IsNullOrWhiteSpace(rawPhone))
                throw new ArgumentException("Phone number is null or empty.");

            var candidate = rawPhone.Split(',')[0].Trim();
            candidate = Regex.Replace(candidate, @"\D", "");

            // Bare 10-digit local number missing the leading zero — prepend it
            if (Regex.IsMatch(candidate, @"^\d{10}$"))
                candidate = "0" + candidate;

            return candidate.ConvertToInternationalFormat();
        }
    }
}
