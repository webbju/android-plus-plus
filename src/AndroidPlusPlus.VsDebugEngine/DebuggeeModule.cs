﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

  // 
  // This class represents a module loaded in the debuggee process to the debugger. 
  // 

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebuggeeModule : IDebugModule3
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class Enumerator : DebugEnumerator<IDebugModule2, IEnumDebugModules2>, IEnumDebugModules2
    {
      public Enumerator (ICollection<IDebugModule2> modules)
        : base (modules)
      {
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly DebugEngine m_debugEngine;

    private bool m_userCode;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeModule (DebugEngine engine)
    {
      m_debugEngine = engine;

      m_userCode = true;

      Name = string.Empty;

      Version = string.Empty;

      Size = 0;

      RemotePath = string.Empty;

      RemoteLoadAddress = 0;

      SymbolsPath = string.Empty;

      SymbolsLoaded = false;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string Name { get; protected set; }

    public string Version { get; protected set; }

    public uint Size { get; protected set; }

    public string RemotePath { get; protected set; }

    public uint RemoteLoadAddress { get; protected set; }

    public string SymbolsPath { get; protected set; }

    public bool SymbolsLoaded { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugModule2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int GetInfo (enum_MODULE_INFO_FIELDS requestedFields, MODULE_INFO [] infoArray)
    {
      // 
      // Retrieve relevant requested data for this module.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        infoArray [0] = new MODULE_INFO ();

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_NAME) != 0)
        {
          infoArray [0].m_bstrName = Name;

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_NAME;
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_URL) != 0)
        {
          infoArray [0].m_bstrUrl = RemotePath;

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_URL;
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_VERSION) != 0)
        {
          infoArray [0].m_bstrVersion = Version;

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_VERSION;
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_DEBUGMESSAGE) != 0)
        {
          infoArray [0].m_bstrDebugMessage = RemotePath;

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_DEBUGMESSAGE;
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_LOADADDRESS) != 0)
        {
          infoArray [0].m_addrLoadAddress = RemoteLoadAddress;

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_LOADADDRESS;
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_PREFFEREDADDRESS) != 0)
        {
          // Assume the module loaded where it was suppose to.

          infoArray [0].m_addrPreferredLoadAddress = RemoteLoadAddress;

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_PREFFEREDADDRESS;
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_SIZE) != 0)
        {
          infoArray [0].m_dwSize = Size;

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_SIZE;
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_LOADORDER) != 0)
        {
          infoArray [0].m_dwLoadOrder = 0;

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_LOADORDER;
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_TIMESTAMP) != 0)
        {
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_URLSYMBOLLOCATION) != 0)
        {
          infoArray [0].m_bstrUrlSymbolLocation = "file://" + SymbolsPath;

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_URLSYMBOLLOCATION;
        }

        if ((requestedFields & enum_MODULE_INFO_FIELDS.MIF_FLAGS) != 0)
        {
          infoArray [0].m_dwModuleFlags = enum_MODULE_FLAGS.MODULE_FLAG_NONE;

          if (SymbolsLoaded)
          {
            infoArray [0].m_dwModuleFlags |= enum_MODULE_FLAGS.MODULE_FLAG_SYMBOLS;
          }

          infoArray [0].dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_FLAGS;
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

    [Obsolete("These methods are not called by the Visual Studio debugger.")]
    public virtual int ReloadSymbols_Deprecated (string urlToSymbols, out string debugMessage)
    {
      LoggingUtils.PrintFunction ();

      debugMessage = string.Empty;

      return Constants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugModule3 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int GetSymbolInfo (enum_SYMBOL_SEARCH_INFO_FIELDS requestedFields, MODULE_SYMBOL_SEARCH_INFO [] infoArray)
    {
      // 
      // Returns a list of paths searched for symbols, and the results of searching path.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        if ((requestedFields & enum_SYMBOL_SEARCH_INFO_FIELDS.SSIF_VERBOSE_SEARCH_INFO) != 0)
        {
          if (SymbolsLoaded)
          {
            infoArray [0].bstrVerboseSearchInfo = string.Format ("Symbols loaded for {0} from {1}", Name, SymbolsPath);

            infoArray [0].dwValidFields = (uint)enum_SYMBOL_SEARCH_INFO_FIELDS.SSIF_VERBOSE_SEARCH_INFO;
          }
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

    public virtual int LoadSymbols ()
    {
      // 
      // Loads and initialises symbols for the current module when explicitly requested by the user.
      // This is not yet supported.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int IsUserCode (out int isUserCode)
    {
      // 
      // Used to support 'JustMyCode' feature.
      // This distinquishment is not made and thus all modules should be considered "My Code".
      // 

      LoggingUtils.PrintFunction ();

      isUserCode = (m_userCode) ? 1 : 0;

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int SetJustMyCodeState (int isUserCode)
    {
      // 
      // Used to support 'JustMyCode' feature.
      // This distinquishment is not supported.
      // 

      LoggingUtils.PrintFunction ();

      m_userCode = (isUserCode == 1);

      return Constants.S_OK;
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
