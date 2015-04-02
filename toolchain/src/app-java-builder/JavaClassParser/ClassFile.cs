////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace app_java_builder.JavaClassParser
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class ClassFile
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    enum AccessFlags
    {
      ACC_PUBLIC = 0x0001,
      ACC_FINAL = 0x0010,
      ACC_SUPER = 0x0020,
      ACC_INTERFACE = 0x0200,
      ACC_ABSTRACT = 0x0400,
      ACC_SYNTHETIC = 0x1000,
      ACC_ANNOTATION = 0x2000,
      ACC_ENUM = 0x4000,
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public uint magic_number;

    public ushort minor_version;

    public ushort major_version;

    public ushort constant_pool_count;

    public ConstantInfo [] constant_pool;

    public ushort access_flags;

    public ushort this_class;

    public ushort super_class;

    public ushort interfaces_count;

    public ushort [] interfaces;

    public ushort fields_count;

    public FieldInfo [] fields;

    public ushort methods_count;

    public MethodInfo [] methods;

    public ushort attributes_count;

    public AttributeInfo [] attributes;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ClassFile (BinaryReader reader)
    {
      if (reader == null)
      {
        throw new ArgumentNullException ("reader");
      }

      magic_number = EndianSwap.SwapUInt32 (reader.ReadUInt32 ());

      if (magic_number != 0xCAFEBABE)
      {
        throw new InvalidOperationException (string.Format ("Unexpected magic number. {0} != 0xCAFEBABE", magic_number.ToString ("X")));
      }

      minor_version = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());

      major_version = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());

      constant_pool_count = EndianSwap.SwapUInt16 (reader.ReadUInt16 ()); // constant_pool_count item is equal to the number of entries in the constant_pool table plus one.

      constant_pool = new ConstantInfo [constant_pool_count];

      for (ushort i = 1; i < constant_pool_count; ++i)
      {
        ConstantInfo.Tags tag = (ConstantInfo.Tags) Enum.ToObject (typeof (ConstantInfo.Tags), reader.ReadByte ());

        switch (tag)
        {
          case ConstantInfo.Tags.CONSTANT_Class: constant_pool [i] = new ConstantClassInfo (); break;
          case ConstantInfo.Tags.CONSTANT_FieldRef: constant_pool [i] = new ConstantFieldRefInfo (); break;
          case ConstantInfo.Tags.CONSTANT_MethodRef: constant_pool [i] = new ConstantMethodRefInfo (); break;
          case ConstantInfo.Tags.CONSTANT_InterfaceMethodRef: constant_pool [i] = new ConstantInterfaceMethodRefInfo (); break;
          case ConstantInfo.Tags.CONSTANT_String: constant_pool [i] = new ConstantStringInfo (); break;
          case ConstantInfo.Tags.CONSTANT_Integer: constant_pool [i] = new ConstantIntegerInfo (); break;
          case ConstantInfo.Tags.CONSTANT_Float: constant_pool [i] = new ConstantFloatInfo (); break;
          case ConstantInfo.Tags.CONSTANT_Long: constant_pool [i] = new ConstantLongInfo (); break;
          case ConstantInfo.Tags.CONSTANT_Double: constant_pool [i] = new ConstantDoubleInfo (); break;
          case ConstantInfo.Tags.CONSTANT_NameAndType: constant_pool [i] = new ConstantNameAndTypeInfo (); break;
          case ConstantInfo.Tags.CONSTANT_Utf8: constant_pool [i] = new ConstantUtf8Info (); break;
          case ConstantInfo.Tags.CONSTANT_MethodHandle: constant_pool [i] = new ConstantMethodHandleInfo (); break;
          case ConstantInfo.Tags.CONSTANT_MethodType: constant_pool [i] = new ConstantMethodTypeInfo (); break;
          case ConstantInfo.Tags.CONSTANT_InvokeDynamic: constant_pool [i] = new ConstantInvokeDynamicInfo (); break;
          default: throw new InvalidOperationException ("Unexpected constant tag: " + tag.ToString ()); break;
        }

        constant_pool [i].Parse (ref reader);

        switch (tag)
        {
          case ConstantInfo.Tags.CONSTANT_Long:
          case ConstantInfo.Tags.CONSTANT_Double:
          {
            // 
            // If a CONSTANT_Long_info or CONSTANT_Double_info structure is the item in the constant_pool table at 
            // index n, then the next usable item in the pool is located at index n+2.
            // 

            ++i;

            break;
          }
        }
      }

      access_flags = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());

      this_class = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());

      super_class = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());

      interfaces_count = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());

      interfaces = new ushort [interfaces_count];

      for (ushort i = 0; i < interfaces_count; ++i)
      {
        interfaces [i] = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());
      }

      fields_count = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());

      fields = new FieldInfo [fields_count];

      for (ushort i = 0; i < fields_count; ++i)
      {
        fields [i] = new FieldInfo ();

        fields [i].Parse (ref reader);
      }

      methods_count = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());

      methods = new MethodInfo [methods_count];

      for (ushort i = 0; i < methods_count; ++i)
      {
        methods [i] = new MethodInfo ();

        methods [i].Parse (ref reader);
      }

      attributes_count = EndianSwap.SwapUInt16 (reader.ReadUInt16 ());

      attributes = new AttributeInfo [attributes_count];

      for (ushort i = 0; i < attributes_count; ++i)
      {
        attributes [i] = new AttributeInfo ();

        attributes [i].Parse (ref reader);
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
