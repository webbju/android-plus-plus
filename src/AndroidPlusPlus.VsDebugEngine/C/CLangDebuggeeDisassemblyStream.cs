////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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

  public class CLangDebuggeeDisassemblyStream : IDebugDisassemblyStream2
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly CLangDebugger m_debugger;

    private readonly enum_DISASSEMBLY_STREAM_SCOPE m_streamScope;

    private readonly DebuggeeCodeContext m_codeContext;

    private DebuggeeAddress m_currentPosition;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeDisassemblyStream (CLangDebugger debugger, enum_DISASSEMBLY_STREAM_SCOPE streamScope, IDebugCodeContext2 codeContext)
    {
      m_debugger = debugger;

      m_streamScope = streamScope;

      m_codeContext = codeContext as DebuggeeCodeContext;

      m_currentPosition = m_codeContext.Address;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetCodeContext (ulong uCodeLocationId, out IDebugCodeContext2 ppCodeContext)
    {
      // 
      // Returns a code context object corresponding to a specified code location identifier.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        IDebugDocumentContext2 documentContext;

        LoggingUtils.RequireOk (m_codeContext.GetDocumentContext (out documentContext));

        ppCodeContext = new DebuggeeCodeContext (m_debugger.Engine, documentContext as DebuggeeDocumentContext, new DebuggeeAddress (uCodeLocationId));
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppCodeContext = null;

        return DebugEngineConstants.E_FAIL;
      }

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetCodeLocationId (IDebugCodeContext2 pCodeContext, out ulong puCodeLocationId)
    {
      // 
      // Returns a code location identifier for a particular code context.
      // 

      LoggingUtils.PrintFunction ();

      puCodeLocationId = (pCodeContext as DebuggeeCodeContext).Address.MemoryAddress;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetCurrentLocation (out ulong puCodeLocationId)
    {
      // 
      // Returns a code location identifier that represents the current code location.
      // 

      LoggingUtils.PrintFunction ();

      puCodeLocationId = m_currentPosition.MemoryAddress;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetDocument (string bstrDocumentUrl, out IDebugDocument2 ppDocument)
    {
      // 
      // Gets the source document associated with this disassembly stream.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        ppDocument = null;

        return DebugEngineConstants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetScope (enum_DISASSEMBLY_STREAM_SCOPE [] pdwScope)
    {
      // 
      // Gets the scope of this disassembly stream.
      // 

      LoggingUtils.PrintFunction ();

      pdwScope [0] = m_streamScope;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetSize (out ulong pnSize)
    {
      // 
      // Gets the size of this disassembly stream. (The maximum number of instructions that are available in the current scope).
      // 

      LoggingUtils.PrintFunction ();

      pnSize = ulong.MaxValue;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Read (uint dwInstructions, enum_DISASSEMBLY_STREAM_FIELDS dwFields, out uint pdwInstructionsRead, DisassemblyData [] prgDisassembly)
    {
      // 
      // Reads instructions starting from the current position in the disassembly stream.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        DebuggeeCodeContext addressCodeContext = m_debugger.GetCodeContextForLocation (m_currentPosition.ToString ());

        if (addressCodeContext == null)
        {
          throw new InvalidOperationException ();
        }

        DebuggeeAddress endAddress = m_currentPosition.Add ((ulong)(dwInstructions));

        string disassemblyCommand = string.Format ("-data-disassemble -s {0} -e {1} -- 0", m_currentPosition.ToString (), endAddress.ToString ());

        MiResultRecord resultRecord = m_debugger.GdbClient.SendCommand (disassemblyCommand);

        if ((resultRecord == null) || ((resultRecord != null) && resultRecord.IsError ()))
        {
          throw new InvalidOperationException ();
        }

        MiResultValue assemblyInstructions = resultRecord ["asm_insns"];

        for (int i = 0; i < Math.Min (assemblyInstructions.Count, dwInstructions); ++i)
        {
          MiResultValue instruction = assemblyInstructions [i];

          DebuggeeAddress instructionAddress = new DebuggeeAddress (instruction ["address"].GetString ());

          if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESS) != 0)
          {
            prgDisassembly [i].bstrAddress = instructionAddress.ToString ();

            prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESS;
          }

          if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESSOFFSET) != 0)
          {
          }

          if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODEBYTES) != 0)
          {
          }

          if (instruction.HasField ("inst"))
          {
            string [] operation = instruction ["inst"].GetString ().Split (new string [] { "\\t"}, StringSplitOptions.None);

            if (instruction.HasField ("inst") && (dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPCODE) != 0)
            {
              prgDisassembly [i].bstrOpcode = operation [0];

              prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPCODE;
            }

            if ((operation.Length > 1) && (dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPERANDS) != 0)
            {
              StringBuilder operandsBuilder = new StringBuilder ();

              for (int o = 1; o < operation.Length; ++o)
              {
                operandsBuilder.Append (operation [o] + " ");
              }

              operandsBuilder.Append ("<*symbol*>");

              prgDisassembly [i].bstrOperands = operandsBuilder.ToString ();

              prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPERANDS;

              prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPERANDS_SYMBOLS;
            }
          }

          if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_SYMBOL) != 0)
          {
            prgDisassembly [i].bstrSymbol = "*symbol*";

            prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_SYMBOL;
          }

          if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODELOCATIONID) != 0)
          {
            prgDisassembly [i].uCodeLocationId = instructionAddress.MemoryAddress;

            prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODELOCATIONID;
          }

          if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_POSITION) != 0)
          {
            TEXT_POSITION [] startPos = new TEXT_POSITION [1];

            TEXT_POSITION [] endPos = new TEXT_POSITION [1];

            LoggingUtils.RequireOk (addressCodeContext.DocumentContext.GetStatementRange (startPos, endPos));

            prgDisassembly [i].posBeg = startPos [0];

            prgDisassembly [i].posEnd = endPos [0];

            prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_POSITION;
          }

          if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_DOCUMENTURL) != 0)
          {
            prgDisassembly [i].bstrDocumentUrl = string.Empty;

            LoggingUtils.RequireOk (addressCodeContext.DocumentContext.GetName (enum_GETNAME_TYPE.GN_URL, out prgDisassembly [i].bstrDocumentUrl));

            prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_DOCUMENTURL;
          }

          if (instruction.HasField ("offset") && (dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_BYTEOFFSET) != 0)
          {
            prgDisassembly [i].dwByteOffset = instruction ["offset"].GetUnsignedInt ();

            prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_BYTEOFFSET;
          }

          if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_FLAGS) != 0)
          {
            if (!string.IsNullOrEmpty (prgDisassembly [i].bstrDocumentUrl))
            {
              prgDisassembly [i].dwFlags |= enum_DISASSEMBLY_FLAGS.DF_HASSOURCE;
            }

            prgDisassembly [i].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_FLAGS;
          }
        }

        pdwInstructionsRead = (uint)assemblyInstructions.Count;

        return DebugEngineConstants.S_OK;
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        pdwInstructionsRead = 0;

        return DebugEngineConstants.E_NOTIMPL;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        pdwInstructionsRead = 0;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Seek (enum_SEEK_START dwSeekStart, IDebugCodeContext2 pCodeContext, ulong uCodeLocationId, long iInstructions)
    {
      // 
      // Moves the read pointer in the disassembly stream a given number of instructions relative to a specified position.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        /*switch (dwSeekStart)
        {
          case enum_SEEK_START.SEEK_START_BEGIN:
          case enum_SEEK_START.SEEK_START_END:
          {
            throw new NotImplementedException ();
          }

          case enum_SEEK_START.SEEK_START_CURRENT:
          {
            if (iInstructions > 0)
            {
              m_currentPosition = m_currentPosition.Add ((ulong)(iInstructions * 4));
            }
            else if (iInstructions < 0)
            {
              m_currentPosition = m_currentPosition.Subtract ((ulong)(Math.Abs (iInstructions) * 4));
            }

            break;
          }

          case enum_SEEK_START.SEEK_START_CODECONTEXT:
          {
            DebuggeeCodeContext codeContext = (pCodeContext as DebuggeeCodeContext);

            if (iInstructions > 0)
            {
              m_currentPosition = codeContext.Address.Add ((ulong)(iInstructions * 4));
            }
            else if (iInstructions < 0)
            {
              m_currentPosition = codeContext.Address.Subtract ((ulong)(Math.Abs (iInstructions) * 4));
            }

            break;
          }

          case enum_SEEK_START.SEEK_START_CODELOCID:
          {
            DebuggeeAddress codeLocAddress = new DebuggeeAddress (uCodeLocationId);

            if (iInstructions > 0)
            {
              m_currentPosition = codeLocAddress.Add ((ulong)(iInstructions * 4));
            }
            else if (iInstructions < 0)
            {
              m_currentPosition = codeLocAddress.Subtract ((ulong)(Math.Abs (iInstructions) * 4));
            }

            break;
          }
        }*/

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

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
