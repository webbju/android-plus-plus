////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.Debugger.Interop;
using AndroidPlusPlus.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class CLangDebuggeeStackFrame : DebuggeeStackFrame
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly CLangDebugger m_debugger;

    private uint m_stackLevel;

    private DebuggeeAddress m_locationAddress;

    private string m_locationFunction;

    private string m_locationModule;

    private bool m_locationIsSymbolicated;

    private bool m_queriedRegisters;

    private bool m_queriedArgumentsAndLocals;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeStackFrame (CLangDebugger debugger, CLangDebuggeeThread thread, MiResultValueTuple frameTuple, string frameName)
      : base (debugger.Engine, thread as DebuggeeThread, frameName)
    {
      m_debugger = debugger;

      if (frameTuple == null)
      {
        throw new ArgumentNullException ("frameTuple");
      }

      m_queriedRegisters = false;

      m_queriedArgumentsAndLocals = false;

      GetInfoFromCurrentLevel (frameTuple);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeAddress Address
    {
      get
      {
        return m_locationAddress;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public uint StackLevel 
    {
      get
      {
        return m_stackLevel;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void Delete ()
    {
      base.Delete ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void GetInfoFromCurrentLevel (MiResultValueTuple frameTuple)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (frameTuple == null)
        {
          throw new ArgumentNullException ("frameTuple");
        }

        if (frameTuple.HasField ("level"))
        {
          m_stackLevel = frameTuple ["level"] [0].GetUnsignedInt ();
        }

        // 
        // Discover the function or shared library location.
        // 

        if (frameTuple.HasField ("addr"))
        {
          m_locationAddress = new DebuggeeAddress (frameTuple ["addr"] [0].GetString ());
        }
        else
        {
          m_locationAddress = new DebuggeeAddress ("0x0");
        }

        if (frameTuple.HasField ("func"))
        {
          m_locationFunction = frameTuple ["func"] [0].GetString ();
        }
        else
        {
          m_locationFunction = "??";
        }

        m_locationIsSymbolicated = !(m_locationFunction.Equals ("??"));

        if (frameTuple.HasField ("from"))
        {
          m_locationModule = Path.GetFileName (frameTuple ["from"] [0].GetString ());
        }
        else
        {
          m_locationModule = string.Empty;
        }

        // 
        // Generate code and document contexts for this frame location.
        // 

        if (frameTuple.HasField ("fullname") && frameTuple.HasField ("line"))
        {
          // 
          // If the symbol table isn't yet loaded, we'll need to specify exactly the location of this stack frame.
          // 

          TEXT_POSITION [] textPositions = new TEXT_POSITION [2];

          textPositions [0].dwLine = frameTuple ["line"] [0].GetUnsignedInt () - 1;

          textPositions [0].dwColumn = 0;

          textPositions [1].dwLine = textPositions [0].dwLine;

          textPositions [1].dwColumn = textPositions [0].dwColumn;

          string filename = PathUtils.ConvertPathCygwinToWindows (frameTuple ["fullname"] [0].GetString ());

          m_documentContext = new DebuggeeDocumentContext (m_debugger.Engine, filename, textPositions [0], textPositions [1], DebugEngineGuids.guidLanguageCpp, m_locationAddress);

          m_codeContext = m_documentContext.GetCodeContext ();

          if (m_codeContext == null)
          {
            throw new InvalidOperationException ();
          }
        }
        else
        {
          m_codeContext = m_debugger.GetCodeContextForLocation ("*" + m_locationAddress.ToString ());

          m_documentContext = (m_codeContext != null) ? m_codeContext.DocumentContext : null;
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

    protected override int QueryArgumentsAndLocals ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (!m_queriedArgumentsAndLocals)
        {
          uint threadId;

          LoggingUtils.RequireOk (m_thread.GetThreadId (out threadId));

          string command = string.Format ("-stack-list-variables --thread {0} --frame {1} --no-values", threadId, StackLevel);

          MiResultRecord resultRecord = m_debugger.GdbClient.SendCommand (command);

          MiResultRecord.RequireOk (resultRecord, command);

          if (resultRecord.HasField ("variables"))
          {
            MiResultValue localVariables = resultRecord ["variables"] [0];

            for (int i = 0; i < localVariables.Values.Count; ++i)
            {
              string variableName = localVariables [i] ["name"] [0].GetString ();

              MiVariable variable = m_debugger.VariableManager.CreateVariableFromExpression (this, variableName);

              if (variable == null)
              {
                continue;
              }

              CLangDebuggeeProperty property = m_debugger.VariableManager.CreatePropertyFromVariable (this, variable);

              if (property == null)
              {
                throw new InvalidOperationException ();
              }

              if (localVariables [i].HasField ("arg"))
              {
                m_stackArguments.TryAdd (variableName, property);

                LoggingUtils.RequireOk (m_property.AddChildren (new DebuggeeProperty [] { property }));
              }
              else
              {
                m_stackLocals.TryAdd (variableName, property);

                LoggingUtils.RequireOk (m_property.AddChildren (new DebuggeeProperty [] { property }));
              }

              //LoggingUtils.RequireOk (m_property.AddChildren (new DebuggeeProperty [] { property }));
            }
          }

          m_queriedArgumentsAndLocals = true;
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override int QueryRegisters ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        // 
        // Returns a list of registers for the current stack level.
        // 

        if (!m_queriedRegisters)
        {
          uint threadId;

          LoggingUtils.RequireOk (m_thread.GetThreadId (out threadId));

          string command = string.Format ("-data-list-register-values --thread {0} --frame {1} r", threadId, StackLevel);

          MiResultRecord resultRecord = m_debugger.GdbClient.SendCommand (command);

          MiResultRecord.RequireOk (resultRecord, command);

          if (!resultRecord.HasField ("register-values"))
          {
            throw new InvalidOperationException ("Failed to retrieve list of register values");
          }

          MiResultValue registerValues = resultRecord ["register-values"] [0];

          int registerValuesCount = registerValues.Values.Count;

          Dictionary<uint, string> registerIdMapping = m_debugger.GdbClient.GetRegisterIdMapping ();

          for (int i = 0; i < registerValuesCount; ++i)
          {
            uint registerId = registerValues [i] ["number"] [0].GetUnsignedInt ();

            string registerValue = registerValues [i] ["value"] [0].GetString ();

            string registerName = registerIdMapping [registerId];

            string registerNamePrettified = "$" + registerName;

            CLangDebuggeeProperty property = new CLangDebuggeeProperty (m_debugger, this, registerNamePrettified, registerValue);

            m_stackRegisters.TryAdd (registerNamePrettified, property);

            LoggingUtils.RequireOk (m_property.AddChildren (new DebuggeeProperty [] { property }));
          }

          m_queriedRegisters = true;
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeProperty EvaluateCustomExpression (enum_EVALFLAGS evaluateFlags, string expression, uint radix)
    {
      // 
      // Evaluates a custom property lookup, and registers a new entry for this expression if one can't be found.
      // 

      LoggingUtils.PrintFunction ();

      DebuggeeProperty property = null;

      try
      {
        if (m_stackRegisters.TryGetValue (expression, out property))
        {
          return property;
        }

        if (m_stackArguments.TryGetValue (expression, out property))
        {
          return property;
        }

        if (m_stackLocals.TryGetValue (expression, out property))
        {
          return property;
        }

        if (m_customExpressions.TryGetValue (expression, out property))
        {
          return property;
        }


        // 
        // Check if this expression has already been queried via a child property.
        // 

        // TODO.

        // 
        // Couldn't find a pre-registered matching property for this expression, creating a new custom one.
        // 

        MiVariable customExpressionVariable = m_debugger.VariableManager.CreateVariableFromExpression (this, expression);

        if (customExpressionVariable != null)
        {
          property = m_debugger.VariableManager.CreatePropertyFromVariable (this, customExpressionVariable);

          if (property != null)
          {
            m_customExpressions.TryAdd (expression, property);

            LoggingUtils.RequireOk (m_property.AddChildren (new DebuggeeProperty [] { property }));
          }
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }

      return property;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override int SetFrameInfo (enum_FRAMEINFO_FLAGS requestedFlags, uint radix, ref FRAMEINFO frameInfo)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        frameInfo.m_dwValidFields = 0;

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME) != 0)
        {
          StringBuilder functionName = new StringBuilder ();

          functionName.Append ("[" + m_locationAddress.ToString () + "] ");

          if (((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE) != 0) && (!string.IsNullOrEmpty (m_locationModule)))
          {
            functionName.Append (m_locationModule + "!");
          }

          functionName.Append (m_locationFunction);

          /*if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS) != 0)
          {
            functionName.Append ("(...)");
          }

          if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LINES) != 0)
          {
            functionName.AppendFormat (" Line {0}", "?");
          }*/

          frameInfo.m_bstrFuncName = functionName.ToString ();

          frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME;
        }

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_RETURNTYPE) != 0)
        {
          frameInfo.m_bstrReturnType = "<return type>";

          frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_RETURNTYPE;
        }

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_ARGS) != 0)
        {
          frameInfo.m_bstrArgs = m_stackArguments.Keys.ToString ();

          frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_ARGS;
        }

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_LANGUAGE) != 0)
        {
          string languageName = string.Empty;

          Guid languageGuid = Guid.Empty;

          GetLanguageInfo (ref languageName, ref languageGuid);

          frameInfo.m_bstrLanguage = languageName;

          frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_LANGUAGE;
        }

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_MODULE) != 0)
        {
          frameInfo.m_bstrModule = m_locationModule;

          frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_MODULE;
        }

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_STACKRANGE) != 0)
        {
          frameInfo.m_addrMin = 0ul;

          frameInfo.m_addrMax = 0ul;

          frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STACKRANGE;
        }

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_FRAME) != 0)
        {
          frameInfo.m_pFrame = this;

          frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FRAME;
        }

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO) != 0)
        {
          frameInfo.m_fHasDebugInfo = (m_locationIsSymbolicated) ? 1 : 0;

          frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO;
        }

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_STALECODE) != 0)
        {
          frameInfo.m_fStaleCode = 0;

          frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STALECODE;
        }

        if ((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP) != 0)
        {
          if (!string.IsNullOrEmpty (m_locationModule))
          {
            IDebugProgram2 debugProgram;

            IEnumDebugModules2 debugProgramModules;

            uint debugModulesCount = 0;

            LoggingUtils.RequireOk (m_thread.GetProgram (out debugProgram));

            LoggingUtils.RequireOk (debugProgram.EnumModules (out debugProgramModules));

            LoggingUtils.RequireOk (debugProgramModules.GetCount (out debugModulesCount));

            DebuggeeModule [] debugModules = new DebuggeeModule [debugModulesCount];

            LoggingUtils.RequireOk (debugProgramModules.Next (debugModulesCount, debugModules, ref debugModulesCount));

            for (int i = 0; i < debugModulesCount; ++i)
            {
              if (m_locationModule.Equals (debugModules [i].Name))
              {
                frameInfo.m_pModule = debugModules [i];

                frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP;

                break;
              }
            }
          }
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override int EnumProperties (enum_DEBUGPROP_INFO_FLAGS requestedFields, uint radix, ref Guid guidFilter, uint timeout, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumDebugProperty)
    {
      try
      {
        if ((guidFilter == DebuggeeProperty.Filters.guidFilterRegisters) || (guidFilter == DebuggeeProperty.Filters.guidFilterAutoRegisters))
        {
          LoggingUtils.RequireOk (QueryRegisters ());
        }

        if ((guidFilter == DebuggeeProperty.Filters.guidFilterAllLocals) ||
            (guidFilter == DebuggeeProperty.Filters.guidFilterAllLocalsPlusArgs) ||
            (guidFilter == DebuggeeProperty.Filters.guidFilterArgs) ||
            (guidFilter == DebuggeeProperty.Filters.guidFilterLocals) ||
            (guidFilter == DebuggeeProperty.Filters.guidFilterLocalsPlusArgs))
        {
          LoggingUtils.RequireOk (QueryArgumentsAndLocals ());
        }

        LoggingUtils.RequireOk (base.EnumProperties (requestedFields, radix, ref guidFilter, timeout, out elementsReturned, out enumDebugProperty));

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        elementsReturned = 0;

        enumDebugProperty = null;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override int GetLanguageInfo (ref string languageName, ref Guid languageGuid)
    {
      // 
      // Gets the language associated with this stack frame. 
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        IDebugDocumentContext2 documentContext = null;

        LoggingUtils.RequireOk (GetDocumentContext (out documentContext));

        if (documentContext == null)
        {
          throw new InvalidOperationException ();
        }

        LoggingUtils.RequireOk (documentContext.GetLanguageInfo (ref languageName, ref languageGuid));

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
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
