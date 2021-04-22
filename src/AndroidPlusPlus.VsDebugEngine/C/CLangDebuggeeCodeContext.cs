////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.VisualStudio.Debugger.Interop;
using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class CLangDebuggeeCodeContext : DebuggeeCodeContext
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected readonly CLangDebugger m_debugger;

    protected string m_symbolName;

    protected string m_symbolOffset;

    protected string m_symbolModule;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeCodeContext (CLangDebugger debugger, DebuggeeAddress address, DebuggeeDocumentContext documentContext)
      : base (debugger.Engine, documentContext, address)
    {
      m_debugger = debugger;

      try
      {
        string command = string.Format ("-interpreter-exec console \"info symbol {0}\"", m_address.ToString ());

        MiResultRecord resultRecord = m_debugger.GdbClient.SendSyncCommand (command);

        MiResultRecord.RequireOk (resultRecord, command);

        string pattern = "(?<symbol>.+)( [\\+] (?<offset>[0-9]+))? (in section (?<section>[^ ]+) of) (?<module>.+)";

        Regex regExMatcher = new Regex (pattern, RegexOptions.IgnoreCase);

        foreach (MiStreamRecord record in resultRecord.Records)
        {
          if (!record.Stream.StartsWith ("No symbol"))
          {
            continue; // early rejection.
          }

          StringBuilder sanitisedStream = new StringBuilder (record.Stream);

          sanitisedStream.Length -= 2; // Strip trailing "\\n"

          Match regExLineMatch = regExMatcher.Match (sanitisedStream.ToString ());

          if (regExLineMatch.Success)
          {
            string symbol = regExLineMatch.Result ("${symbol}");

            string offset = regExLineMatch.Result ("${offset}");

            string section = regExLineMatch.Result ("${section}");

            string module = regExLineMatch.Result ("${module}");

            ulong addressOffset = 0ul;

            ulong.TryParse (offset, out addressOffset);

            m_symbolName = symbol;

            m_symbolOffset = addressOffset.ToString ();

            //string moduleFile = Path.GetFileName (module);

            //CLangDebuggeeModule module = m_debugger.NativeProgram.GetModule (moduleFile);

            //MODULE_INFO [] moduleArray = new MODULE_INFO [1];

            //LoggingUtils.RequireOk (module.GetInfo (enum_MODULE_INFO_FIELDS.MIF_URL, moduleArray));

            //infoArray [0].bstrModuleUrl = moduleArray [0].m_bstrUrl;

            m_symbolModule = PathUtils.ConvertPathMingwToWindows (module);
          }
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override int SetInfo (enum_CONTEXT_INFO_FIELDS requestedFields, CONTEXT_INFO [] infoArray)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        LoggingUtils.RequireOk (base.SetInfo (requestedFields, infoArray));

        if ((requestedFields & enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL) != 0)
        {
          infoArray [0].bstrModuleUrl = m_symbolModule;

          infoArray [0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL;
        }

        if ((requestedFields & enum_CONTEXT_INFO_FIELDS.CIF_FUNCTION) != 0)
        {
          infoArray [0].bstrFunction = m_symbolName;

          infoArray [0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_FUNCTION;
        }

        if ((requestedFields & enum_CONTEXT_INFO_FIELDS.CIF_ADDRESSOFFSET) != 0)
        {
          infoArray [0].bstrAddressOffset = m_symbolOffset;

          infoArray [0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_ADDRESSOFFSET;
        }

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static CLangDebuggeeCodeContext GetCodeContextForLocation (CLangDebugger debugger, string location)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (string.IsNullOrEmpty (location))
        {
          throw new ArgumentNullException (nameof(location));
        }

        if (location.StartsWith ("0x"))
        {
          location = "*" + location;
        }
        else if (location.StartsWith ("\""))
        {
          location = location.Replace ("\\", "/");

          location = location.Replace ("\"", "\\\""); // required to escape the nested string.
        }

        string command = string.Format ("-interpreter-exec console \"info line {0}\"", location);

        MiResultRecord resultRecord = debugger.GdbClient.SendSyncCommand (command);

        MiResultRecord.RequireOk (resultRecord, command);

        string pattern = "Line (?<line>[0-9]+) of ([\\]*\"(?<file>.+)[\\]*[\"]+) starts at address (?<startaddr>[^ ]+) (?<startsym>[^+]+[+]?[0-9]*[>]?) (but contains no code|and ends at (?<endaddr>[^ ]+) (?<endsym>[^+]+[+]?[0-9]*[>]?)?)";

        pattern = pattern.Replace ("\\", "\\\\");

        Regex regExMatcher = new Regex (pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        foreach (MiStreamRecord record in resultRecord.Records)
        {
          if (!record.Stream.StartsWith ("Line"))
          {
            continue; // early rejection.
          }

          StringBuilder sanitisedStream = new StringBuilder (record.Stream);

          sanitisedStream.Replace ("\\\"", "\"");

          sanitisedStream.Replace ("\\\\", "\\");

          Match regExLineMatch = regExMatcher.Match (sanitisedStream.ToString ());

          if (regExLineMatch.Success)
          {
            string line = regExLineMatch.Result ("${line}");

            string file = regExLineMatch.Result ("${file}");

            string startaddr = regExLineMatch.Result ("${startaddr}");

            string startsym = regExLineMatch.Result ("${startsym}");

            string endaddr = regExLineMatch.Result ("${endaddr}");

            string endsym = regExLineMatch.Result ("${endsym}");

            TEXT_POSITION [] documentPositions = new TEXT_POSITION [2];

            documentPositions [0].dwLine = uint.Parse (line) - 1;

            documentPositions [0].dwColumn = 0;

            documentPositions [1].dwLine = documentPositions [0].dwLine;

            documentPositions [1].dwColumn = uint.MaxValue;

            DebuggeeAddress startAddress = new DebuggeeAddress (startaddr);

            DebuggeeDocumentContext documentContext = new DebuggeeDocumentContext (debugger.Engine, file, documentPositions [0], documentPositions [1]);

            CLangDebuggeeCodeContext codeContext = new CLangDebuggeeCodeContext (debugger, startAddress, documentContext);

            documentContext.SetCodeContext (codeContext);

            return codeContext;
          }
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }

      return null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static CLangDebuggeeCodeContext GetCodeContextForDocumentContext (CLangDebugger debugger, DebuggeeDocumentContext documentContext)
    {
      LoggingUtils.PrintFunction ();

      string fileName;

      TEXT_POSITION [] startOffset = new TEXT_POSITION [1];

      TEXT_POSITION [] endOffset = new TEXT_POSITION [1];

      LoggingUtils.RequireOk (documentContext.GetName (enum_GETNAME_TYPE.GN_FILENAME, out fileName));

      LoggingUtils.RequireOk (documentContext.GetStatementRange (startOffset, endOffset));

      string location = string.Format ("\"{0}:{1}\"", fileName, startOffset [0].dwLine + 1);

      return GetCodeContextForLocation (debugger, location);
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
