namespace DiplomaTry2.Components.Tables;

public sealed record GridColumn(string Title, string Field, int WidthGrow = 1, string? DisplayFormat = null, bool Visible = true);

public sealed record PrinterGridRow(int Number, string? Source, string? Name, string? ShareName,
    string? Comment, string? Port, string Model, string Online);

public sealed record EventGridRow(DateTime? Time, string Sender, string Computer, string Printer,
    string Document, int? Pages, long? Size);

public sealed record ModelGridRow(string Name, string Color, string Duplex);
