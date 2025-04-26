using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

public class StringUtil
{
    // Dictionary of full street types to abbreviations
    private static readonly Dictionary<string, string> StreetAbbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Ave.", "Ave" },
        { "Avenue", "Ave" },
        { "Blvd.", "Blvd" },
        { "Boulevard", "Blvd" },
        { "Cir.", "Cir" },
        { "Circle", "Cir" },
        { "Cm.", "Cm" },
        { "Common", "Cm" },
        { "Court", "Ct" },
        { "Ct.", "Ct" },
        { "Cresent", "Cres" },
        { "Cres.", "Cres" },
        { "Cr.", "Cres" },
        { "Cr", "Cres" },
        { "Dr.", "Dr" },
        { "Drive", "Dr" },
        { "Gr.", "Gr" },
        { "Green", "Gr" },
        { "Highway", "Hwy" },
        { "Hwy.", "Hwy" },
        { "Lane", "Ln" },
        { "Ln.", "Ln" },
        { "Parkway", "Pkwy" },
        { "Pkwy.", "Pkwy" },
        { "Park", "Pk" },
        { "Pk.", "Pk" },
        { "Pl.", "Pl" },
        { "Place", "Pl" },
        { "Rd.", "Rd" },
        { "Road", "Rd" },
        { "Sq.", "Sq" },
        { "Square", "Sq" },
        { "St.", "St" },
        { "Street", "St" },
        { "Ter.", "Terr" },
        { "Terr.", "Terr" },
        { "Terrace", "Terr" },
        { "Ter", "Terr" },

        // Common Cm, Park, Terr, Green Gr
        // Add more mappings as needed
    };

    public static string AbbreviateStreet(string street)
    {
        if (string.IsNullOrWhiteSpace(street))
            return street;

        // Split into words
        string[] words = street.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2)
            return street; // Too short to process

        // Check if first word is numeric
        if (char.IsDigit(words[0][0]))
        {
            // First word is number, second is street name, rest is type + suffix
            string prefix = $"{words[0]} {words[1]}"; // Keep "115 Marquis" intact
            string remainder = string.Join(" ", words.Skip(2)); // "Court SE"

            // Check and abbreviate only the remainder
            string abbreviatedRemainder = remainder;
            foreach (var pair in StreetAbbreviations)
            {
                string abbrevPattern = $@"\b{Regex.Escape(pair.Value)}\b";
                if (Regex.IsMatch(abbreviatedRemainder, abbrevPattern, RegexOptions.IgnoreCase))
                    continue; // Skip if abbreviation already present

                string fullWordPattern = $@"\b{pair.Key}\b";
                string newRemainder = Regex.Replace(abbreviatedRemainder, fullWordPattern, pair.Value, RegexOptions.IgnoreCase);
                if (newRemainder != abbreviatedRemainder) // Check if a replacement occurred
                {
                    abbreviatedRemainder = newRemainder;
                    break; // Exit loop after first replacement
                }
            }
            // Recombine
            return string.IsNullOrEmpty(abbreviatedRemainder) ? prefix : $"{prefix} {abbreviatedRemainder}";
        }
        else
        {
            // If no leading number, apply to whole string (fallback)
            return AbbreviateWholeString(street);
        }
    }

    public static string AbbreviateWholeString(string street)
    {
        string result = street;
        foreach (var pair in StreetAbbreviations)
        {
            string abbrevPattern = $@"\b{Regex.Escape(pair.Value)}\b";
            if (Regex.IsMatch(result, abbrevPattern, RegexOptions.IgnoreCase))
                continue;

            string fullWordPattern = $@"\b{pair.Key}\b";
            result = Regex.Replace(result, fullWordPattern, pair.Value, RegexOptions.IgnoreCase);
        }
        return result;
    }


    // Optional: Reverse method to expand abbreviations to full words
    public static string ExpandStreet(string street)
    {
        if (string.IsNullOrEmpty(street))
            return street;

        string result = street;
        foreach (var pair in StreetAbbreviations)
        {
            // Pattern matches abbreviation (e.g., "Rd\." or "Rd")
            string pattern = $@"\b{pair.Value.Replace(".", @"\.")}\b";
            result = Regex.Replace(result, pattern, pair.Key, RegexOptions.IgnoreCase);
        }

        return result;
    }

    // Test the methods
    public static void Main()
    {
        string[] testStreets =
        {
            "115 Marquis COURT SE",
            "456 Oak Road",
            "789 Pine Avenue",
            "123 Broadway Street",
            "999 Highway Dr"
        };

        Console.WriteLine("Abbreviated:");
        foreach (string street in testStreets)
        {
            string abbreviated = AbbreviateStreet(street);
            Console.WriteLine(abbreviated);
        }

        Console.WriteLine("\nExpanded:");
        foreach (string street in testStreets)
        {
            string expanded = ExpandStreet(AbbreviateStreet(street));
            Console.WriteLine(expanded);
        }
    }

    public static bool CompareFirstNames(string firstName1, string firstName2)
    {
        // Normalize inputs
        string normalized1 = GetFirstWord(firstName1);
        string normalized2 = GetFirstWord(firstName2);

        // Compare case-insensitively
        return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
    }

    public static bool RecordhasChanges(StudentContact contact, string part = "")
    {
        bool result = false;
        bool studentChanged = false;
        bool contact1Changed = false;
        bool contact2Changed = false;

        if (contact.Q5NoUpdate == "FALSE" && !string.IsNullOrWhiteSpace(contact.Q8NewStuAdd)) // student address change
        {
            // If contact.Q8NewStuAdd has value and Ps address list is empty, add Q8
            // If contact.Q4StuCurrAdd and contact.Q8NewStuAdd have values, Check if Q4 in the PS address list.
            //    If found, update with Q8
            //    If not, check if Q8 in the list
            //       If found, update it with Q8 (apply Formats, City/State/Zip),
            //       If not found and there is only one in PS, update with Q8
            //       If not found and there are more than one in PS, ??
            // If contact.Q4StuCurrAdd is empty and contact.Q8NewStuAdd is not, Check if Q8 in the PS address list.
            //    Follow from above line: If not, check if Q8 in the list
            studentChanged = true;
            if (part == "Student") return true;
        }
        if (part == "Student") return false;

        if (
               (contact.Q22PG1NoUpdate == "FALSE" && !string.IsNullOrWhiteSpace(contact.Q25NewPG1Add))
            // contact 1 address change
            // If contact.Q25NewPG1Add has value and Ps address list is empty, add Q25
            // If contact.Q21PG1Add and contact.Q25NewPG1Add have values, Check if Q21 in the PS address list.
            //    If found, update with Q25
            //    If not, check if Q25 in the list
            //       If found, update it with Q25 (apply Formats, City/State/Zip),
            //       If not found and there is only one in PS, update with Q25
            //       If not found and there are more than one in PS, ??
            // If contact.Q21PG1Add is empty and contact.Q25NewPG1Add is not, Check if Q25 in the PS address list.
            //    Follow from above line: If not, check if Q25 in the list

            || (contact.Q22PG1NoUpdate == "FALSE" && !string.IsNullOrWhiteSpace(contact.Q29PG1Email))
            // contact 1 email change
            // If contact.Q29PG1Email has value and Ps email list is empty, add Q29     
            // If contact.Q29PG1Email has value, check if more than 1 email in PS email list   
            //    If only 1 in the list, update it with Q29
            //    If more than 1 in the list, Check if more than 1 is Primary
            //      If only 1 in the list, update it with Q29
            //      If more than 1 in the list, Check from the primary set if more than 1 is current
            //      If only 1 is Current, update it with Q29
            //      If more than 1 is Current, update the first one???

            || (contact.Q22PG1NoUpdate == "FALSE" && !string.IsNullOrWhiteSpace(contact.Q33PG1Ph1))
            // contact 1 phone 1 change
            // If contact.Q33PG1Ph1 has value and Ps phone list is empty, add Q33
            // If contact.Q33PG1Ph1 has value, check if more than 1 phone in Ps phone list
            // If only 1 phone in the list, update it with Q33
            // If more than 1 phone in the list, Check if more than 1 has the same phoneType
            // If only 1 has the same phoneType, update it with Q33 
            // If none have the same phoneType, Add Q33
            // If more than 1 have the same phoneType, Check if more than 1 on these has the same Primary setting
            // If only 1 matches, update it with Q33
            // If more than 1 matches, update the first one ???

            || (contact.Q22PG1NoUpdate == "FALSE" && !string.IsNullOrWhiteSpace(contact.Q38PG1Ph2)) // contact 1 phone 2 change
            || (contact.Q22PG1NoUpdate == "FALSE" && !string.IsNullOrWhiteSpace(contact.Q43PG1Ph3)) // contact 1 phone 2 change
            )
        {
            contact1Changed = true;
            if (part == "Contact1") return true;
        }
        if (part == "Contact1") return false;

        if (
            (contact.Q56SameAddress == "FALSE" && !string.IsNullOrWhiteSpace(contact.Q59PG2Add))
            // contact 2 address change
            // If contact.Q59PG2Add has value and Ps address list is empty, add Q59
            // If contact.Q59PG2Add has value, check if Q59 in the list
            //   If found, update it with Q59 (apply Formats, City/State/Zip),
            //   If not found and there is only 1 in PS, update with Q59
            //   If not found and there are more than 1 in PS, ??

            || (!string.IsNullOrWhiteSpace(contact.Q63PG2Email)) 
            // contact 2 email change
            // rules same as contact 1 email

            || (!string.IsNullOrWhiteSpace(contact.Q67PG2Ph1))
            // contact 2 phone 1 change
            // rules same as contact 1 phones

            || (!string.IsNullOrWhiteSpace(contact.Q72PG2Ph2)) // contact 2 phone 2 change
            || (!string.IsNullOrWhiteSpace(contact.Q77PG2Ph3)) // contact 2 phone 3 change
            )
        {
            contact2Changed = true;
            if (part == "Contact2") return true;
        }
        if (part == "Contact2") return false;


        return studentChanged || contact1Changed || contact2Changed;
    }

    private static string GetFirstWord(string name)
    {
        // Handle null or empty
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Split by spaces and take first word
        string[] words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? words[0] : string.Empty;
    }

    public static bool isPhoneNull(string phone)
    {
        string template = "___-___-____";
        if (string.IsNullOrWhiteSpace(phone)) return true;
        if (phone.Trim() == template) return true;
        return false;
    }
}