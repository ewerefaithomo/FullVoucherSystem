using FullVoucherSystem.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FullVoucherSystem.Documents
{
    public class UBTHVoucherDocument : IDocument
    {
        private readonly BillingCertification _m;
        private readonly string _amountWords;

        public UBTHVoucherDocument(
            BillingCertification model,
            string amountWords)
        {
            _m = model;
            _amountWords = amountWords;
        }

        public DocumentMetadata GetMetadata()
            => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);

                page.Margin(8);

                page.DefaultTextStyle(x =>
                    x.FontSize(8));

                page.Content()
                    .Column(col =>
                    {
                        Header(col);

                        VoucherSection(col);

                        PayeeSection(col);

                        ServiceSection(col);

                        DeductionSection(col);

                        AmountWordsSection(col);

                        SignatureSection(col);

                        CertificateSection(col);

                        ReceiptSection(col);

                        AdvanceSection(col);
                        
                        Header(col);

                        VoucherSection(col);

                        CodingSection(col);

                        HeadSubHeadBoxes(col);

                        DateBoxes(col);

                        AmountBoxes(col);

                        PayeeSection(col);

                        ServiceSection(col);

                        DeductionSection(col);

                        AmountWordsSection(col);

                        SignatureSection(col);

                        CertificateSection(col);

                        ReceiptSection(col);

                        AdvanceSection(col);
                    });
            });
        }

        private void Header(ColumnDescriptor col)
        {
            col.Item()
                .AlignCenter()
                .Text("UNIVERSITY OF BENIN TEACHING HOSPITAL")
                .Bold()
                .FontSize(12);

            col.Item()
                .AlignCenter()
                .Text("PAYMENT VOUCHER")
                .Bold()
                .FontSize(11);
        }

        private void VoucherSection(ColumnDescriptor col)
        {
            col.Item().Border(1).Padding(3).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                });

                table.Cell()
                    .Padding(2)
                    .Text($"VOUCHER NO: {_m.VoucherNumber}")
                    .Bold();

                table.Cell()
                    .Padding(2)
                    .Text($"Checked and Passed for Payment at: {_m.CheckedLocation}");
            });

            col.Item().Border(1).Padding(2).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Text($"Station: {_m.Station}");
                table.Cell().Text($"Head: {_m.Head}");
                table.Cell().Text($"S/Head: {_m.SubHead}");
                table.Cell().Text($"Date: {_m.Date:dd-MMM-yyyy}");
            });
        }

        private void CodingSection(ColumnDescriptor col)
        {
            col.Item().Border(1).Padding(3).Column(column =>
            {
                column.Item().Text("Classification Code")
                    .Bold();

                column.Item().Element(x =>
                    DigitBoxes(
                        x,
                        _m.ClassificationCode?.Replace(".", ""),
                        12));

                column.Item().PaddingTop(3);

                column.Item().Text("Source Classification Code")
                    .Bold();

                column.Item().Element(x =>
                    DigitBoxes(
                        x,
                        _m.SourceClassificationCode?.Replace(".", ""),
                        15));
            });
        }

        private void HeadSubHeadBoxes(
    ColumnDescriptor col)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("HEAD").Bold();

                    c.Item().Element(x =>
                        DigitBoxes(
                            x,
                            _m.Head,
                            4));
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("S/HEAD").Bold();

                    c.Item().Element(x =>
                        DigitBoxes(
                            x,
                            _m.SubHead,
                            6));
                });
            });
        }

        private void DateBoxes(
    ColumnDescriptor col)
        {
            string dateCode =
                _m.Date.ToString("ddMMyy");

            col.Item().Column(c =>
            {
                c.Item().Text("Date");

                c.Item().Element(x =>
                    DigitBoxes(
                        x,
                        dateCode,
                        6));
            });
        }

        private void AmountBoxes(
    ColumnDescriptor col)
        {
            string amount =
                ((int)_m.Amount)
                .ToString();

            col.Item().Column(c =>
            {
                c.Item().Text("Amount");

                c.Item().Element(x =>
                    DigitBoxes(
                        x,
                        amount,
                        10));
            });
        }


        private void SignatureSection1(ColumnDescriptor col)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                table.Cell()
                    .Border(1)
                    .Height(90)
                    .Padding(5)
                    .Text(text =>
                    {
                        text.Line($"Payable at: {_m.CheckedLocation}");
                        text.Line("");
                        text.Line("Signature: __________________");
                        text.Line("");
                        text.Line($"Name in Block Letters:");
                        text.Line($"{_m.Voucher_FirstName} {_m.Voucher_Surname}");
                    });

                table.Cell()
                    .Border(1)
                    .Height(90)
                    .Padding(5)
                    .Text(text =>
                    {
                        text.Line($"Checked By:");
                        text.Line($"{_m.Bill_FirstName} {_m.Bill_Surname}");
                        text.Line("");
                        text.Line("Signature: __________________");
                    });
            });
        }


        private void CertificateSection1(
    ColumnDescriptor col)
        {
            col.Item()
                .AlignCenter()
                .Text("CERTIFICATE")
                .Bold()
                .FontSize(10);

            col.Item()
                .Border(1)
                .Height(95)
                .Padding(6)
                .Text(text =>
                {
                    text.Span(
                        "I Certify the above amount is correct, and was incurred under the authority quoted, that the service has been duly performed; that the rate/price charged is according to regulations/contract and is fair and reasonable; that the amount of ");

                    text.Span(
                        $"{_m.TotalAmount:N2}")
                        .Bold();

                    text.Span(
                        " Naira/Kobo may be paid under the Classification quoted.");
                });
        }


        private void PayeeSection(ColumnDescriptor col)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(80);
                    c.RelativeColumn();
                });

                table.Cell().Border(1).Padding(3)
                    .Text("Payee:");

                table.Cell().Border(1).Padding(3)
                    .Text(_m.Payee);

                table.Cell().Border(1).Padding(3)
                    .Text("Address:");

                table.Cell().Border(1).Padding(3)
                    .Text(_m.Address);

                table.Cell().Border(1).Padding(3)
                    .Text("Payment For:");

                table.Cell().Border(1).Padding(3)
                    .Text(_m.Description);
            });
        }

        private void DigitBoxes(
    IContainer container,
    string value,
    int boxCount)
        {
            value = value ?? "";

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (int i = 0; i < boxCount; i++)
                        columns.ConstantColumn(14);
                });

                for (int i = 0; i < boxCount; i++)
                {
                    string digit =
                        i < value.Length
                        ? value[i].ToString()
                        : "";

                    table.Cell()
                        .Border(1)
                        .Height(14)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(digit)
                        .FontSize(8);
                }
            });
        }
        private void ServiceSection(ColumnDescriptor col)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(90);
                    c.RelativeColumn();
                    c.ConstantColumn(120);
                });

                table.Header(h =>
                {
                    h.Cell().Border(1)
                        .Padding(3)
                        .Text("Date")
                        .Bold();

                    h.Cell().Border(1)
                        .Padding(3)
                        .Text("Detailed Description of Service / Work")
                        .Bold();

                    h.Cell().Border(1)
                        .Padding(3)
                        .AlignRight()
                        .Text("RATE N:K")
                        .Bold();
                });

                table.Cell()
                    .Border(1)
                    .Height(70)
                    .Padding(3)
                    .Text(_m.Date.ToString("dd-MMM-yyyy"));

                table.Cell()
                    .Border(1)
                    .Height(70)
                    .Padding(3)
                    .Text(_m.Description);

                table.Cell()
                    .Border(1)
                    .Height(70)
                    .Padding(3)
                    .AlignRight()
                    .Text(_m.Amount.ToString("N2"));
            });

            col.Item().PaddingTop(5)
                .Text("I _____________________");

            col.Item()
                .Text("Certify on honour that the above amount is for the interest of the public service.");
        }

        private void DeductionSection(ColumnDescriptor col)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(120);
                });

                AddRow(table, "CONTRACT SUM", _m.Amount);
                AddRow(table, "LESS % FGN WHT", _m.FGN_WHT);
                AddRow(table, "LESS % EDG WHT", _m.EDG_WHT);
                AddRow(table, "LESS % VAT", 0);
                AddRow(table, "LESS % STAMP DUTY", _m.StampDuty);
                AddRow(table, "TOTAL", _m.TotalAmount, true);
            });
        }

        private void AddRow(
            TableDescriptor table,
            string label,
            decimal value,
            bool bold = false)
        {
            table.Cell()
                .Border(1)
                .Padding(3)
                .Text(label);

            table.Cell()
                .Border(1)
                .Padding(3)
                .AlignRight()
                .Text(value.ToString("N2"));
        }

        private void AmountWordsSection(ColumnDescriptor col)
        {
            col.Item()
                .Border(1)
                .Padding(5)
                .Text(text =>
                {
                    text.Line("Checked and Passed for Payment");
                    text.Line("(Insert Amount in Words)");
                    text.Line("");
                    text.Line(_amountWords);
                });
        }

        private void SignatureSection(ColumnDescriptor col)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                table.Cell()
                    .Border(1)
                    .Padding(5)
                    .Text($@"Payable at: {_m.CheckedLocation}

Signature: ______________________

Name in Block Letters:
{_m.Voucher_FirstName} {_m.Voucher_Surname}");

                table.Cell()
                    .Border(1)
                    .Padding(5)
                    .Text($@"Checked By:
{_m.Bill_FirstName} {_m.Bill_Surname}

Signature: ______________________");
            });
        }

        private void CertificateSection(ColumnDescriptor col)
        {
            col.Item()
                .AlignCenter()
                .Text("CERTIFICATE")
                .Bold();

            col.Item()
                .Border(1)
                .Padding(5)
                .Text(
                $"I Certify the above amount is correct, and was incurred under the authority quoted, that the service has been duly performed; that the rate/price charged is according to regulations/contract and is fair and reasonable; that the amount of {_m.TotalAmount:N2} Naira/Kobo may be paid under the Classification quoted.");

            col.Item()
                .Border(1)
                .Padding(5)
                .Text($@"Signature of Officer Contr. Expenditure:

Date: {_m.Date:dd-MMM-yyyy}

Place: {_m.Station}

Designation: {_m.Bill_Designation}");
        }

        private void ReceiptSection(ColumnDescriptor col)
        {
            col.Item()
                .Border(1)
                .Padding(5)
                .Text($@"Received from UBTH the Sum of: {_m.TotalAmount:N2}

In full settlement of the Account.

Place: {_m.Station}

Signature: ______________________");
        }

        private void AdvanceSection(ColumnDescriptor col)
        {
            col.Item()
                .Border(1)
                .Padding(5)
                .Text(@"FOR USE IN PAYMENT OF ADVANCES

Certified the Advance of _____________________

Has been entered on 124(A)(B)

Date: _____________

Voucher No: _____________

Signature: _____________

Name in Block Letters:
______________________");
        }
    }
}