////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        if (location.StartsWith ("0x"))
        {
          location = "*" + location;
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
          MiResultRecord resultRecord = m_debugger.GdbClient.SendCommand (string.Format ("-break-insert -f {0} {1}", ((m_breakpointEnabled) ? "" : "-d"), location));

          if ((resultRecord != null) && (!resultRecord.IsError ()))
          {
            CLangDebuggeeBreakpointBound boundBreakpoint = null;

            DebuggeeBreakpointError errorBreakpoint = null;

            if (resultRecord.HasField ("bkpt"))
            {
              uint number = resultRecord ["bkpt"] ["number"].GetUnsignedInt ();

              string addr = resultRecord ["bkpt"] ["addr"].GetString ();

              if (!string.IsNullOrEmpty (addr))
              {
                bool pending = addr.Equals ("<PENDING>");

                bool multiple = addr.Equals ("<MULTIPLE>");

                if (pending)
                {
                  // 
                  // Address can't be satistfied. Unsatisified likely indicates the modules or symbols associated with the context aren't loaded, yet.
                  // 

                  MiBreakpoint boundGdbBreakpoint = boundGdbBreakpoint = new MiBreakpoint (number, MiBreakpoint.Pending);

                  boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, documentContext.GetCodeContext (), boundGdbBreakpoint);

                  errorBreakpoint = new DebuggeeBreakpointError (m_breakpointManager, this, documentContext.GetCodeContext (), "Bound breakpoint waiting on additional library symbols");
                }
                else if (multiple)
                {
                  // 
                  // Breakpoint satisfied to multiple locations, no single memory address available.
                  // 

                  MiBreakpoint boundGdbBreakpoint = boundGdbBreakpoint = new MiBreakpoint (number, MiBreakpoint.Multiple);

                  boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, documentContext.GetCodeContext (), boundGdbBreakpoint);
                }
                else
                {
                  // 
                  // Address satistfied, and the breakpoint is legitimately bound.
                  // 

                  DebuggeeAddress boundAddress = new DebuggeeAddress (addr);

                  DebuggeeCodeContext addressContext = new DebuggeeCodeContext (m_debugger.Engine, documentContext, boundAddress);

                  MiBreakpoint boundGdbBreakpoint = boundGdbBreakpoint = new MiBreakpoint (number, boundAddress.MemoryAddress);

                  boundBreakpoint = new CLangDebuggeeBreakpointBound (m_debugger, m_breakpointManager, this, addressContext, boundGdbBreakpoint);
                }
              }
            }

            if (boundBreakpoint != null)
            {
              m_boundBreakpoints.Add (boundBreakpoint);

              m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
            }

            if (errorBreakpoint != null)
            {
              m_errorBreakpoints.Add (errorBreakpoint);

              m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
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
      // Iterate through all registered 'bound' breakpoints, and respond to discrepancies between live and internally cached data.
      // 

      LoggingUtils.PrintFunction ();

      foreach (IDebugBoundBreakpoint2 boundBreakpoint in m_boundBreakpoints)
      {
        if (boundBreakpoint is CLangDebuggeeBreakpointBound)
        {
          CLangDebuggeeBreakpointBound breakpoint = boundBreakpoint as CLangDebuggeeBreakpointBound;

          m_debugger.GdbClient.SendAsyncCommand (string.Format ("-break-info {0}", breakpoint.GdbBreakpoint.ID), delegate (MiResultRecord resultRecord)
          {
            if ((resultRecord == null) || ((resultRecord != null) && resultRecord.IsError ()))
            {
              throw new InvalidOperationException ();
            }

            string addr = resultRecord ["BreakpointTable"] ["body"] [0] ["addr"].GetString ();

            if (!string.IsNullOrEmpty (addr))
            {
              bool pending = addr.Equals ("<PENDING>");

              bool multiple = addr.Equals ("<MULTIPLE>");

              m_errorBreakpoints.Clear ();

              if (pending && (!breakpoint.GdbBreakpoint.IsPending ()))
              {
                // 
                // Address can't be satistfied. Unsatisified likely indicates the modules or symbols associated with the context aren't loaded, yet.
                // 

                IDebugBreakpointResolution2 resolution;

                LoggingUtils.RequireOk (breakpoint.GetBreakpointResolution (out resolution));

                breakpoint.GdbBreakpoint.Address = MiBreakpoint.Pending;

                (resolution as DebuggeeBreakpointResolution).CodeContext.Address = new DebuggeeAddress (breakpoint.GdbBreakpoint.Address);

                DebuggeeBreakpointError errorBreakpoint = new DebuggeeBreakpointError (m_breakpointManager, this, (resolution as DebuggeeBreakpointResolution).CodeContext, "Bound breakpoint waiting on additional library symbols");

                m_errorBreakpoints.Add (errorBreakpoint);

                m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointError (errorBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
              }
              else if (multiple && (breakpoint.GdbBreakpoint.IsMultiple ()))
              {
                // 
                // Breakpoint satisfied to multiple locations, no single memory address available.
                // 

                IDebugBreakpointResolution2 resolution;

                LoggingUtils.RequireOk (breakpoint.GetBreakpointResolution (out resolution));

                breakpoint.GdbBreakpoint.Address = MiBreakpoint.Multiple;

                (resolution as DebuggeeBreakpointResolution).CodeContext.Address = new DebuggeeAddress (breakpoint.GdbBreakpoint.Address);

                m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
              }
              else if (!pending && !multiple)
              {
                // 
                // Address satistfied, and the breakpoint is legitimately bound.
                // 

                DebuggeeAddress boundAddress = new DebuggeeAddress (addr);

                if (breakpoint.GdbBreakpoint.Address != boundAddress.MemoryAddress)
                {
                  breakpoint.GdbBreakpoint.Address = boundAddress.MemoryAddress;

                  IDebugBreakpointResolution2 resolution;

                  LoggingUtils.RequireOk (breakpoint.GetBreakpointResolution (out resolution));

                  (resolution as DebuggeeBreakpointResolution).CodeContext.Address = boundAddress;

                  m_debugger.Engine.Broadcast (new DebugEngineEvent.BreakpointBound (this, boundBreakpoint), m_debugger.NativeProgram.DebugProgram, m_debugger.NativeProgram.GetThread (m_debugger.NativeProgram.CurrentThreadId));
                }
              }
            }
          });
        }
        else
        {
          throw new NotImplementedException ();
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
