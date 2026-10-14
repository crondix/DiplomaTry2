using DevExpress.Blazor;
using System.Globalization;

namespace DiplomaTry2.Components.Tables;

public static class GridLayoutPolicy
{
    // Retain widths/order/sort while dropping filters whose editors are no longer visible.
    public static GridPersistentLayout Migrate(GridPersistentLayout source, IReadOnlyList<GridColumn> columns)
    {
        var visible = source.Columns.Where(column => column.Visible != false && columns.Any(definition => definition.Field == column.FieldName && definition.Visible)).ToArray();
        var total = visible.Sum(column => Weight(column.Width));
        var migrated = new List<GridPersistentLayoutColumn>();
        foreach (var column in source.Columns)
        {
            var isVisible = column.Visible != false && columns.Any(definition => definition.Field == column.FieldName && definition.Visible);
            migrated.Add(new GridPersistentLayoutColumn
            {
                ColumnType = column.ColumnType, FieldName = column.FieldName, VisibleIndex = column.VisibleIndex,
                Visible = isVisible, GroupIndex = -1, SortIndex = column.SortIndex, SortOrder = column.SortOrder,
                Width = isVisible && total > 0 ? (100 * Weight(column.Width) / total).ToString("0.######", CultureInfo.InvariantCulture) + "%" : column.Width
            });
        }
        return new GridPersistentLayout
        {
            PageIndex = string.IsNullOrEmpty(source.SearchText) && string.IsNullOrEmpty(source.FilterCriteria) ? source.PageIndex : 0,
            PageSize = source.PageSize,
            ShowAllRows = source.ShowAllRows,
            SearchText = "",
            FilterCriteria = null,
            Columns = new GridPersistentLayoutCollection<GridPersistentLayoutColumn>(migrated)
        };
    }

    private static double Weight(string? width) =>
        double.TryParse(width?.TrimEnd('%').Replace("px", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && double.IsFinite(value) && value > 0 ? value : 1;
}
