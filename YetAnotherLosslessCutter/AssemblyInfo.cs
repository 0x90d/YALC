using System.Reflection;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Yet Another Lossless Cutter")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("YALC")]
[assembly: AssemblyCopyright("Copyright © 2023")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]


[assembly: AssemblyVersion(YALCConstants.ASSEMBLY_VERSION)]
[assembly: AssemblyFileVersion(YALCConstants.ASSEMBLY_FILE_VERSION)]
[assembly: AssemblyInformationalVersion(YALCConstants.ASSEMBLY_INFORMATIONAL_VERSION)]



static class YALCConstants
{
    public const string ASSEMBLY_VERSION = "1.0.5.0";
    public const string ASSEMBLY_INFORMATIONAL_VERSION = "v1.0.5";
    public const string ASSEMBLY_FILE_VERSION = ASSEMBLY_VERSION;
}