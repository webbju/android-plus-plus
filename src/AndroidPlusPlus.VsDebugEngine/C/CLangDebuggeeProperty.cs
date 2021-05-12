////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;
using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Text.RegularExpressions;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class CLangDebuggeeProperty : DebuggeeProperty
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly CLangDebugger m_debugger;

    private MiVariable m_gdbVariable;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeProperty (CLangDebugger debugger, CLangDebuggeeStackFrame stackFrame, string expression, string value)
      : base (debugger.Engine, stackFrame, expression, value)
    {
      m_debugger = debugger;

      m_gdbVariable = null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeProperty (CLangDebugger debugger, CLangDebuggeeStackFrame stackFrame, MiVariable gdbVariable)
      : base (debugger.Engine, stackFrame, gdbVariable.Expression, string.Empty)
    {
      m_debugger = debugger;

      m_gdbVariable = gdbVariable;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeProperty (CLangDebuggeeProperty parent, MiVariable gdbVariable)
      : this (parent.m_debugger, parent.m_stackFrame as CLangDebuggeeStackFrame, gdbVariable)
    {
      m_parent = parent;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public MiVariable GdbVariable
    {
      get
      {
        return m_gdbVariable;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugProperty2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override int EnumChildren (enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, ref Guid guidFilter, enum_DBG_ATTRIB_FLAGS dwAttribFilter, string pszNameFilter, uint dwTimeout, out IEnumDebugPropertyInfo2 ppEnum)
    {
      //
      // Enumerates the children of a property. This provides support for dereferencing pointers, displaying members of an array, or fields of a class or struct.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        if (m_gdbVariable != null)
        {
          if (m_gdbVariable.HasChildren && (m_gdbVariable.Children.Count == 0))
          {
            m_debugger.VariableManager.CreateChildVariables (m_gdbVariable, 1);
          }

          if (m_gdbVariable.Children.Count != m_children.Count)
          {
            // TODO: Dispose the properties as appropriate.

            m_children.Clear ();

            foreach (MiVariable childVariable in m_gdbVariable.Children.Values)
            {
              if (childVariable.IsPseudoChild)
              {
                CLangDebuggeeProperty pseudoChildProperty = m_debugger.VariableManager.CreatePropertyFromVariable (m_stackFrame as CLangDebuggeeStackFrame, childVariable) ?? throw new InvalidOperationException ("Failed to create child property.");

                CLangDebuggeeProperty [] childSubProperties = m_debugger.VariableManager.GetChildProperties (m_stackFrame as CLangDebuggeeStackFrame, pseudoChildProperty);

                m_children.AddRange (childSubProperties);
              }
              else
              {
                CLangDebuggeeProperty childProperty = m_debugger.VariableManager.CreatePropertyFromVariable (m_stackFrame as CLangDebuggeeStackFrame, childVariable) ?? throw new InvalidOperationException ("Failed to create child property.");

                m_children.Add (childProperty);
              }
            }
          }
        }

        LoggingUtils.RequireOk (base.EnumChildren (dwFields, dwRadix, ref guidFilter, dwAttribFilter, pszNameFilter, dwTimeout, out ppEnum));

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppEnum = null;

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override int GetMemoryBytes (out IDebugMemoryBytes2 memoryBytes)
    {
      //
      // Returns the memory bytes for a property value.
      //

      LoggingUtils.PrintFunction ();

      return m_debugger.NativeProgram.GetMemoryBytes (out memoryBytes);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override int GetMemoryContext (out IDebugMemoryContext2 memoryContext)
    {
      //
      // Returns the memory context for a property value.
      //

      LoggingUtils.PrintFunction ();

      memoryContext = null;

      try
      {
        //
        // Pick out a memory address from a pool of potential strings.
        //
        //   This is mainly to support GDB reporting symbols like:
        //
        //   {void (JNIEnv *, jclass)} 0xb3e66c84 <Java_com_example_hellogdbserver_HelloGdbServer_invokeCrash>
        //

        string [] expressionPool = { m_expression, m_gdbVariable.Value };

        string pattern = "(?<address>0x[A-Za-z0-9]+)";

        Regex regExMatcher = new Regex (pattern, RegexOptions.IgnoreCase);

        foreach (string expression in expressionPool)
        {
          Match regExLineMatch = regExMatcher.Match (expression);

          if (regExLineMatch.Success)
          {
            string address = regExLineMatch.Result ("${address}");

            memoryContext = CLangDebuggeeCodeContext.GetCodeContextForLocation (m_debugger, address);

            break;
          }
        }

        if (memoryContext == null)
        {
          return Constants.S_GETMEMORYCONTEXT_NO_MEMORY_CONTEXT;
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

    public override int GetPropertyInfo (enum_DEBUGPROP_INFO_FLAGS requestedFields, uint radix, uint timeout, IDebugReference2 [] debugReferenceArray, uint argumentCount, DEBUG_PROPERTY_INFO [] propertyInfoArray)
    {
      //
      // Fills in a DEBUG_PROPERTY_INFO structure that describes a property.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        LoggingUtils.RequireOk (base.GetPropertyInfo (requestedFields, radix, timeout, debugReferenceArray, argumentCount, propertyInfoArray));

#if false
        if ((m_gdbVariable != null) && (requestedFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME) != 0)
        {
          propertyInfoArray [0].bstrFullName = m_gdbVariable.Name;

          propertyInfoArray [0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME;
        }
#endif

        if ((requestedFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME) != 0)
        {
          if (m_gdbVariable != null)
          {
            propertyInfoArray [0].bstrName = m_gdbVariable.Expression;

            propertyInfoArray [0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME;
          }
        }

        if ((requestedFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE) != 0)
        {
          if (m_gdbVariable != null)
          {
            propertyInfoArray [0].bstrType = m_gdbVariable.Type;

            propertyInfoArray [0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE;
          }
        }

        if ((requestedFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0)
        {
#if false
          if ((requestedFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE_AUTOEXPAND) != 0)
          {
          }

          if ((requestedFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE_RAW) != 0)
          {
          }

          if ((requestedFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE_NO_TOSTRING) != 0)
          {
          }
#endif
          if (m_gdbVariable != null)
          {
            propertyInfoArray [0].bstrValue = m_gdbVariable.Value;

            propertyInfoArray [0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE;
          }
        }

        if ((requestedFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB) != 0)
        {
          if (m_gdbVariable != null)
          {
            if (m_gdbVariable.HasChildren)
            {
              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
            }

            if (m_gdbVariable.Expression.StartsWith ("$")) // Register. '$r0'
            {
              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_STORAGE_REGISTER;

              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_DATA;

              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_NONE;
            }
            else if (string.IsNullOrEmpty (m_gdbVariable.Type))
            {
              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_PROPERTY;
            }
            else if (m_gdbVariable.Value.Equals ("{...}"))
            {
              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_PROPERTY;

              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_CLASS;
            }
            else if (m_gdbVariable.Type.Contains ("(")) // Function, i.e: 'bool (void)'
            {
              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_METHOD;

              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY;

              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_NONE;
            }
            else
            {
              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_DATA;
            }

            if (m_gdbVariable.Name.Contains (".public"))
            {
              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_PUBLIC;
            }
            else if (m_gdbVariable.Name.Contains (".private"))
            {
              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_PRIVATE;
            }
            else if (m_gdbVariable.Name.Contains (".protected"))
            {
              propertyInfoArray [0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_PROTECTED;
            }
          }

          propertyInfoArray [0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB;
        }

        if ((requestedFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NO_NONPUBLIC_MEMBERS) != 0)
        {
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

    public override int GetSize (out uint size)
    {
      //
      // Returns the size, in bytes, of the property value.
      //

      LoggingUtils.PrintFunction ();

      size = 0;

      try
      {
        LoggingUtils.RequireOk(m_stackFrame.GetThread(out IDebugThread2 stackThread));

        LoggingUtils.RequireOk (stackThread.GetThreadId (out uint stackThreadId));

        string command = string.Format ("-data-evaluate-expression --thread {0} --frame {1} --language c \"sizeof({2})\"", stackThreadId, (m_stackFrame as CLangDebuggeeStackFrame).StackLevel, m_expression);

        MiResultRecord resultRecord = m_debugger.GdbClient.SendSyncCommand (command);

        MiResultRecord.RequireOk (resultRecord, command);

        if (resultRecord.HasField ("value"))
        {
          size = resultRecord ["value"] [0].GetUnsignedInt ();

          return Constants.S_GETSIZE_NO_SIZE;
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

    public override int SetValueAsString (string value, uint radix, uint timeout)
    {
      //
      // Sets the value of a property from a string.
      //

      LoggingUtils.PrintFunction ();

      try
      {
        string command = string.Format ("-var-assign \"{0}\" \"{1}\"", m_gdbVariable.Name, value);

        MiResultRecord resultRecord = m_debugger.GdbClient.SendSyncCommand (command);

        MiResultRecord.RequireOk (resultRecord, command);

        if (resultRecord.HasField ("value"))
        {
          m_gdbVariable.Populate (resultRecord.Results);

          m_debugger.VariableManager.UpdateVariable (m_gdbVariable);

          return Constants.S_OK;
        }

        return Constants.S_FALSE;
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
