public class StudentContact
{
    // Student-related fields
    public string Campus { get; set; }              // "Campus"
    public string Status { get; set; }              // "Status"
    public DateTime DateModified { get; set; }      // "Date Modified"
    public DateTime? DateCompleted { get; set; }    // "Date Completed" (nullable if optional)
    public string StudentId { get; set; }           // "Student ID"
    public string LastName { get; set; }            // "Last Name"
    public string FirstName { get; set; }           // "First Name"
    public string Grade { get; set; }               // "Grade"
    public string HR { get; set; }                  // "HR" (possibly Homeroom)

    // Student additional info (Q1-Q12)
    public string Q1StuFN { get; set; }             // "Q1 - Stu FN" (Student First Name)
    public string Q2StuMN { get; set; }             // "Q2 - Stu MN" (Student Middle Name)
    public string Q3StuLN { get; set; }             // "Q3 - Stu LN" (Student Last Name)
    public string Q4StuCurrAdd { get; set; }        // "Q4 - Stu Curr Add" (Current Address)
    public string Q5NoUpdate { get; set; }          // "Q5 - No Update"
    public string Q6UpdateAdd { get; set; }         // "Q6 - Update Add" (Updated Address)
    public string Q7NewStuUnit { get; set; }        // "Q7 - New Stu Unit" (New Student Unit/Apt)
    public string Q8NewStuAdd { get; set; }         // "Q8 - New Stu Add" (New Student Address)
    public string Q9City { get; set; }              // "Q9 - City:"
    public string Q10Prov { get; set; }             // "Q10 - Prov:" (Province/State)
    public string Q11PC { get; set; }               // "Q11 - PC" (Postal Code)
    public DateTime? Q12NewAddrEffDt { get; set; }  // "Q12 - NE Waddr Eff Dt" (New Address Effective Date, nullable)

    // Parent/Guardian 1 (PG1) Relationship and Legal Info (Q13-Q20)
    public string Q13Relationship { get; set; }     // "Q13 - Relationship:"
    public string Q14PG1FN { get; set; }            // "Q14 - PG1-FN" (Parent/Guardian 1 First Name)
    public string Q15PG1MidNm { get; set; }         // "Q15 - PG1-Mid Nm" (Middle Name)
    public string Q16PG1LN { get; set; }            // "Q16 - PG1-LN" (Last Name)
    public string Q17Custody { get; set; }          // "Q17 - Custody?" (e.g., "Yes", "No")
    public string Q18LegDocName { get; set; }       // "Q18 - Leg Doc Name" (Legal Document Name)
    public string Q19NoLegDoc { get; set; }         // "Q19 - No Leg Doc" (e.g., "Yes", "No")
    public string Q20LivesWith { get; set; }        // "Q20 - liveswith?" (e.g., "PG1", "PG2")

    // PG1 Address Info (Q21-Q28)
    public string Q21PG1Add { get; set; }           // "Q21 - PG1Add" (Address)
    public string Q22PG1NoUpdate { get; set; }      // "Q22 - PG1no Update"
    public string Q23PG1UpdateAddr { get; set; }    // "Q23 - PG1Update Addr" (Updated Address)
    public string Q24NewPG1Unit { get; set; }       // "Q24 - NEWPG1Unit" (New Unit/Apt)
    public string Q25NewPG1Add { get; set; }        // "Q25 - New PG1Add" (New Address)
    public string Q26PG1City { get; set; }          // "Q26 - PG1City"
    public string Q27PG1Prov { get; set; }          // "Q27 - PG1Prov" (Province/State)
    public string Q28PG1PC { get; set; }            // "Q28 - PG1PC" (Postal Code)

    // PG1 Contact Info (Q29-Q47)
    public string Q29PG1Email { get; set; }         // "Q29 - PG1Email"
    public string Q30PG1AccAllEm { get; set; }      // "Q30 - PG1-Acc All Em" (Accept All Emails)
    public string Q31PG1AccLtdEm { get; set; }      // "Q31 - PG1-Acc Ltd Em" (Accept Limited Emails)
    public string Q32Primary { get; set; }          // "Q32 - Primary" (Primary Contact?)
    public string Q33PG1Ph1 { get; set; }           // "Q33 - PG1Ph1" (Phone 1)
    public string Q34Cell { get; set; }             // "Q34 - Cell" (Cell Phone 1)
    public string Q35Work { get; set; }             // "Q35 - Work" (Work Phone 1)
    public string Q36Home { get; set; }             // "Q36 - Home" (Home Phone 1)
    public string Q37PG1Txt1 { get; set; }          // "Q37 - PG1Txt1" (Text Allowed Phone 1?)
    public string Q38PG1Ph2 { get; set; }           // "Q38 - PG1Ph2" (Phone 2)
    public string Q39Cell2 { get; set; }            // "Q39 - Cell" (Cell Phone 2)
    public string Q40Work2 { get; set; }            // "Q40 - Work" (Work Phone 2)
    public string Q41Home2 { get; set; }            // "Q41 - Home" (Home Phone 2)
    public string Q42PG1Txt2 { get; set; }          // "Q42 - PG1Txt2" (Text Allowed Phone 2?)
    public string Q43PG1Ph3 { get; set; }           // "Q43 - PG1Ph3" (Phone 3)
    public string Q44Cell3 { get; set; }            // "Q44 - Cell" (Cell Phone 3)
    public string Q45Work3 { get; set; }            // "Q45 - Work" (Work Phone 3)
    public string Q46Home3 { get; set; }            // "Q46 - Home" (Home Phone 3)
    public string Q47PG1Txt3 { get; set; }          // "Q47 - PG1Txt3" (Text Allowed Phone 3?)

    // Parent/Guardian 2 (PG2) Relationship and Legal Info (Q48-Q55)
    public string Q48RelationshipToStudent { get; set; } // "Q48 - Relationship to student:"
    public string Q49PG2FN { get; set; }            // "Q49 - PG2FN" (First Name)
    public string Q50PG2MN { get; set; }            // "Q50 - PG2MN" (Middle Name)
    public string Q51PG2LN { get; set; }            // "Q51 - PG2LN" (Last Name)
    public string Q52Custody2 { get; set; }         // "Q52 - Custody?"
    public string Q53LegalDocName { get; set; }     // "Q53 - Legal Doc Name"
    public string Q54NoLegalDoc { get; set; }       // "Q54 - No Legal Doc"
    public string Q55PG2LiveWith { get; set; }      // "Q55 - PG2Live With"

    // PG2 Address Info (Q56-Q62)
    public string Q56SameAddress { get; set; }      // "Q56 - Same Address" (Same as PG1?)
    public string Q57DifferentAddress { get; set; } // "Q57 - Different address, enter below"
    public string Q58PG2Apt { get; set; }           // "Q58 - PG2Apt" (Apartment/Unit)
    public string Q59PG2Add { get; set; }           // "Q59 - PG2Add" (Address)
    public string Q60PG2City { get; set; }          // "Q60 - PG2City"
    public string Q61PG2Prov { get; set; }          // "Q61 - PG2Prov" (Province/State)
    public string Q62PG2PC { get; set; }            // "Q62 - PG2PC" (Postal Code)

    // PG2 Contact Info (Q63-Q81)
    public string Q63PG2Email { get; set; }         // "Q63 - PG2Email:"
    public string Q64AcceptAllEmails { get; set; }  // "Q64 - Accept all emails"
    public string Q65PG2AccLtdEm { get; set; }      // "Q65 - PG2-Acc Ltd Em" (Accept Limited Emails)
    public string Q66Primary2 { get; set; }         // "Q66 - Primary" (Primary Contact?)
    public string Q67PG2Ph1 { get; set; }           // "Q67 - PG2Ph1" (Phone 1)
    public string Q68CellPG2 { get; set; }          // "Q68 - Cell" (Cell Phone 1)
    public string Q69WorkPG2 { get; set; }          // "Q69 - Work" (Work Phone 1)
    public string Q70HomePG2 { get; set; }          // "Q70 - Home" (Home Phone 1)
    public string Q71PG2Ph1Txt { get; set; }        // "Q71 - PG2Ph1Txt" (Text Allowed Phone 1?)
    public string Q72PG2Ph2 { get; set; }           // "Q72 - PG2Ph2" (Phone 2)
    public string Q73Cell2PG2 { get; set; }         // "Q73 - Cell" (Cell Phone 2)
    public string Q74Work2PG2 { get; set; }         // "Q74 - Work" (Work Phone 2)
    public string Q75Home2PG2 { get; set; }         // "Q75 - Home" (Home Phone 2)
    public string Q76PG2Ph2Txt { get; set; }        // "Q76 - PG2Ph2Txt" (Text Allowed Phone 2?)
    public string Q77PG2Ph3 { get; set; }           // "Q77 - PG2Ph3" (Phone 3)
    public string Q78Cell3PG2 { get; set; }         // "Q78 - Cell" (Cell Phone 3)
    public string Q79Work3PG2 { get; set; }         // "Q79 - Work" (Work Phone 3)
    public string Q80Home3PG2 { get; set; }         // "Q80 - Home" (Home Phone 3)
    public string Q81PG2Ph3Txt { get; set; }        // "Q81 - PG2Ph3Txt" (Text Allowed Phone 3?)

    // Confirmation
    public string Q82Confirm { get; set; }          // "Q82 - confirm" (Confirmation field)
}