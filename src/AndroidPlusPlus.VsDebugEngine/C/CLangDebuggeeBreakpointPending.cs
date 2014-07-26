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
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        documentContext = null;

        codeContext = null;

        location = string.Empty;

        LoggingUtils.HandleException (e);

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

        m_debugger.RunInterruptOperation (delegate ()
        {
          MiResultRecord resultRecord = m_debugger.GdbClient.SendCommand (string.Format ("-break-insert -f {0} {1}", ((m_breakpointEnabled) ? "" : "-d"), PathUtils.SantiseWindowsPath (location)));

          if (resultRecord != null)
          {
            if (resultRecord.IsError ())
            {
              DebuggeeBreakpointError errorBreakpoint = new DebuggeeBreakpointError (m_breakpointManager, this, documentContext.GetCodeContext (), resultRecord.Records [1].Stream);

              m_errorBreakpoints.Add (errorBreakpoint);

              m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
            }
            else if (resultRecord.HasField ("bkpt"))
            {
              uint number = resultRecord ["bkpt"] [0] ["number"] [0].GetUnsignedInt ();

              string addr = resultRecord ["bkpt"] [0] ["addr"] [0].GetString ();

              if (!string.IsNullOrEmpty (addr))
              {
                bool pending = addr.Equals ("<PENDING>");

                bool multiple = addr.Equals ("<MULTIPLE>");

                if (pending)
                {
                  // 
                  // Address can't be satisfied. Unsatisfied likely indicates the modules or symbols associated with the context aren't loaded, yet.
                  // 

                  MiBreakpoint boundGdbBreakpoint = new MiBreakpoint (number, MiBreakpoint.Pending);

                  CLangDebuggeeBreakpointError errorBreakpoint = new CLangDebuggeeBreakpointError (m_debugger, m_breakpointManager, this, documentContext.GetCodeContext (), boundGdbBreakpoint, "Additional library symbols required.");

                  m_errorBreakpoints.Add (errorBreakpoint);

                  m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
                }
                else if (multiple)
                {
                  // 
                  // Breakpoint satisfied to multiple locations, no single memory address available.
                  // 

                  MiBreakpoint boundGdbBreakpoint = new MiBreakpoint (number, MiBreakpoint.Multiple);

                  CLangDebuggeeBreakpointBound boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, documentContext.GetCodeContext (), boundGdbBreakpoint);

                  m_boundBreakpoints.Clear ();

                  m_boundBreakpoints.Add (boundBreakpoint);

                  m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
                }
                else
                {
                  // 
                  // Address satisfied, and the breakpoint is legitimately bound.
                  // 

                  DebuggeeAddress boundAddress = new DebuggeeAddress (addr);

                  DebuggeeCodeContext addressContext = new DebuggeeCodeContext (m_debugger.Engine, documentContext, boundAddress);

                  MiBreakpoint boundGdbBreakpoint = new MiBreakpoint (number, boundAddress.MemoryAddress);

                  CLangDebuggeeBreakpointBound boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, addressContext, boundGdbBreakpoint);

                  m_boundBreakpoints.Clear ();

                  m_boundBreakpoints.Add (boundBreakpoint);

                  m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
                }
              }
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

    public override void RefreshBoundBreakpoints ()
    {
      // 
      // Refresh the status of any active/satisfied breakpoints.
      // 

      LoggingUtils.PrintFunction ();

      foreach (IDebugBoundBreakpoint2 boundBreakpoint in m_boundBreakpoints)
      {
        RefreshBreakpoint (boundBreakpoint);
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

      foreach (IDebugErrorBreakpoint2 errorBreakpoint in m_errorBreakpoints)
      {
        if (errorBreakpoint is CLangDebuggeeBreakpointError)
        {
          RefreshBreakpoint (errorBreakpoint);
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

        LoggingUtils.RequireOk (boundBreakpoint.GetBreakpointResolution (out boundBreakpointResolution));

        resolution = (DebuggeeBreakpointResolution) boundBreakpointResolution;
      }
      else if (breakpoint is CLangDebuggeeBreakpointError)
      {
        errorBreakpoint = breakpoint as CLangDebuggeeBreakpointError;

        gdbBreakpoint = errorBreakpoint.GdbBreakpoint;

        IDebugErrorBreakpointResolution2 errorBreakpointResolution;

        LoggingUtils.RequireOk (errorBreakpoint.GetBreakpointResolution (out errorBreakpointResolution));

        resolution = (DebuggeeBreakpointResolution) errorBreakpointResolution;
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

        m_errorBreakpoints.Clear ();

        m_errorBreakpoints.Add (errorBreakpoint);

        m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
      }
      else
      {
        string addr = resultRecord ["BreakpointTable"] [0] ["body"] [0] ["bkpt"] [0] ["addr"] [0].GetString ();

        if (!string.IsNullOrEmpty (addr))
        {
          bool pending = addr.Equals ("<PENDING>");

          bool multiple = addr.Equals ("<MULTIPLE>");

          if (pending && (!gdbBreakpoint.IsPending ()))
          {
            // 
            // Address can't be satisfied. Unsatisfied likely indicates the modules or symbols associated with the context aren't loaded, yet.
            // 

            gdbBreakpoint.Address = MiBreakpoint.Pending;

            (resolution as DebuggeeBreakpointResolution).CodeContext.Address = new DebuggeeAddress (gdbBreakpoint.Address);

            errorBreakpoint = new CLangDebuggeeBreakpointError (m_debugger, m_breakpointManager, this, (resolution as DebuggeeBreakpointResolution).CodeContext, gdbBreakpoint, "Additional library symbols required.");

            m_errorBreakpoints.Clear ();

            m_errorBreakpoints.Add (errorBreakpoint);

            m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
          }
          else if (multiple && (gdbBreakpoint.IsMultiple ()))
          {
            // 
            // Breakpoint satisfied to multiple locations, no single memory address available.
            // 

            gdbBreakpoint.Address = MiBreakpoint.Multiple;

            (resolution as DebuggeeBreakpointResolution).CodeContext.Address = new DebuggeeAddress (gdbBreakpoint.Address);

            errorBreakpoint = new CLangDebuggeeBreakpointError (m_debugger, m_breakpointManager, this, (resolution as DebuggeeBreakpointResolution).CodeContext, gdbBreakpoint, "Breakpoint satisfied to multiple locations, no single memory address available.");

            m_errorBreakpoints.Clear ();

            m_errorBreakpoints.Add (errorBreakpoint);

            m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
          }
          else if (!pending && !multiple)
          {
            // 
            // Address satisfied, and the breakpoint is legitimately bound.
            // 

            DebuggeeAddress boundAddress = new DebuggeeAddress (addr);

            if (gdbBreakpoint.Address != boundAddress.MemoryAddress)
            {
              gdbBreakpoint.Address = boundAddress.MemoryAddress;

              DebuggeeCodeContext addressContext = (resolution as DebuggeeBreakpointResolution).CodeContext;

              addressContext.Address = boundAddress;

              boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, addressContext, gdbBreakpoint);

              m_boundBreakpoints.Clear ();

              m_boundBreakpoints.Add (boundBreakpoint);

              m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
            }
          }
        }
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
