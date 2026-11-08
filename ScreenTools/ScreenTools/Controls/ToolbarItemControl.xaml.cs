using System.Windows;
using System.Windows.Controls;

namespace ScreenTools.Controls;

/// <summary>
/// Provides a reusable toolbar item with an icon, label, and active state.
/// </summary>
public partial class ToolbarItemControl : UserControl
{
    /// <summary>
    /// Provides the LabelProperty value exposed by this component.
    /// </summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(ToolbarItemControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Provides the IconContentProperty value exposed by this component.
    /// </summary>
    public static readonly DependencyProperty IconContentProperty =
        DependencyProperty.Register(
            nameof(IconContent),
            typeof(object),
            typeof(ToolbarItemControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Provides the IsActiveProperty value exposed by this component.
    /// </summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(ToolbarItemControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Provides the ItemWidthProperty value exposed by this component.
    /// </summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(ToolbarItemControl),
            new PropertyMetadata(44d));

    /// <summary>
    /// Provides the ItemPaddingProperty value exposed by this component.
    /// </summary>
    public static readonly DependencyProperty ItemPaddingProperty =
        DependencyProperty.Register(
            nameof(ItemPadding),
            typeof(Thickness),
            typeof(ToolbarItemControl),
            new PropertyMetadata(new Thickness(0)));

    /// <summary>
    /// Initializes the reusable toolbar-item control and its dependency-property bindings.
    /// </summary>
    public ToolbarItemControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the reusable toolbar-item control and its dependency-property bindings.
    /// </summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Initializes the reusable toolbar-item control and its dependency-property bindings.
    /// </summary>
    public object? IconContent
    {
        get => GetValue(IconContentProperty);
        set => SetValue(IconContentProperty, value);
    }

    /// <summary>
    /// Initializes the reusable toolbar-item control and its dependency-property bindings.
    /// </summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// Initializes the reusable toolbar-item control and its dependency-property bindings.
    /// </summary>
    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>
    /// Initializes the reusable toolbar-item control and its dependency-property bindings.
    /// </summary>
    public Thickness ItemPadding
    {
        get => (Thickness)GetValue(ItemPaddingProperty);
        set => SetValue(ItemPaddingProperty, value);
    }
}
