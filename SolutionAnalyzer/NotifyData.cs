namespace SolutionAnalyzer
{
    /// <summary>
    /// Class NotifyData.
    /// Inform about solution state changes 
    /// </summary>
    internal class NotifyData
    {
        /// <summary>
        /// Enum ENotifyType
        /// </summary>
        public enum ENotifyType
        {
            /// <summary>
            /// The none
            /// </summary>
            None,

            /// <summary>
            /// The solution opened
            /// </summary>
            SolutionOpened,

            /// <summary>
            /// The solution closed
            /// </summary>
            SolutionClosed,
        }

        /// <summary>
        /// Gets or sets the type of the notify.
        /// </summary>
        /// <value>The type of the notify.</value>
        public ENotifyType NotifyType { get; set; }
    }
}
