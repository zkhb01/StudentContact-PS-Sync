namespace StudentContact_PS_Sync
{
    using System;
    using System.Text.RegularExpressions;

    public class EmailValidator
    {
        // Modified method to accept a logging delegate
        public static string ValidateAndFormatEmail(string email, Action<string> logMethod, out bool isValid)
        {
            // Step 1: Trim and convert to lowercase
            email = email?.Trim().ToLowerInvariant() ?? string.Empty;

            // Step 2: Basic validation check
            isValid = IsEmailValid(email);

            // Step 3: Fix missing period in common domains (.com, .net, .ca)
            if (!string.IsNullOrEmpty(email))
            {
                email = FixMissingDomainPeriod(email);
            }

            // Step 4: Re-validate after attempting fixes
            isValid = IsEmailValid(email);

            // Step 5: Log if the email is still invalid after attempting fixes
            if (!isValid && logMethod != null)
            {
                logMethod($"## Invalid email that cannot be fixed: '{email}'");
                return string.Empty;
            }

            return email;
        }

        private static bool IsEmailValid(string email)
        {
            // Check for null/empty
            if (string.IsNullOrEmpty(email))
                return false;

            // Basic email regex pattern
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, pattern))
                return false;

            // Ensure it has at least one '@' and a domain
            if (!email.Contains("@") || email.StartsWith("@") || email.EndsWith("@"))
                return false;

            // Check for valid domain (at least one character after the last dot)
            int lastDotIndex = email.LastIndexOf('.');
            if (lastDotIndex == -1 || lastDotIndex == email.Length - 1)
                return false;

            return true;
        }

        private static string FixMissingDomainPeriod(string email)
        {
            // Split email into local part and domain
            string[] parts = email.Split('@');
            if (parts.Length != 2)
                return email; // Invalid format, return as-is

            string localPart = parts[0];
            string domain = parts[1];

            // Check for common domains without a period
            if (domain.EndsWith("com") && !domain.Contains("."))
            {
                domain = domain.Replace("com", ".com");
            }
            else if (domain.EndsWith("net") && !domain.Contains("."))
            {
                domain = domain.Replace("net", ".net");
            }
            else if (domain.EndsWith("ca") && !domain.Contains("."))
            {
                domain = domain.Replace("ca", ".ca");
            }
            else if (domain.EndsWith("org") && !domain.Contains("."))
            {
                domain = domain.Replace("org", ".org");
            }
            else if (domain.EndsWith("edu") && !domain.Contains("."))
            {
                domain = domain.Replace("edu", ".edu");
            }
            else if (domain.EndsWith("fr") && !domain.Contains("."))
            {
                domain = domain.Replace("fr", ".fr");
            }

            // Reconstruct email
            return $"{localPart}@{domain}";
        }

        // Example usage in a console app with a mock webDriver.Log
        static void Main(string[] args)
        {
            // Mock logging method (simulating webDriver.Log)
            Action<string> webDriverLog = (message) =>
            {
                Console.WriteLine($"[Log] {message}");
            };

            string[] testEmails =
            {
            "Test@Examplecom",      // Missing period in .com (can be fixed)
            "User@Domain.net ",     // Has period, needs trim
            "InvalidEmail",         // Invalid format (will log)
            "Another@SiteCA",       // Missing period in .ca (can be fixed)
            "Mixed@Case.COM"        // Needs lowercase
        };

            foreach (var email in testEmails)
            {
                string formattedEmail = ValidateAndFormatEmail(email, webDriverLog, out bool isValid);
                Console.WriteLine($"Original: {email}");
                Console.WriteLine($"Formatted: {formattedEmail}");
                Console.WriteLine($"Is Valid: {isValid}\n");
            }
        }
    }
}
