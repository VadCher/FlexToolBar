namespace FlexToolBar.Core
{
    /// <summary>
    /// Specifies the visual layout size modes for controls inside a Ribbon/FlexGroup context.
    /// </summary>
    public enum RibbonControlSize
    {
        /// <summary>
        /// Large mode: Displayed as a large icon stacked vertically above the text label.
        /// </summary>
        Large,

        /// <summary>
        /// Medium mode: Displayed as a small icon placed horizontally next to the text label.
        /// </summary>
        Medium,

        /// <summary>
        /// Small mode: Displayed strictly as a small icon, hiding the text label into a ToolTip.
        /// </summary>
        Small
    }
}
