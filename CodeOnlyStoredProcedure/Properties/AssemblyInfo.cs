using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Code Only Stored Proecedures")]
[assembly: AssemblyDescription("A library for easily calling Stored Procedures in .NET. Works great with Entity Framework Code First models. Code Only Stored Procedures will not create any Stored Procedures on your database. Instead, its aim is to make it easy to call your existing stored procedures by writing simple code.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Abraham Heidebrecht")]
[assembly: AssemblyProduct("CodeOnlyStoredProcedure")]
[assembly: AssemblyCopyright("Copyright © Abraham Heidebrecht 2013 - 2014")]
[assembly: ComVisible(false)]

// The assembly version gets set via TeamCity build. To change the version number, update
// the Version.xml file.

#if NET40
[assembly: InternalsVisibleTo("CodeOnlyTests-NET40")]
#else
[assembly: InternalsVisibleTo("CodeOnlyTests")]
#endif
