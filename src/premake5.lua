
solution "AndroidPlusPlus"

  configurations { "Debug", "Release" }
  
  location "../projects/vs2010"
  
  include "AndroidPlusPlus.Common"
  
  include "AndroidPlusPlus.MsBuild"
  
  include "AndroidPlusPlus.MsBuild.Exporter"
  
  include "AndroidPlusPlus.VsDebugEngine"
  
  include "AndroidPlusPlus.VsDebugLauncherX"
  
  --include "AndroidPlusPlus.VsDebugLauncherXI"
  
  --include "AndroidPlusPlus.VsDebugLauncherXII"
  
  include "AndroidPlusPlus.VsIntegratedPackage"
  
  --include "AndroidPlusPlus.VsIsolatedShell"
  
  --startproject "../AndroidPlusPlus.VsIntegratedPackage"
  
  