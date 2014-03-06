////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

  public class DebuggeeBreakpointPending : IDebugPendingBreakpoint2
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public sealed class Enumerator : DebugEnumerator<IDebugPendingBreakpoint2, IEnumDebugPendingBreakpoints2>, IEnumDebugPendingBreakpoints2
    {
      public Enumerator (List<IDebugPendingBreakpoint2> breakpoints)
        : base (breakpoints)
      {
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected readonly DebugBreakpointManager m_breakpointManager;

    protected readonly IDebugBreakpointRequest2 m_breakpointRequest;

    protected readonly BP_REQUEST_INFO m_breakpointRequestInfo;

    protected List<IDebugBoundBreakpoint2> m_boundBreakpoints;

    protected List<IDebugErrorBreakpoint2> m_errorBreakpoints;

    protected bool m_breakpointEnabled;

    protected bool m_breakpointDeleted;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeBreakpointPending (DebugBreakpointManager breakpointManager, IDebugBreakpointRequest2 breakpointRequest)
    {
      m_breakpointManager = breakpointManager;

      m_breakpointRequest = breakpointRequest;

      BP_REQUEST_INFO [] requestInfo = new BP_REQUEST_INFO [1];

      LoggingUtils.RequireOk (m_breakpointRequest.GetRequestInfo (enum_BPREQI_FIELDS.BPREQI_ALLFIELDS, requestInfo));

      m_breakpointRequestInfo = requestInfo [0];

      m_boundBreakpoints = new List<IDebugBoundBreakpoint2> ();

      m_errorBreakpoints = new List<IDebugErrorBreakpoint2> ();

      m_breakpointEnabled = true;

      m_breakpointDeleted = false;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ClearBoundBreakpoints ()
    {
      // 
      // Remove all of the bound breakpoints for this pending breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        lock (m_boundBreakpoints)
        {
          for (int i = m_boundBreakpoints.Count - 1; i >= 0; --i)
          {
            m_boundBreakpoints [i].Delete ();
          }

          m_boundBreakpoints.Clear ();
        }
      }
      catch (Exception)
      {
        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int EvaluateBreakpointLocation (out DebuggeeDocumentContext documentContext, out DebuggeeCodeContext codeContext, out string location)
    {
      LoggingUtils.PrintFunction ();

      documentContext = null;

      codeContext = null;

      location = string.Empty;

      try
      {
        switch (m_breakpointRequestInfo.bpLocation.bpLocationType)
        {
          case (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE:
          {
            // 
            // Specifies the location type of the breakpoint as a line of source code.
            // 

            string fileName;

            IDebugDocumentPosition2 documentPostion = (IDebugDocumentPosition2)Marshal.GetObjectForIUnknown (m_breakpointRequestInfo.bpLocation.unionmember2);

            LoggingUtils.RequireOk (documentPostion.GetFileName (out fileName));

            bool fileInCurrentProject = true; // TODO

            if (File.Exists (fileName) && fileInCurrentProject)
            {
              TEXT_POSITION [] startPos = new TEXT_POSITION [1];

              TEXT_POSITION [] endPos = new TEXT_POSITION [1];

              LoggingUtils.RequireOk (documentPostion.GetRange (startPos, endPos));

              documentContext = new DebuggeeDocumentContext (m_breakpointManager.Engine, fileName, startPos [0], endPos [0], DebugEngineGuids.guidLanguageCpp, null);

              location = string.Format ("\"{0}:{1}\"", fileName.Replace ('\\', '/'), startPos [0].dwLine + 1);
            }
            else
            {
              throw new NotImplementedException ();
            }

            break;
          }

          case (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET:
          {
            // 
            // Specifies the location type of the breakpoint as a code function offset.
            // 

            string function = string.Empty;

            IDebugFunctionPosition2 functionPosition = (IDebugFunctionPosition2)Marshal.GetObjectForIUnknown (m_breakpointRequestInfo.bpLocation.unionmember2);

            TEXT_POSITION [] textPos = new TEXT_POSITION [1];

            LoggingUtils.RequireOk (functionPosition.GetFunctionName (out function));

            LoggingUtils.RequireOk (functionPosition.GetOffset (textPos));

            if (!string.IsNullOrEmpty (function))
            {
              location = function;
            }

            break;
          }

          case (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_CONTEXT:
          {
            // 
            // Specifies the location type of the breakpoint as a code context.
            // 

            codeContext = ((IDebugCodeContext2)Marshal.GetObjectForIUnknown (m_breakpointRequestInfo.bpLocation.unionmember1)) as DebuggeeCodeContext;

            if (codeContext != null)
            {
              location = codeContext.Address.ToString ();
            }

            break;
          }

          case (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_STRING:
          {
            // 
            // Specifies the location type of the breakpoint as a code string.
            // 

            throw new NotImplementedException ();
          }

          case (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_ADDRESS:
          {
            // 
            // Specifies the location type of the breakpoint as a code address.
            // 

            string address = Marshal.PtrToStringBSTR (m_breakpointRequestInfo.bpLocation.unionmember4);

            if (!string.IsNullOrEmpty (address))
            {
              location = address;
            }

            break;
          }

          case (uint)enum_BP_LOCATION_TYPE.BPLT_DATA_STRING:
          {
            // 
            // Specifies the location type of the breakpoint as a data string.
            // 

            string dataExpression = Marshal.PtrToStringBSTR (m_breakpointRequestInfo.bpLocation.unionmember3);

            if (!string.IsNullOrEmpty (dataExpression))
            {
              location = dataExpression;
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
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_NOTIMPL;
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

    public virtual int CreateBoundBreakpoint (string location, DebuggeeDocumentContext documentContext, DebuggeeCodeContext codeContext)
    {
      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void RefreshBoundBreakpoints ()
    {
      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugPendingBreakpoint2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int Bind ()
    {
      // 
      // Binds this pending breakpoint to one or more code locations.
      // 

      LoggingUtils.PrintFunction ();

      DebuggeeDocumentContext documentContext = null;

      DebuggeeCodeContext codeContext = null;

      string bindLocation = string.Empty;

      try
      {
        lock (m_errorBreakpoints)
        {
          m_errorBreakpoints.Clear ();
        }

        if (m_breakpointDeleted)
        {
          return DebugEngineConstants.E_BP_DELETED;
        }

        LoggingUtils.RequireOk (EvaluateBreakpointLocation (out documentContext, out codeContext, out bindLocation));

        LoggingUtils.RequireOk (CreateBoundBreakpoint (bindLocation, documentContext, codeContext));

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

    public int CanBind (out IEnumDebugErrorBreakpoints2 ppErrorEnum)
    {
      // 
      // Determines whether this pending breakpoint can bind to a code location.
      // 

      LoggingUtils.PrintFunction ();

      ppErrorEnum = null;

      try
      {
        if (m_breakpointDeleted)
        {
          return DebugEngineConstants.E_BP_DELETED;
        }

        if (m_errorBreakpoints.Count > 0)
        {
          LoggingUtils.RequireOk (EnumErrorBreakpoints (enum_BP_ERROR_TYPE.BPET_ALL, out ppErrorEnum));

          return DebugEngineConstants.S_FALSE;
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

    public int Delete ()
    {
      // 
      // Deletes this pending breakpoint and all breakpoints bound from it.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        if (m_breakpointDeleted)
        {
          return DebugEngineConstants.E_BP_DELETED;
        }

        ClearBoundBreakpoints ();

        m_breakpointDeleted = true;

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

    public int Enable (int fEnable)
    {
      // 
      // Toggles the enabled state of this pending breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        m_breakpointEnabled = (fEnable != 0);

        if (m_breakpointDeleted)
        {
          return DebugEngineConstants.E_BP_DELETED;
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

    public int EnumBoundBreakpoints (out IEnumDebugBoundBreakpoints2 ppEnum)
    {
      // 
      // Enumerates all breakpoints bound from this pending breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        ppEnum = new DebuggeeBreakpointBound.Enumerator (m_boundBreakpoints);

        if (m_boundBreakpoints.Count > 0)
        {
          return DebugEngineConstants.S_FALSE;
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppEnum = null;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EnumErrorBreakpoints (enum_BP_ERROR_TYPE bpErrorType, out IEnumDebugErrorBreakpoints2 ppEnum)
    {
      // 
      // Enumerates all error breakpoints that resulted from this pending breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        ppEnum = new DebuggeeBreakpointError.Enumerator (m_errorBreakpoints);

        if (m_breakpointDeleted)
        {
          return DebugEngineConstants.E_BP_DELETED;
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppEnum = null;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetBreakpointRequest (out IDebugBreakpointRequest2 ppBPRequest)
    {
      // 
      // Gets the breakpoint request that was used to create this pending breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        ppBPRequest = m_breakpointRequest;

        if (m_breakpointDeleted)
        {
          return DebugEngineConstants.E_BP_DELETED;
        }

        if (ppBPRequest == null)
        {
          throw new InvalidOperationException ();
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppBPRequest = null;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetState (PENDING_BP_STATE_INFO [] pState)
    {
      // 
      // Gets the state of this pending breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        pState [0].state = enum_PENDING_BP_STATE.PBPS_NONE;

        if (m_breakpointDeleted)
        {
          pState [0].state = enum_PENDING_BP_STATE.PBPS_DELETED;
        }
        else
        {
          pState [0].state = (m_breakpointEnabled) ? enum_PENDING_BP_STATE.PBPS_ENABLED : enum_PENDING_BP_STATE.PBPS_DISABLED;
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

    public int SetCondition (BP_CONDITION bpCondition)
    {
      // 
      // Sets or changes the condition associated with this pending breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        lock (m_boundBreakpoints)
        {
          foreach (DebuggeeBreakpointBound boundBreakpoint in m_boundBreakpoints)
          {
            LoggingUtils.RequireOk (boundBreakpoint.SetCondition (bpCondition));
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

    public int SetPassCount (BP_PASSCOUNT bpPassCount)
    {
      // 
      // Sets or changes the pass count associated with this pending breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        if (m_breakpointDeleted)
        {
          return DebugEngineConstants.E_BP_DELETED;
        }

        lock (m_boundBreakpoints)
        {
          foreach (DebuggeeBreakpointBound boundBreakpoint in m_boundBreakpoints)
          {
            LoggingUtils.RequireOk (boundBreakpoint.SetPassCount (bpPassCount));
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

    public int Virtualize (int fVirtualize)
    {
      // 
      // Toggles the virtualized state of this pending breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      if (m_breakpointDeleted)
      {
        return DebugEngineConstants.E_BP_DELETED;
      }

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

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
