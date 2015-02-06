////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Debugger.Interop;
using AndroidPlusPlus.Common;
using System.Threading;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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

    public override int EvaluateBreakpointLocation (out DebuggeeDocumentContext documentContext, out DebuggeeCodeContext codeContext, out string location)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        LoggingUtils.RequireOk (base.EvaluateBreakpointLocation (out documentContext, out codeContext, out location));

        switch (m_breakpointRequestInfo.bpLocation.bpLocationType)
        {
          case (uint) enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE:
          {
            DebuggeeCodeContext gdbCodeContext = m_debugger.GetCodeContextForLocation (location);

            if (gdbCodeContext != null)
            {
              DebuggeeDocumentContext gdbDocumentContext = gdbCodeContext.DocumentContext;

              if (gdbDocumentContext == null)
              {
                throw new InvalidOperationException ();
              }

              codeContext = gdbCodeContext;

              documentContext = gdbDocumentContext;
            }

            break;
          }

          default:
          {
            break;
          }
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        documentContext = null;

        codeContext = null;

        location = string.Empty;

        return DebugEngineConstants.E_FAIL;
      }
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
        if ((m_breakpointRequestInfo.bpLocation.bpLocationType & (uint)enum_BP_TYPE.BPT_DATA) != 0)
        {
          throw new NotImplementedException ();
        }

        m_debugger.RunInterruptOperation (delegate (CLangDebugger debugger)
        {
          string command = string.Format ("-break-insert -f {0} {1}", ((m_breakpointEnabled) ? "" : "-d"), PathUtils.SantiseWindowsPath (location));

          MiResultRecord resultRecord = debugger.GdbClient.SendCommand (command);

          if (resultRecord != null)
          {
            if (resultRecord.IsError ())
            {
              string errorReason = "<unknown error>";

              if (resultRecord.HasField ("msg"))
              {
                errorReason = resultRecord ["msg"] [0].GetString ();
              }

              LoggingUtils.RequireOk (CreateErrorBreakpoint (errorReason, documentContext, documentContext.GetCodeContext ()));
            }
            else
            {
              MiResultValue breakpointData = resultRecord ["bkpt"] [0];

              MiBreakpoint breakpoint = new MiBreakpoint (breakpointData.Values);

              LoggingUtils.RequireOk (CreateBoundBreakpoint (breakpoint, documentContext, codeContext));
            }
          }
        });

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

    public int CreateErrorBreakpoint (string errorReason, MiBreakpoint gdbBreakpoint, DebuggeeDocumentContext documentContext, DebuggeeCodeContext codeContext)
    {
      // 
      // Create a C-language breakpoint. This is tied to a GDB/MI breakpoint object.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        CLangDebuggeeBreakpointError errorBreakpoint = new CLangDebuggeeBreakpointError (m_debugger, m_breakpointManager, this, documentContext.GetCodeContext (), gdbBreakpoint, errorReason);

        lock (m_errorBreakpoints)
        {
          m_errorBreakpoints.Add (errorBreakpoint);
        }

        uint numDebugPrograms = 1;

        IEnumDebugPrograms2 debugPrograms;

        IDebugProgram2 [] debugProgramsArray = new IDebugProgram2 [numDebugPrograms];

        LoggingUtils.RequireOk (m_breakpointManager.Engine.EnumPrograms (out debugPrograms));

        LoggingUtils.RequireOk (debugPrograms.Next (numDebugPrograms, debugProgramsArray, ref numDebugPrograms));

        m_breakpointManager.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), debugProgramsArray [0], null);

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

    public int CreateBoundBreakpoint (MiBreakpoint breakpoint, DebuggeeDocumentContext documentContext, DebuggeeCodeContext codeContext)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (breakpoint == null)
        {
          throw new ArgumentNullException ("breakpoint");
        }

        if (breakpoint.IsPending ())
        {
          // 
          // Address can't be satisfied. Unsatisfied likely indicates the modules or symbols associated with the context aren't loaded, yet.
          // 

          LoggingUtils.RequireOk (CreateErrorBreakpoint ("Additional library symbols required.", breakpoint, documentContext, documentContext.GetCodeContext ()));
        }
        else if (breakpoint.IsMultiple ())
        {
          // 
          // Breakpoint satisfied to multiple locations, no single memory address available.
          // 

          CLangDebuggeeBreakpointBound boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, documentContext.GetCodeContext (), breakpoint);

          lock (m_boundBreakpoints)
          {
            m_boundBreakpoints.Clear ();

            m_boundBreakpoints.Add (boundBreakpoint);
          }

          m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
        }
        else
        {
          // 
          // Address satisfied, and the breakpoint is legitimately bound.
          // 

          DebuggeeAddress boundAddress = new DebuggeeAddress (breakpoint.Address);

          DebuggeeCodeContext addressContext = new DebuggeeCodeContext (m_debugger.Engine, documentContext, boundAddress);

          CLangDebuggeeBreakpointBound boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, addressContext, breakpoint);

          lock (m_boundBreakpoints)
          {
            m_boundBreakpoints.Clear ();

            m_boundBreakpoints.Add (boundBreakpoint);
          }

          m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
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

    public override void RefreshBoundBreakpoints ()
    {
      // 
      // Refresh the status of any active/satisfied breakpoints.
      // 

      LoggingUtils.PrintFunction ();

      lock (m_boundBreakpoints)
      {
        if (m_boundBreakpoints.Count > 0)
        {
          List<IDebugBoundBreakpoint2> boundBreakpoints = new List<IDebugBoundBreakpoint2> (m_boundBreakpoints);

          foreach (IDebugBoundBreakpoint2 boundBreakpoint in boundBreakpoints)
          {
            RefreshBreakpoint (boundBreakpoint);
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void RefreshErrorBreakpoints ()
    {
      // 
      // Refresh the status of any previously failed breakpoints. Ignore any DebuggeeBreakpointError base class objects.
      // 

      LoggingUtils.PrintFunction ();

      lock (m_errorBreakpoints)
      {
        if (m_errorBreakpoints.Count > 0)
        {
          List<IDebugErrorBreakpoint2> errorBreakpoints = new List<IDebugErrorBreakpoint2> (m_errorBreakpoints);

          foreach (IDebugErrorBreakpoint2 errorBreakpoint in errorBreakpoints)
          {
            if (errorBreakpoint is CLangDebuggeeBreakpointError)
            {
              RefreshBreakpoint (errorBreakpoint);
            }
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void RefreshBreakpoint (object breakpoint)
    {
      // 
      // Validate breakpoint input type. This function can be used for 'bound' and 'error' objects, so we need to handle this appropriately.
      // 

      LoggingUtils.PrintFunction ();

      CLangDebuggeeBreakpointBound boundBreakpoint = null;

      CLangDebuggeeBreakpointError errorBreakpoint = null;

      MiBreakpoint gdbBreakpoint;

      DebuggeeBreakpointResolution resolution;

      if (breakpoint == null)
      {
        throw new ArgumentNullException ("breakpoint");
      }
      else if (breakpoint is CLangDebuggeeBreakpointBound)
      {
        boundBreakpoint = breakpoint as CLangDebuggeeBreakpointBound;

        gdbBreakpoint = boundBreakpoint.GdbBreakpoint;

        IDebugBreakpointResolution2 boundBreakpointResolution;

        int handle = boundBreakpoint.GetBreakpointResolution (out boundBreakpointResolution);

        if (handle == DebugEngineConstants.E_BP_DELETED)
        {
          return;
        }

        LoggingUtils.RequireOk (handle);

        resolution = (DebuggeeBreakpointResolution) boundBreakpointResolution;
      }
      else if (breakpoint is CLangDebuggeeBreakpointError)
      {
        errorBreakpoint = breakpoint as CLangDebuggeeBreakpointError;

        gdbBreakpoint = errorBreakpoint.GdbBreakpoint;

        IDebugErrorBreakpointResolution2 errorBreakpointResolution;

        int handle = errorBreakpoint.GetBreakpointResolution (out errorBreakpointResolution);

        if (handle == DebugEngineConstants.E_BP_DELETED)
        {
          return;
        }

        resolution = (DebuggeeBreakpointResolution) errorBreakpointResolution;

        lock (m_errorBreakpoints)
        {
          m_errorBreakpoints.Remove (errorBreakpoint);
        }
      }
      else
      {
        throw new ArgumentException ("breakpoint");
      }

      // 
      // Query breakpoint info/status directly from GDB/MI.
      // 

      MiResultRecord resultRecord = m_debugger.GdbClient.SendCommand (string.Format ("-break-info {0}", gdbBreakpoint.ID));

      if (resultRecord == null)
      {
        throw new InvalidOperationException ();
      }
      else if (resultRecord.IsError ())
      {
        // 
        // GDB/MI breakpoint info request failed.
        // 

        gdbBreakpoint.Address = MiBreakpoint.Pending;

        (resolution as DebuggeeBreakpointResolution).CodeContext.Address = new DebuggeeAddress (gdbBreakpoint.Address);

        errorBreakpoint = new CLangDebuggeeBreakpointError (m_debugger, m_breakpointManager, this, (resolution as DebuggeeBreakpointResolution).CodeContext, gdbBreakpoint, resultRecord.Records [1].Stream);

        lock (m_errorBreakpoints)
        {
          m_errorBreakpoints.Add (errorBreakpoint);
        }

        m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
      }
      else
      {
        MiResultValue breakpointData = resultRecord ["BreakpointTable"] [0] ["body"] [0] ["bkpt"] [0];

        gdbBreakpoint = new MiBreakpoint (breakpointData.Values);

        DebuggeeCodeContext codeContext = (resolution as DebuggeeBreakpointResolution).CodeContext;

        DebuggeeDocumentContext documentContext = codeContext.DocumentContext;

        LoggingUtils.RequireOk (CreateBoundBreakpoint (gdbBreakpoint, documentContext, codeContext));
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override int Bind ()
    {
      // 
      // Binds this pending breakpoint to one or more code locations.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        LoggingUtils.RequireOk (base.Bind ());

        LoggingUtils.RequireOk (SetPassCount (m_breakpointRequestInfo.bpPassCount));

        LoggingUtils.RequireOk (SetCondition (m_breakpointRequestInfo.bpCondition));

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
