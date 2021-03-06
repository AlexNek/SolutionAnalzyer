namespace SolutionAnalyzer
{
    using System;

    /// <summary>
    /// Helper class that exposes all GUIDs used across VS Package.
    /// </summary>
    internal sealed partial class PackageGuids
    {
        /// <summary>
        /// The unique identifier solution analyzer package string
        /// </summary>
        public const string guidSolutionAnalyzerPackageString = "f11df89d-ef7d-42b8-8130-551e10819c91";
        /// <summary>
        /// The unique identifier solution analyzer package
        /// </summary>
        public static Guid guidSolutionAnalyzerPackage = new Guid(guidSolutionAnalyzerPackageString);

        /// <summary>
        /// The unique identifier solution analyzer package command set string
        /// </summary>
        public const string guidSolutionAnalyzerPackageCmdSetString = "7b55ef16-4bcc-40d0-a6c2-5d34389d752f";
        /// <summary>
        /// The unique identifier solution analyzer package command set
        /// </summary>
        public static Guid guidSolutionAnalyzerPackageCmdSet = new Guid(guidSolutionAnalyzerPackageCmdSetString);

        /// <summary>
        /// The unique identifier images string
        /// </summary>
        public const string guidImagesString = "e53fd8cf-070b-4667-814e-b91dc749eafe";
        /// <summary>
        /// The unique identifier images
        /// </summary>
        public static Guid guidImages = new Guid(guidImagesString);
    }
    /// <summary>
    /// Helper class that encapsulates all CommandIDs uses across VS Package.
    /// </summary>
    internal sealed partial class PackageIds
    {
        /// <summary>
        /// The menu tools group
        /// </summary>
        public const int MenuToolsGroup = 0x1020;
        /// <summary>
        /// The main window command identifier
        /// </summary>
        public const int MainWindowCommandId = 0x0100;
        /// <summary>
        /// The BMP pic1
        /// </summary>
        public const int bmpPic1 = 0x0001;
        /// <summary>
        /// The BMP pic2
        /// </summary>
        public const int bmpPic2 = 0x0002;
        /// <summary>
        /// The BMP pic search
        /// </summary>
        public const int bmpPicSearch = 0x0003;
        /// <summary>
        /// The BMP pic x
        /// </summary>
        public const int bmpPicX = 0x0004;
        /// <summary>
        /// The BMP pic arrows
        /// </summary>
        public const int bmpPicArrows = 0x0005;
        /// <summary>
        /// The BMP pic strikethrough
        /// </summary>
        public const int bmpPicStrikethrough = 0x0006;
    }
}