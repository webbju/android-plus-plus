// Guids.cs
// MUST match guids.h
using System;

namespace AndroidPlusPlus.VsIsolatedShell.AboutBoxPackage
{
  static class GuidList
  {
    public const string guidAboutBoxPackagePkgString = "f91aac9b-7f0c-4679-b008-2f041167675f";
    public const string guidAboutBoxPackageCmdSetString = "27ae8c60-94ab-434e-9724-570e5fafef06";

    public static readonly Guid guidAboutBoxPackageCmdSet = new Guid (guidAboutBoxPackageCmdSetString);
  };
}