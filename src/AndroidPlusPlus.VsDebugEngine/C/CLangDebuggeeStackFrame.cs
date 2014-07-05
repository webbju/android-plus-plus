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

    private string m_locationId;

    private DebuggeeAddress m_locationAddress;

    private string m_locationFunction;

    private string m_locationModule;

    private bool m_locationIsSymbolicated;

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

      GetInfoFromCurrentLevel (frameTuple);

      LoggingUtils.RequireOk (QueryArgumentsAndLocals ());

      LoggingUtils.RequireOk (QueryRegisters ());

      m_property = new CLangDebuggeeProperty (debugger, this, frameName);

      DebuggeeProperty [] arguments = new DebuggeeProperty [m_stackArguments.Count];

      m_stackArguments.Values.CopyTo (arguments, 0);

      m_property.AddChildren (arguments);

      DebuggeeProperty [] locals = new DebuggeeProperty [m_stackLocals.Count];

      m_stackLocals.Values.CopyTo (locals, 0);

      m_property.AddChildren (locals);
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
        // Construct a descriptive location identifier.
        // 

        StringBuilder locationBuilder = new StringBuilder ();

        locationBuilder.Append ("[" + m_locationAddress.ToString () + "]");

        locationBuilder.Append (" " + m_locationFunction);

        if (!string.IsNullOrEmpty (m_locationModule))
        {
          locationBuilder.Append (" (" + m_locationModule + ")");
        }

        m_locationId = locationBuilder.ToString ();

        // 
        // Generate code and document contexts for this frame location.
        // 

        if (frameTuple.HasField ("fullname") && frameTuple.HasField ("line"))
        {
          TEXT_POSITION [] textPositions = new TEXT_POSITION [2];

          textPositions [0].dwLine = frameTuple ["line"] [0].GetUnsignedInt () - 1;

          textPositions [0].dwColumn = 0;

          textPositions [1].dwLine = textPositions [0].dwLine;

          textPositions [1].dwColumn = textPositions [0].dwColumn;

          string filename = PathUtils.ConvertPathCygwinToWindows (frameTuple ["fullname"] [0].GetString ());

          m_documentContext = new DebuggeeDocumentContext (m_debugger.Engine, filename, textPositions [0], textPositions [1], DebugEngineGuids.guidLanguageCpp, m_locationAddress);

          m_codeContext = m_documentContext.GetCodeContext ();
        }
        else
        {
          m_codeContext = m_debugger.GetCodeContextForLocation ("*" + m_locationAddress.ToString ());

          if (m_codeContext != null)
          {
            m_documentContext = m_codeContext.DocumentContext;
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

    protected override int QueryArgumentsAndLocals ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        uint threadId;

        LoggingUtils.RequireOk (m_thread.GetThreadId (out threadId));

        MiResultRecord resultRecord = m_debugger.GdbClient.SendCommand (string.Format ("-stack-list-variables --thread {0} --frame {1} --no-values", threadId, StackLevel));

        if ((resultRecord == null) || ((resultRecord != null) && resultRecord.IsError ()))
        {
          throw new InvalidOperationException ();
        }

        MiResultValue localVariables = resultRecord ["variables"] [0];

        for (int i = 0; i < localVariables.Values.Count; ++i)
        {
          string variableName = localVariables [i] ["name"] [0].GetString ();

          MiVariable variable = m_debugger.VariableManager.CreateVariableFromExpression (this, variableName);

          DebuggeeProperty property = m_debugger.VariableManager.CreatePropertyFromVariable (this, variable);

          if (localVariables [i].HasField ("arg"))
          {
            m_stackArguments.TryAdd (variableName, property);
          }
          else
          {
            m_stackLocals.TryAdd (variableName, property);
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

    protected override int QueryRegisters ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (m_stackRegisters.Count == 0)
        {
          // 
          // Returns a list of registers for the current stack level. Caches results for faster lookup.
          // 

          uint threadId;

          LoggingUtils.RequireOk (m_thread.GetThreadId (out threadId));

          MiResultRecord registerValueRecord = m_debugger.GdbClient.SendCommand (string.Format ("-data-list-register-values --thread {0} --frame {1} r", threadId, StackLevel));

          if ((registerValueRecord == null) || (registerValueRecord.IsError ()) || (!registerValueRecord.HasField ("register-values")))
          {
            throw new InvalidOperationException ("Failed to retrieve list of register values");
          }

          MiResultValue registerValues = registerValueRecord ["register-values"] [0];

          Dictionary<uint, string> registerIdMapping = m_debugger.GdbClient.GetRegisterIdMapping ();

          for (int i = 0; i < registerValues.Values.Count; ++i)
          {
            uint registerId = registerValues [i] ["number"] [0].GetUnsignedInt ();

            string registerValue = registerValues [i] ["value"] [0].GetString ();

            string registerName = "$" + registerIdMapping [registerId];

            string registerNamePrettified = "$" + registerName;

            CLangDebuggeeProperty property = new CLangDebuggeeProperty (m_debugger, this, registerNamePrettified);

            property.Value = registerValue;

            m_stackRegisters.TryAdd (registerNamePrettified, property);
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

    public DebuggeeProperty EvaluateCustomExpression (string expression, uint radix)
    {
      // 
      // Evaluates a custom property lookup, and registers a new entry for this expression if one can't be found.
      // 

      LoggingUtils.PrintFunction ();

      DebuggeeProperty property = null;

      try
      {
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

        if (m_stackRegisters.TryGetValue (expression, out property))
        {
          return property;
        }

        // 
        // Couldn't find a pre-registered matching property for this expression, creating a new custom one.
        // 

        MiVariable customExpressionVariable = m_debugger.VariableManager.CreateVariableFromExpression (this, expression);

        if (customExpressionVariable != null)
        {
          property = m_debugger.VariableManager.CreatePropertyFromVariable (this, customExpressionVariable);

          m_customExpressions.TryAdd (expression, property);
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
          frameInfo.m_bstrFuncName = m_locationId;

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

        if (((requestedFlags & enum_FRAMEINFO_FLAGS.FIF_LANGUAGE) != 0) && (m_documentContext != null))
        {
          string languageName = string.Empty;

          Guid languageGuid = Guid.Empty;

          m_documentContext.GetLanguageInfo (ref languageName, ref languageGuid);

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
          frameInfo.m_addrMin = 0L;

          frameInfo.m_addrMax = 0L;

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

    public override int GetLanguageInfo (ref string languageName, ref Guid languageGuid)
    {
      // 
      // Gets the language associated with this stack frame. 
      // 

      LoggingUtils.PrintFunction ();

      languageName = "C++";

      languageGuid = DebugEngineGuids.guidLanguageCpp;

      try
      {
        IDebugDocumentContext2 documentContext = null;

        GetDocumentContext (out documentContext);

        if (documentContext != null)
        {
          LoggingUtils.RequireOk (documentContext.GetLanguageInfo (ref languageName, ref languageGuid));
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

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
