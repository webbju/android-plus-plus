////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;
using Microsoft.VisualStudio.Debugger.Interop;
using System;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  public class CLangDebuggeeBreakpointPending : DebuggeeBreakpointPending
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly CLangDebugger m_debugger;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeBreakpointPending (CLangDebugger debugger, DebugBreakpointManager breakpointManager, IDebugBreakpointRequest2 breakpointRequest)
      : base (breakpointManager, breakpointRequest)
    {
      m_debugger = debugger;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override int CreateBoundBreakpoint (string location, DebuggeeDocumentContext documentContext, DebuggeeCodeContext codeContext)
    {
      //
      // Register a new GDB breakpoint.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        if (m_breakpointRequestInfo.bpLocation.bpLocationType == (uint)enum_BP_TYPE.BPT_DATA)
        {
          throw new NotImplementedException ();
        }

        m_debugger.RunInterruptOperation (async (CLangDebugger debugger) =>
        {
          string command = string.Format ("-break-insert -f {0} {1}", ((m_breakpointEnabled) ? "" : "-d"), PathUtils.SantiseWindowsPath (location));

          debugger.GdbClient.SendCommand (command, delegate (MiResultRecord resultRecord)
          {
            if (resultRecord == null)
            {
              return;
            }
            else if (resultRecord.IsError)
            {
              string errorReason = "<unknown error>";

              if (resultRecord.HasField ("msg"))
              {
                errorReason = resultRecord ["msg"] [0].GetString ();
              }

              LoggingUtils.RequireOk (CreateErrorBreakpoint (errorReason, null, documentContext, codeContext));
            }
            else
            {
              MiResultValue breakpointData = resultRecord ["bkpt"] [0];

              MiBreakpoint breakpoint = new MiBreakpoint (breakpointData.Values);

              LoggingUtils.RequireOk (CreateBoundBreakpoint (breakpoint, documentContext, codeContext));
            }
          });
        });

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

    protected int CreateErrorBreakpoint (string errorReason, MiBreakpoint gdbBreakpoint, DebuggeeDocumentContext documentContext, DebuggeeCodeContext codeContext)
    {
      //
      // Create a C-language breakpoint. This is tied to a GDB/MI breakpoint object.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        ClearErrorBreakpoints();

        var errorBreakpoint = new CLangDebuggeeBreakpointError (m_debugger, m_breakpointManager, this, codeContext, gdbBreakpoint, errorReason);

        m_errorBreakpoints.Add (errorBreakpoint);

        m_breakpointManager.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), m_debugger.NativeProgram, null);

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

    protected int CreateBoundBreakpoint (MiBreakpoint breakpoint, DebuggeeDocumentContext documentContext, DebuggeeCodeContext codeContext)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (breakpoint == null)
        {
          throw new ArgumentNullException (nameof(breakpoint));
        }

        if (breakpoint.IsPending)
        {
          //
          // Address can't be satisfied. Unsatisfied likely indicates the modules or symbols associated with the context aren't loaded, yet.
          //

          DebuggeeAddress pendingAddress = new DebuggeeAddress (MiBreakpoint.Pending);

          DebuggeeCodeContext pendingContext = new CLangDebuggeeCodeContext (m_debugger, pendingAddress, documentContext);

          LoggingUtils.RequireOk (CreateErrorBreakpoint ("Additional library symbols required.", breakpoint, documentContext, pendingContext));
        }
        else if (breakpoint.IsMultiple)
        {
          //
          // Breakpoint satisfied to multiple locations, no single memory address available.
          //

          ClearBoundBreakpoints();

          CLangDebuggeeBreakpointBound boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, codeContext, breakpoint);

          m_boundBreakpoints.Add(boundBreakpoint);

          m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
        }
        else
        {
          //
          // Address satisfied, and the breakpoint is legitimately bound.
          //

          DebuggeeAddress boundAddress = new DebuggeeAddress (breakpoint.Address);

          DebuggeeCodeContext addressContext = new CLangDebuggeeCodeContext (m_debugger, boundAddress, documentContext);

          CLangDebuggeeBreakpointBound boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, addressContext, breakpoint);

          ClearBoundBreakpoints();

          m_boundBreakpoints.Add (boundBreakpoint);

          m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
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

    protected override void RefreshBreakpoint (object breakpoint)
    {
      //
      // Validate breakpoint input type. This function can be used for 'bound' and 'error' objects, so we need to handle this appropriately.
      //

      LoggingUtils.PrintFunction ();

      if (breakpoint is CLangDebuggeeBreakpointBound boundBreakpoint)
      {
        int handle = boundBreakpoint.GetBreakpointResolution(out IDebugBreakpointResolution2 boundBreakpointResolution);

        if (handle == Constants.E_BP_DELETED)
        {
          return;
        }

        LoggingUtils.RequireOk (handle);

        QueryBreakpointStatus((DebuggeeBreakpointResolution)boundBreakpointResolution, boundBreakpoint.GdbBreakpoint);
      }
      else if (breakpoint is CLangDebuggeeBreakpointError errorBreakpoint)
      {
        int handle = errorBreakpoint.GetBreakpointResolution(out IDebugErrorBreakpointResolution2 errorBreakpointResolution);

        if (handle == Constants.E_BP_DELETED)
        {
          return;
        }

        LoggingUtils.RequireOk(handle);

        QueryBreakpointStatus((DebuggeeBreakpointResolution)errorBreakpointResolution, errorBreakpoint.GdbBreakpoint);
      }
      else
      {
        throw new ArgumentException (nameof(breakpoint));
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void QueryBreakpointStatus (DebuggeeBreakpointResolution breakpointResolution, MiBreakpoint miBreakpoint)
    {
      //
      // Query breakpoint info/status directly from GDB/MI.
      //

      string command = string.Format("-break-info {0}", miBreakpoint.ID);

      m_debugger.GdbClient.SendCommand(command, (MiResultRecord resultRecord) =>
      {
        if (resultRecord == null)
        {
          throw new ArgumentNullException(nameof(resultRecord));
        }
        else if (resultRecord.IsError())
        {
          //
          // GDB/MI breakpoint info request failed.
          //

          miBreakpoint.Address = MiBreakpoint.Pending;

          breakpointResolution.CodeContext.Address = new DebuggeeAddress(miBreakpoint.Address);

          CreateErrorBreakpoint(resultRecord.Records[1].Stream, miBreakpoint, null, breakpointResolution.CodeContext);
        }
        else
        {
          //
          // We've probably got sane breakpoint information back. Update current breakpoint values and re-process.
          //

          var breakpointData = resultRecord["BreakpointTable"][0]["body"][0]["bkpt"][0];

          var currentGdbBreakpoint = new MiBreakpoint(breakpointData.Values);

          var codeContext = breakpointResolution.CodeContext;

          LoggingUtils.RequireOk(CreateBoundBreakpoint(currentGdbBreakpoint, codeContext.DocumentContext, codeContext));
        }
      });
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

}
