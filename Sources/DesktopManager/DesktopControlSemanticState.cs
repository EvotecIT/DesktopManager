using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>Represents selection-container and selection-item state.</summary>
public sealed class DesktopControlSelectionObservation {
    /// <summary>Gets or sets the selected item labels or values.</summary>
    public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets whether multiple items can be selected.</summary>
    public bool? CanSelectMultiple { get; set; }
    /// <summary>Gets or sets whether the provider requires one selected item.</summary>
    public bool? IsSelectionRequired { get; set; }
    /// <summary>Gets or sets whether this control is a selected item.</summary>
    public bool? IsSelected { get; set; }
    /// <summary>Gets or sets whether selected-item labels exceeded the configured item or character bound.</summary>
    public bool IsTruncated { get; set; }
}

/// <summary>Represents numeric range state.</summary>
public sealed class DesktopControlRangeObservation {
    /// <summary>Gets or sets the current value.</summary>
    public double? Value { get; set; }
    /// <summary>Gets or sets the minimum value.</summary>
    public double? Minimum { get; set; }
    /// <summary>Gets or sets the maximum value.</summary>
    public double? Maximum { get; set; }
    /// <summary>Gets or sets the small-change increment.</summary>
    public double? SmallChange { get; set; }
    /// <summary>Gets or sets the large-change increment.</summary>
    public double? LargeChange { get; set; }
    /// <summary>Gets or sets whether the range is read-only.</summary>
    public bool? IsReadOnly { get; set; }
}

/// <summary>Represents scroll-container state.</summary>
public sealed class DesktopControlScrollObservation {
    /// <summary>Gets or sets whether horizontal scrolling is available.</summary>
    public bool? HorizontallyScrollable { get; set; }
    /// <summary>Gets or sets whether vertical scrolling is available.</summary>
    public bool? VerticallyScrollable { get; set; }
    /// <summary>Gets or sets the horizontal scroll percentage.</summary>
    public double? HorizontalPercent { get; set; }
    /// <summary>Gets or sets the vertical scroll percentage.</summary>
    public double? VerticalPercent { get; set; }
    /// <summary>Gets or sets the horizontal viewport percentage.</summary>
    public double? HorizontalViewSize { get; set; }
    /// <summary>Gets or sets the vertical viewport percentage.</summary>
    public double? VerticalViewSize { get; set; }
}

/// <summary>Represents grid, table, and grid-item state.</summary>
public sealed class DesktopControlGridObservation {
    /// <summary>Gets or sets the row count for a grid container.</summary>
    public int? RowCount { get; set; }
    /// <summary>Gets or sets the column count for a grid container.</summary>
    public int? ColumnCount { get; set; }
    /// <summary>Gets or sets the row coordinate for a grid item.</summary>
    public int? Row { get; set; }
    /// <summary>Gets or sets the column coordinate for a grid item.</summary>
    public int? Column { get; set; }
    /// <summary>Gets or sets the row span for a grid item.</summary>
    public int? RowSpan { get; set; }
    /// <summary>Gets or sets the column span for a grid item.</summary>
    public int? ColumnSpan { get; set; }
    /// <summary>Gets or sets the row or column ordering reported by a table.</summary>
    public string RowOrColumnMajor { get; set; } = string.Empty;
    /// <summary>Gets or sets the row header labels.</summary>
    public IReadOnlyList<string> RowHeaders { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets the column header labels.</summary>
    public IReadOnlyList<string> ColumnHeaders { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets whether table header labels exceeded the configured item or character bound.</summary>
    public bool IsTruncated { get; set; }
}
