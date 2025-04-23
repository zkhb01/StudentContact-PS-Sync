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

    private static string GetFirstWord(string name)
    {
        // Handle null or empty
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Split by spaces and take first word
        string[] words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? words[0] : string.Empty;
    }

}