﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;
using System.Globalization;

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

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeDisassemblyStream (CLangDebugger debugger, enum_DISASSEMBLY_STREAM_SCOPE streamScope, IDebugCodeContext2 codeContext)
    {
      m_debugger = debugger;

      m_streamScope = streamScope;

      m_codeContext = codeContext as DebuggeeCodeContext;
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
        string location = string.Format ("0x{0:X8}", uCodeLocationId);

        ppCodeContext = CLangDebuggeeCodeContext.GetCodeContextForLocation (m_debugger, location);

        if (ppCodeContext == null)
        {
          throw new InvalidOperationException ();
        }

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppCodeContext = null;

        return Constants.E_FAIL;
      }
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

      try
      {
        CONTEXT_INFO [] contextInfoArray = new CONTEXT_INFO [1];

        LoggingUtils.RequireOk (pCodeContext.GetInfo (enum_CONTEXT_INFO_FIELDS.CIF_ADDRESSABSOLUTE, contextInfoArray));

        if (contextInfoArray [0].bstrAddressAbsolute.StartsWith ("0x"))
        {
          puCodeLocationId = ulong.Parse (contextInfoArray [0].bstrAddressAbsolute.Substring (2), NumberStyles.HexNumber);
        }
        else
        {
          puCodeLocationId = ulong.Parse (contextInfoArray [0].bstrAddressAbsolute, NumberStyles.HexNumber);
        }

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        puCodeLocationId = 0ul;

        return Constants.E_FAIL;
      }
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

      try
      {
        LoggingUtils.RequireOk (GetCodeLocationId (m_codeContext, out puCodeLocationId));

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        puCodeLocationId = 0ul;

        return Constants.E_FAIL;
      }
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

        return Constants.E_NOTIMPL;
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

      return Constants.S_OK;
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

      pnSize = 1024; // TODO: this seems a reasonable amount, right now.

      return Constants.S_OK;
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
        ulong startAddress = m_codeContext.Address.MemoryAddress;

        ulong endAddress = startAddress + ((ulong)(dwInstructions) * 4); // TODO: 4 for a 32-bit instruction set?

        string disassemblyCommand = string.Format ("-data-disassemble -s 0x{0:X8} -e 0x{1:X8} -- 1", startAddress, endAddress);

        MiResultRecord resultRecord = m_debugger.GdbClient.SendSyncCommand (disassemblyCommand);

        MiResultRecord.RequireOk (resultRecord, disassemblyCommand);

        if (!resultRecord.HasField ("asm_insns"))
        {
          throw new InvalidOperationException ("-data-disassemble result missing 'asm_insns' field");
        }

        MiResultValueList assemblyRecords = (MiResultValueList) resultRecord ["asm_insns"] [0];

        long maxInstructions = Math.Min (assemblyRecords.Values.Count, dwInstructions);

        if (maxInstructions == 0)
        {
          throw new InvalidOperationException ();
        }

        int currentInstruction = 0;

        for (int i = 0; i < assemblyRecords.Values.Count; ++i)
        {
          MiResultValue recordValue = assemblyRecords [i];

          if (recordValue.Variable.Equals ("src_and_asm_line"))
          {
            // 
            // Parse mixed-mode disassembly reports.
            // 

            uint line = recordValue ["line"] [0].GetUnsignedInt ();

            string file = recordValue ["file"] [0].GetString ();

            MiResultValueList lineAsmInstructionValues = (MiResultValueList) recordValue ["line_asm_insn"] [0];

            foreach (MiResultValue instructionValue in lineAsmInstructionValues.Values)
            {
              string address = instructionValue ["address"] [0].GetString ();

              if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESS) != 0)
              {
                prgDisassembly [currentInstruction].bstrAddress = address;

                prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESS;
              }

              if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESSOFFSET) != 0)
              {
                string offset = instructionValue ["offset"] [0].GetString ();

                prgDisassembly [currentInstruction].bstrAddressOffset = offset;

                prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESSOFFSET;
              }

              if (((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPCODE) != 0) || ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPERANDS) != 0) || ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPERANDS_SYMBOLS) != 0))
              {
                string inst = instructionValue ["inst"] [0].GetString ();

                var operations = inst.Split (new string [] { "\\t" }, StringSplitOptions.None);

                if (operations.Length > 0)
                {
                  prgDisassembly [currentInstruction].bstrOpcode = operations [0];

                  prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPCODE;
                }

                if (operations.Length > 1)
                {
                  prgDisassembly [currentInstruction].bstrOperands = operations [1];

                  prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPERANDS;
                }

                if (operations.Length > 2)
                {
                  prgDisassembly [currentInstruction].bstrOperands += " " + operations [2];

                  prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPERANDS_SYMBOLS;
                }
              }

              if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_SYMBOL) != 0)
              {
                string functionName = instructionValue ["func-name"] [0].GetString ();

                prgDisassembly [currentInstruction].bstrSymbol = functionName;

                prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_SYMBOL;
              }

              if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODELOCATIONID) != 0)
              {
                DebuggeeAddress instructionAddress = new DebuggeeAddress (address);

                prgDisassembly [currentInstruction].uCodeLocationId = instructionAddress.MemoryAddress;

                prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODELOCATIONID;
              }

              if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_POSITION) != 0)
              {
                prgDisassembly [currentInstruction].posBeg.dwLine = line;

                prgDisassembly [currentInstruction].posBeg.dwColumn = 0;

                prgDisassembly [currentInstruction].posEnd.dwLine = line;

                prgDisassembly [currentInstruction].posEnd.dwColumn = uint.MaxValue;

                prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_POSITION;
              }

              if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_DOCUMENTURL) != 0)
              {
                prgDisassembly [currentInstruction].bstrDocumentUrl = "file://" + file;

                prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_DOCUMENTURL;
              }

              if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_BYTEOFFSET) != 0)
              {
                uint offset = instructionValue ["offset"] [0].GetUnsignedInt ();

                prgDisassembly [currentInstruction].dwByteOffset = offset;

                prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_BYTEOFFSET;
              }

              if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_FLAGS) != 0)
              {
                prgDisassembly [currentInstruction].dwFlags |= enum_DISASSEMBLY_FLAGS.DF_HASSOURCE;

                prgDisassembly [currentInstruction].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_FLAGS;
              }

              if (++currentInstruction >= maxInstructions)
              {
                break;
              }
            }
          }
        }

        pdwInstructionsRead = (uint) currentInstruction;

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        pdwInstructionsRead = 0;

        return Constants.E_FAIL;
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
        switch (dwSeekStart)
        {
          case enum_SEEK_START.SEEK_START_BEGIN:
          {
            // 
            // Starts seeking at the beginning of the current document.
            // 

            throw new NotImplementedException ();
          }

          case enum_SEEK_START.SEEK_START_END:
          {
            // 
            // Starts seeking at the end of the current document.
            // 

            throw new NotImplementedException ();
          }

          case enum_SEEK_START.SEEK_START_CURRENT:
          {
            // 
            // Starts seeking at the current position of the current document.
            // 

            ulong offsetAddress = m_codeContext.Address.MemoryAddress;

            if (iInstructions > 0)
            {
              offsetAddress += (ulong)(iInstructions * 4);
            }
            else if (iInstructions < 0)
            {
              offsetAddress += (ulong)(Math.Abs (iInstructions) * 4);
            }

            m_codeContext.Address = new DebuggeeAddress (offsetAddress);

            break;
          }

          case enum_SEEK_START.SEEK_START_CODECONTEXT:
          {
            // 
            // Starts seeking at the given code context of the current document.
            // 

            DebuggeeCodeContext codeContext = (pCodeContext as DebuggeeCodeContext);

            ulong offsetAddress = codeContext.Address.MemoryAddress;

            if (iInstructions > 0)
            {
              offsetAddress += (ulong)(iInstructions * 4);
            }
            else if (iInstructions < 0)
            {
              offsetAddress += (ulong)(Math.Abs (iInstructions) * 4);
            }

            m_codeContext.Address = new DebuggeeAddress (offsetAddress);

            break;
          }

          case enum_SEEK_START.SEEK_START_CODELOCID:
          {
            // 
            // Starts seeking at the given code location identifier. 
            // 

            ulong offsetAddress = uCodeLocationId;

            if (iInstructions > 0)
            {
              offsetAddress += (ulong)(iInstructions * 4);
            }
            else if (iInstructions < 0)
            {
              offsetAddress += (ulong)(Math.Abs (iInstructions) * 4);
            }

            m_codeContext.Address = new DebuggeeAddress (offsetAddress);

            break;
          }
        }

        return Constants.S_OK;
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
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

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
