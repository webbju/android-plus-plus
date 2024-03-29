﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public static class JavaUtilities
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<string> ConvertJavaOutputToVS (IEnumerable<string> lines)
    {
      var ret = new List<string>();

      foreach (var line in lines)
      {
        ConvertJavaOutputToVS(line);
      }

      return ret;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertJavaOutputToVS (string line)
    {
      // 
      // Parse and reformat Java error and warning output into a Visual Studio 'jump to line' style.
      // 

      StringBuilder vsOutputBuilder = new StringBuilder (line);

      string [] patterns = new string []
      {
        // Android\com\google\android\vending\licensing\LicenseChecker.java:257: warning: [deprecation] toGMTString() in Date has been deprecated
        "^(?<sourcefile>.?.?[^:]*.*?):(?<row>[0-9]*): (?<message>.*$)",
      };

      foreach (string pattern in patterns)
      {
        Regex regExMatcher = new Regex (pattern, RegexOptions.Compiled);

        Match regExMatch = regExMatcher.Match (line);

        if (regExMatch.Success)
        {
          string sourcefile = regExMatch.Result ("${sourcefile}");

          string row = regExMatch.Result ("${row}");

          string column = regExMatch.Result ("${column}");

          string message = regExMatch.Result ("${message}");

          vsOutputBuilder.Clear ();

          if (!string.IsNullOrWhiteSpace (sourcefile) && !sourcefile.Equals ("${sourcefile}"))
          {
            vsOutputBuilder.Append (PathUtils.ConvertPathCygwinToWindows (sourcefile));
          }

          if (!string.IsNullOrWhiteSpace (row) && !row.Equals ("${row}"))
          {
            if (string.IsNullOrWhiteSpace (column) && !column.Equals ("${column}"))
            {
              vsOutputBuilder.AppendFormat ("({0},{1})", row, column);
            }
            else
            {
              vsOutputBuilder.AppendFormat ("({0})", row);
            }
          }

          vsOutputBuilder.Append (": ");

          if (!string.IsNullOrWhiteSpace (message))
          {
            vsOutputBuilder.Append (message);
          }

          break;
        }
      }

      vsOutputBuilder.Replace ("error: ", "error : ");

      vsOutputBuilder.Replace ("warning: ", "warning : ");

      return vsOutputBuilder.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
