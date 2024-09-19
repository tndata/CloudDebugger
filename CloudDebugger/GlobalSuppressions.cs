// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't care about this in this application")]
[assembly: SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "We don't care about this in this application")]
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "No need for ConfigureAwait in ASP.NET Core application code, as there is no SynchronizationContext.")]
[assembly: SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Not needed in this application")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "We catch these errors using no-nullable reference types")]
[assembly: SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "We don't care about this in this application")]
[assembly: SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "We don't care about this in this application")]
[assembly: SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "We don't care about this in this application")]
