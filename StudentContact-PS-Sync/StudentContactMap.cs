using CsvHelper.Configuration;

public sealed class StudentContactMap : ClassMap<StudentContact>
{
    public StudentContactMap()
    {
        // Student-related fields
        Map(m => m.Campus).Name("Campus");
        Map(m => m.Status).Name("Status");
        Map(m => m.DateModified).Name("Date Modified");
        Map(m => m.DateCompleted).Name("Date Completed");
        Map(m => m.StudentId).Name("Student ID");
        Map(m => m.LastName).Name("Last Name");
        Map(m => m.FirstName).Name("First Name");
        Map(m => m.Grade).Name("Grade");
        Map(m => m.HR).Name("HR");

        // Student additional info (Q1-Q12)
        Map(m => m.Q1StuFN).Name("Q1 - Stu FN");
        Map(m => m.Q2StuMN).Name("Q2 - Stu MN");
        Map(m => m.Q3StuLN).Name("Q3 - Stu LN");
        Map(m => m.Q4StuCurrAdd).Name("Q4 - Stu Curr Add");
        Map(m => m.Q5NoUpdate).Name("Q5 - No Update");
        Map(m => m.Q6UpdateAdd).Name("Q6 - Update Add");
        Map(m => m.Q7NewStuUnit).Name("Q7 - New Stu Unit");
        Map(m => m.Q8NewStuAdd).Name("Q8 - New Stu Add");
        Map(m => m.Q9City).Name("Q9 - City:");
        Map(m => m.Q10Prov).Name("Q10 - Prov:");
        Map(m => m.Q11PC).Name("Q11 - PC");
        Map(m => m.Q12NewAddrEffDt).Name("Q12 - NE Waddr Eff Dt");

        // PG1 Relationship and Legal Info (Q13-Q20)
        Map(m => m.Q13Relationship).Name("Q13 - Relationship:");
        Map(m => m.Q14PG1FN).Name("Q14 - PG1-FN");
        Map(m => m.Q15PG1MidNm).Name("Q15 - PG1-Mid Nm");
        Map(m => m.Q16PG1LN).Name("Q16 - PG1-LN");
        Map(m => m.Q17Custody).Name("Q17 - Custody?");
        Map(m => m.Q18LegDocName).Name("Q18 - Leg Doc Name");
        Map(m => m.Q19NoLegDoc).Name("Q19 - No Leg Doc");
        Map(m => m.Q20LivesWith).Name("Q20 - liveswith?");

        // PG1 Address Info (Q21-Q28)
        Map(m => m.Q21PG1Add).Name("Q21 - PG1Add");
        Map(m => m.Q22PG1NoUpdate).Name("Q22 - PG1no Update");
        Map(m => m.Q23PG1UpdateAddr).Name("Q23 - PG1Update Addr");
        Map(m => m.Q24NewPG1Unit).Name("Q24 - NEWPG1Unit");
        Map(m => m.Q25NewPG1Add).Name("Q25 - New PG1Add");
        Map(m => m.Q26PG1City).Name("Q26 - PG1City");
        Map(m => m.Q27PG1Prov).Name("Q27 - PG1Prov");
        Map(m => m.Q28PG1PC).Name("Q28 - PG1PC");

        // PG1 Contact Info (Q29-Q47)
        Map(m => m.Q29PG1Email).Name("Q29 - PG1Email");
        Map(m => m.Q30PG1AccAllEm).Name("Q30 - PG1-Acc All Em");
        Map(m => m.Q31PG1AccLtdEm).Name("Q31 - PG1-Acc Ltd Em");
        Map(m => m.Q32Primary).Name("Q32 - Primary");
        Map(m => m.Q33PG1Ph1).Name("Q33 - PG1Ph1");
        Map(m => m.Q34Cell).Name("Q34 - Cell");
        Map(m => m.Q35Work).Name("Q35 - Work");
        Map(m => m.Q36Home).Name("Q36 - Home");
        Map(m => m.Q37PG1Txt1).Name("Q37 - PG1Txt1");
        Map(m => m.Q38PG1Ph2).Name("Q38 - PG1Ph2");
        Map(m => m.Q39Cell2).Name("Q39 - Cell");
        Map(m => m.Q40Work2).Name("Q40 - Work");
        Map(m => m.Q41Home2).Name("Q41 - Home");
        Map(m => m.Q42PG1Txt2).Name("Q42 - PG1Txt2");
        Map(m => m.Q43PG1Ph3).Name("Q43 - PG1Ph3");
        Map(m => m.Q44Cell3).Name("Q44 - Cell");
        Map(m => m.Q45Work3).Name("Q45 - Work");
        Map(m => m.Q46Home3).Name("Q46 - Home");
        Map(m => m.Q47PG1Txt3).Name("Q47 - PG1Txt3");

        // PG2 Relationship and Legal Info (Q48-Q55)
        Map(m => m.Q48RelationshipToStudent).Name("Q48 - Relationship to student:");
        Map(m => m.Q49PG2FN).Name("Q49 - PG2FN");
        Map(m => m.Q50PG2MN).Name("Q50 - PG2MN");
        Map(m => m.Q51PG2LN).Name("Q51 - PG2LN");
        Map(m => m.Q52Custody2).Name("Q52 - Custody?");
        Map(m => m.Q53LegalDocName).Name("Q53 - Legal Doc Name");
        Map(m => m.Q54NoLegalDoc).Name("Q54 - No Legal Doc");
        Map(m => m.Q55PG2LiveWith).Name("Q55 - PG2Live With");

        // PG2 Address Info (Q56-Q62)
        Map(m => m.Q56SameAddress).Name("Q56 - Same Address");
        Map(m => m.Q57DifferentAddress).Name("Q57 - Different address, enter below");
        Map(m => m.Q58PG2Apt).Name("Q58 - PG2Apt");
        Map(m => m.Q59PG2Add).Name("Q59 - PG2Add");
        Map(m => m.Q60PG2City).Name("Q60 - PG2City");
        Map(m => m.Q61PG2Prov).Name("Q61 - PG2Prov");
        Map(m => m.Q62PG2PC).Name("Q62 - PG2PC");

        // PG2 Contact Info (Q63-Q81)
        Map(m => m.Q63PG2Email).Name("Q63 - PG2Email:");
        Map(m => m.Q64AcceptAllEmails).Name("Q64 - Accept all emails");
        Map(m => m.Q65PG2AccLtdEm).Name("Q65 - PG2-Acc Ltd Em");
        Map(m => m.Q66Primary2).Name("Q66 - Primary");
        Map(m => m.Q67PG2Ph1).Name("Q67 - PG2Ph1");
        Map(m => m.Q68CellPG2).Name("Q68 - Cell");
        Map(m => m.Q69WorkPG2).Name("Q69 - Work");
        Map(m => m.Q70HomePG2).Name("Q70 - Home");
        Map(m => m.Q71PG2Ph1Txt).Name("Q71 - PG2Ph1Txt");
        Map(m => m.Q72PG2Ph2).Name("Q72 - PG2Ph2");
        Map(m => m.Q73Cell2PG2).Name("Q73 - Cell");
        Map(m => m.Q74Work2PG2).Name("Q74 - Work");
        Map(m => m.Q75Home2PG2).Name("Q75 - Home");
        Map(m => m.Q76PG2Ph2Txt).Name("Q76 - PG2Ph2Txt");
        Map(m => m.Q77PG2Ph3).Name("Q77 - PG2Ph3");
        Map(m => m.Q78Cell3PG2).Name("Q78 - Cell");
        Map(m => m.Q79Work3PG2).Name("Q79 - Work");
        Map(m => m.Q80Home3PG2).Name("Q80 - Home");
        Map(m => m.Q81PG2Ph3Txt).Name("Q81 - PG2Ph3Txt");

        // Confirmation
        Map(m => m.Q82Confirm).Name("Q82 - confirm");
    }
}