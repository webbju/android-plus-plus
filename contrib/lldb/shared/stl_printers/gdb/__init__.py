# The MIT License (MIT)
# 
# Copyright (c) 2015 Siva Chandra
# 
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
# 
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
# 
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.

import lldb


pretty_printers = []


TYPE_CODE_BITSTRING = -1
TYPE_CODE_UNDEF = 0
TYPE_CODE_PTR = 1
TYPE_CODE_ARRAY = 2
TYPE_CODE_STRUCT = 3
TYPE_CODE_UNION = 4
TYPE_CODE_ENUM = 5
TYPE_CODE_FLAGS = 6
TYPE_CODE_FUNC = 7
TYPE_CODE_INT = 8
TYPE_CODE_FLT = 9
TYPE_CODE_VOID = 10
TYPE_CODE_SET = 11
TYPE_CODE_RANGE = 12
TYPE_CODE_STRING = 13
TYPE_CODE_ERROR = 14
TYPE_CODE_METHOD = 15
TYPE_CODE_METHODPTR = 16
TYPE_CODE_MEMBERPTR = 17
TYPE_CODE_REF = 18
TYPE_CODE_CHAR = 19
TYPE_CODE_BOOL = 20
TYPE_CODE_COMPLEX = 21
TYPE_CODE_TYPEDEF = 22
TYPE_CODE_NAMESPACE = 23
TYPE_CODE_DECFLOAT = 24
TYPE_CODE_MODULE = 25
TYPE_CODE_INTERNAL_FUNCTION = 26
TYPE_CODE_XMETHOD = 27


OP_ADD = 0
OP_SUB = 1
OP_BITWISE_AND = 2
OP_BITWISE_OR = 3
OP_BITWISE_XOR = 4
OP_LSHIFT = 5
OP_RSHIFT = 6


TYPE_CLASS_TO_TYPE_CODE_MAP = {
    lldb.eTypeClassInvalid: TYPE_CODE_UNDEF,
    lldb.eTypeClassArray: TYPE_CODE_ARRAY,
    lldb.eTypeClassBlockPointer: TYPE_CODE_UNDEF,
    lldb.eTypeClassBuiltin: TYPE_CODE_UNDEF,
    lldb.eTypeClassClass: TYPE_CODE_STRUCT,
    lldb.eTypeClassComplexFloat: TYPE_CODE_COMPLEX,
    lldb.eTypeClassComplexInteger: TYPE_CODE_COMPLEX,
    lldb.eTypeClassEnumeration: TYPE_CODE_ENUM,
    lldb.eTypeClassFunction: TYPE_CODE_FUNC,
    lldb.eTypeClassMemberPointer: TYPE_CODE_PTR,
    lldb.eTypeClassObjCObject: TYPE_CODE_UNDEF,
    lldb.eTypeClassObjCInterface: TYPE_CODE_UNDEF,
    lldb.eTypeClassObjCObjectPointer:TYPE_CODE_UNDEF,
    lldb.eTypeClassPointer: TYPE_CODE_UNDEF,
    lldb.eTypeClassReference: TYPE_CODE_REF,
    lldb.eTypeClassStruct: TYPE_CODE_STRUCT,
    lldb.eTypeClassTypedef: TYPE_CODE_TYPEDEF,
    lldb.eTypeClassUnion: TYPE_CODE_UNION,
    lldb.eTypeClassVector: TYPE_CODE_UNDEF,
    lldb.eTypeClassOther:TYPE_CODE_UNDEF,
    lldb.eTypeClassAny: TYPE_CODE_UNDEF

}


BASIC_TYPE_TO_TYPE_CODE_MAP = {
    lldb.eBasicTypeInvalid: TYPE_CODE_UNDEF,
    lldb.eBasicTypeVoid: TYPE_CODE_VOID,
    lldb.eBasicTypeChar: TYPE_CODE_CHAR,
    lldb.eBasicTypeSignedChar: TYPE_CODE_CHAR,
    lldb.eBasicTypeUnsignedChar: TYPE_CODE_CHAR,
    lldb.eBasicTypeWChar: TYPE_CODE_CHAR,
    lldb.eBasicTypeSignedWChar: TYPE_CODE_CHAR,
    lldb.eBasicTypeUnsignedWChar: TYPE_CODE_CHAR,
    lldb.eBasicTypeChar16: TYPE_CODE_CHAR,
    lldb.eBasicTypeChar32: TYPE_CODE_CHAR,
    lldb.eBasicTypeShort: TYPE_CODE_INT,
    lldb.eBasicTypeUnsignedShort: TYPE_CODE_INT,
    lldb.eBasicTypeInt: TYPE_CODE_INT,
    lldb.eBasicTypeUnsignedInt: TYPE_CODE_INT,
    lldb.eBasicTypeLong: TYPE_CODE_INT,
    lldb.eBasicTypeUnsignedLong: TYPE_CODE_INT,
    lldb.eBasicTypeLongLong: TYPE_CODE_INT,
    lldb.eBasicTypeUnsignedLongLong: TYPE_CODE_INT,
    lldb.eBasicTypeInt128: TYPE_CODE_INT,
    lldb.eBasicTypeUnsignedInt128: TYPE_CODE_INT,
    lldb.eBasicTypeBool: TYPE_CODE_BOOL,
    lldb.eBasicTypeHalf: TYPE_CODE_UNDEF,
    lldb.eBasicTypeFloat: TYPE_CODE_FLT,
    lldb.eBasicTypeDouble: TYPE_CODE_FLT,
    lldb.eBasicTypeLongDouble: TYPE_CODE_FLT,
    lldb.eBasicTypeFloatComplex: TYPE_CODE_COMPLEX,
    lldb.eBasicTypeDoubleComplex: TYPE_CODE_COMPLEX,
    lldb.eBasicTypeLongDoubleComplex: TYPE_CODE_COMPLEX,
    lldb.eBasicTypeObjCID: TYPE_CODE_UNDEF,
    lldb.eBasicTypeObjCClass: TYPE_CODE_UNDEF,
    lldb.eBasicTypeObjCSel: TYPE_CODE_UNDEF,
    lldb.eBasicTypeNullPtr: TYPE_CODE_UNDEF,
    lldb.eBasicTypeOther:TYPE_CODE_UNDEF,
}


BASIC_UNSIGNED_INTEGER_TYPES = [
    lldb.eBasicTypeUnsignedChar, lldb.eBasicTypeUnsignedWChar,
    lldb.eBasicTypeUnsignedShort, lldb.eBasicTypeUnsignedInt,
    lldb.eBasicTypeUnsignedLong, lldb.eBasicTypeUnsignedLongLong,
    lldb.eBasicTypeUnsignedInt128
]

BASIC_SIGNED_INTEGER_TYPES = [
    lldb.eBasicTypeChar, lldb.eBasicTypeSignedChar, lldb.eBasicTypeWChar,
    lldb.eBasicTypeChar16, lldb.eBasicTypeChar32, lldb.eBasicTypeShort,
    lldb.eBasicTypeInt, lldb.eBasicTypeLong, lldb.eBasicTypeLongLong,
    lldb.eBasicTypeInt128
]

BASIC_FLOAT_TYPES = [
    lldb.eBasicTypeFloat, lldb.eBasicTypeDouble, lldb.eBasicTypeLongDouble
]


BUILTIN_TYPE_NAME_TO_SBTYPE_MAP = {
    'char': None,
    'unsigned char': None,
    'short': None,
    'unsigned short': None,
    'int': None,
    'unsigned': None,
    'unsigned int': None,
    'long': None,
    'unsigned long': None,
    'long long': None,
    'unsigned long long': None,
    'float': None,
    'double': None,
    'long double': None
}

def get_builtin_sbtype(typename):
    sbtype = BUILTIN_TYPE_NAME_TO_SBTYPE_MAP.get(typename)
    if not sbtype:
        target = lldb.debugger.GetSelectedTarget()
        sbtype = target.FindFirstType(typename)
        BUILTIN_TYPE_NAME_TO_SBTYPE_MAP[typename] = sbtype
    return sbtype


class EnumField(object):
    def __init__(self, name, enumval, type):
        self.name = name
        self.enumval = enumval
        self.type = type
        self.is_base_class = False
    

class MemberField(object):
    def __init__(self, name, type, bitsize, parent_type):
        self.name = name
        self.type = type
        self.bitsize = bitsize
        self.parent_type = parent_type
        self.is_base_class = False


class BaseClassField(object):
    def __init__(self, name, type):
        self.name = name
        self.type = type
        self.is_base_class = True


class Type(object):
    def __init__(self, sbtype_object):
        self._sbtype_object = sbtype_object

    def sbtype(self):
        return self._sbtype_object

    def _is_baseclass(self, baseclass_sbtype):
        base_sbtype = Type(baseclass_sbtype).strip_typedefs().sbtype()
        self_sbtype = self.strip_typedefs().sbtype()
        for i in range(self_sbtype.GetNumberOfDirectBaseClasses()):
            base_mem = self_sbtype.GetDirectBaseClassAtIndex(i)
            base_sbtype_i = Type(base_mem.GetType()).strip_typedefs().sbtype()
            if base_sbtype_i == base_sbtype:
                return (True, base_mem.GetOffsetInBytes())
            else:
                is_baseclass, offset = Type(base_sbtype_i)._is_baseclass(
                    baseclass_sbtype)
                if is_baseclass:
                    return (True, base_mem.GetOffsetInBytes() + offset)
        return (False, None)

    def __str__(self):
        return self._sbtype_object.GetName()

    @property
    def code(self):
        type_class = self._sbtype_object.GetTypeClass()
        type_code = TYPE_CLASS_TO_TYPE_CODE_MAP.get(type_class,
                                                    TYPE_CODE_UNDEF)
        if int(type_code) != int(TYPE_CODE_UNDEF):
            return int(type_code)

        if type_class == lldb.eTypeClassBuiltin:
            basic_type = self._sbtype_object.GetBasicType()
            return int(
                BASIC_TYPE_TO_TYPE_CODE_MAP.get(basic_type, TYPE_CODE_UNDEF))

        return TYPE_CODE_UNDEF

    @property
    def name(self):
        return self._sbtype_object.GetName()

    @property
    def sizeof(self):
        return self._sbtype_object.GetByteSize()

    @property
    def tag(self):
        return self._sbtype_object.GetName()

    def target(self):
        type_class = self._sbtype_object.GetTypeClass()
        if type_class == lldb.eTypeClassPointer:
            return Type(self._sbtype_object.GetPointeeType())
        elif type_class == lldb.eTypeClassReference:
            return Type(self._sbtype_object.GetDereferencedType())
        elif type_class == lldb.eTypeClassArray:
            return Type(self._sbtype_object.GetArrayElementType())
        elif type_class == lldb.eTypeClassFunction:
            return Type(self._sbtype_object.GetFunctionReturnType())
        else:
            raise TypeError('Type "%s" cannot have target type.' %
                            self._sbtype_object.GetName())

    def strip_typedefs(self):
        sbtype = self._sbtype_object
        while sbtype.GetTypedefedType():
            if sbtype == sbtype.GetTypedefedType():
                break
            sbtype = sbtype.GetTypedefedType()
        return Type(sbtype)

    def unqualified(self):
        return Type(self._sbtype_object.GetUnqualifiedType())

    def pointer(self):
        return Type(self._sbtype_object.GetPointerType())

    def template_argument(self, n):
        # TODO: This is woefully incomplete!
        return Type(self._sbtype_object.GetTemplateArgumentType(n))

    def fields(self):
        type_class = self._sbtype_object.GetTypeClass()
        fields = []
        if type_class == lldb.eTypeClassEnumeration:
            enum_list = self._sbtype_object.GetEnumMembers()
            for i in range(0, enum_list.GetSize()):
                e = enum_list.GetTypeEnumMemberAtIndex(i)
                fields.append(EnumField(e.GetName(),
                                        e.GetValueAsSigned(),
                                        Type(e.GetType())))
        elif (type_class == lldb.eTypeClassUnion or
              type_class == lldb.eTypeClassStruct or
              type_class == lldb.eTypeClassClass):
            n_baseclasses = self._sbtype_object.GetNumberOfDirectBaseClasses()
            for i in range(0, n_baseclasses):
                c = self._sbtype_object.GetDirectBaseClassAtIndex(i)
                fields.append(BaseClassField(c.GetName(), Type(c.GetType())))
            for i in range(0, self._sbtype_object.GetNumberOfFields()):
                f = self._sbtype_object.GetFieldAtIndex(i)
                fields.append(MemberField(f.GetName(),
                                          Type(f.GetType()),
                                          f.GetBitfieldSizeInBits(),
                                          self))
        else:
            raise TypeError('Type "%s" cannot have fields.' %
                            self._sbtype_object.GetName())
        return fields


class Value(object):
    def __init__(self, sbvalue_object):
        self._sbvalue_object = sbvalue_object

    def sbvalue(self):
        return self._sbvalue_object

    def _stripped_sbtype(self):
        sbtype = self._sbvalue_object.GetType()
        stripped_sbtype = Type(sbtype).strip_typedefs().sbtype()
        type_class = stripped_sbtype.GetTypeClass()
        return stripped_sbtype, type_class

    def _as_number(self):
        sbtype, type_class = self._stripped_sbtype()
        if ((type_class == lldb.eTypeClassPointer) or
            (type_class in BASIC_UNSIGNED_INTEGER_TYPES)):
            numval = self._sbvalue_object.GetValueAsUnsigned()
        elif type_class in BASIC_SIGNED_INTEGER_TYPES:
            numval = self._sbvalue_object.GetValueAsSigned()
        elif type_class in BASIC_FLOAT_TYPES:
            err = lldb.SBError()
            if type_class == lldb.eBasicTypeFloat:
                numval = self._sbvalue_object.GetData().GetFloat(err, 0)
            elif type_class == lldb.eBasicTypeDouble:
                numval = self._sbvalue_object.GetData().GetDouble(err, 0)
            elif type_class == lldb.eBasicTypeLongDouble:
                numval = self._sbvalue_object.GetData().GetLongDouble(err, 0)
            else:
                raise RuntimeError('Something unexpected has happened.')
            if not err.Success():
                raise RuntimeError(
                    'Could not convert float type value to a number:\n%s' %
                    err.GetCString())
        else:
            return TypeError(
                'Conversion of non-numerical/non-pointer values to numbers is '
                'not supported.')
        return numval

    def _binary_op(self, other, op, reverse=False):
        sbtype, type_class = self._stripped_sbtype()
        if type_class == lldb.eTypeClassPointer:
            if not (op == OP_ADD or op == OP_SUB) or reverse:
                raise TypeError(
                    'Invalid binary operation on/with pointer value.')
        if isinstance(other, int) or isinstance(other, long):
            other_val = other
            other_sbtype = get_builtin_sbtype('long')
            other_type_class = lldb.eTypeClassBuiltin
        elif isinstance(other, float):
            other_val = other
            other_sbtype = get_builtin_sbtype('double')
            other_type_class = lldb.eTypeClassBuiltin
        elif isinstance(other, Value):
            other_sbtype, other_type_class = other._stripped_sbtype()
            if (other_type_class == lldb.eTypeClassPointer and
                not (type_class == lldb.eTypeClassPointer and op == OP_SUB)):
                raise TypeError(
                    'Invalid binary operation on/with pointer value.')
            other_val = other._as_number()
        else:
            raise TypeError('Cannot perform binary operation with/on value '
                            'of type "%s".' % str(type(other)))
        if op == OP_BITWISE_AND:
            res = self._as_number() & other_val
        elif op == OP_BITWISE_OR:
            res = self._as_number() | other_val
        elif op == OP_BITWISE_XOR:
            res = self._as_number() ^ other_val
        elif op == OP_ADD:
            if type_class == lldb.eTypeClassPointer:
                addr = self._sbvalue_object.GetValueAsUnsigned()
                new_addr = (addr +
                            other_val * sbtype.GetPointeeType().GetByteSize())
                new_sbvalue = self._sbvalue_object.CreateValueFromAddress(
                    '', new_addr, sbtype.GetPointeeType())
                return Value(new_sbvalue.AddressOf().Cast(
                    self._sbvalue_object.GetType()))
            else:
                res = self._as_number() + other_val
        elif op == OP_SUB:
            if reverse:
                res = other_val - self._as_number()
            else:
                if type_class == lldb.eTypeClassPointer:
                    if other_type_class == lldb.eTypeClassPointer:
                        if sbtype != other_sbtype:
                            raise TypeError('Arithmetic operation on '
                                            'incompatible pointer types.')
                        diff = self._as_number() - other_val
                        return diff / sbtype.GetPointeeType().GetByteSize()
                    else:
                        return self._binary_op(- other_val, OP_ADD)
                else:
                    res = self._as_number() - other_val
        elif op == OP_LSHIFT:
            if reverse:
                return other_val << self._as_number()
            else:
                res = self._as_number() << other_val
        elif op == OP_RSHIFT:
            if reverse:
                return other_val >> self._as_number()
            else:
                res = self._as_number() >> other_val
        else:
            raise RuntimeError('Unsupported or incorrect binary operation.')
        data = lldb.SBData()
        data.SetDataFromUInt64Array([res])
        return Value(self._sbvalue_object.CreateValueFromData(
            '', data, self._sbvalue_object.GetType()))

    def _cmp(self, other):
        if (isinstance(other, int) or isinstance(other, long) or
            isinstance(other, float)):
            other_val = other
        elif isinstance(other, Value):
            other_val = other._as_number()
        else:
            raise TypeError('Comparing incompatible types.')
        self_val = self._as_number()
        if self_val == other_val:
                return 0
        elif self_val < other_val:
            return -1
        else:
            return 1

    def __str__(self):
        valstr = self._sbvalue_object.GetSummary()
        if not valstr:
            valstr = self._sbvalue_object.GetValue()
        if not valstr:
            valstr = str(self._sbvalue_object)
        return valstr

    def __int__(self):
        return int(self._as_number())

    def __long__(self):
        return long(self._as_number())

    def __float__(self):
        return float(self._as_number())

    def __getitem__(self, index):
        sbtype = self._sbvalue_object.GetType()
        stripped_sbtype, type_class = self._stripped_sbtype()
        if type_class == lldb.eTypeClassPointer:
            val = Value(
                self._sbvalue_object.Cast(stripped_sbtype).Dereference())
            stripped_sbtype, type_class = val._stripped_sbtype()
            stripped_sbval = val.sbvalue().Cast(stripped_sbtype)
        else:
            stripped_sbval = self._sbvalue_object.Cast(stripped_sbtype)
        if (type_class == lldb.eTypeClassClass or
            type_class == lldb.eTypeClassStruct or
            type_class == lldb.eTypeClassUnion):
            if not isinstance(index, str):
                raise KeyError('Key value used to subscript a '
                               'class/struct/union value is not a string.')
            mem_sbval = stripped_sbval.GetChildMemberWithName(index)
            if (not mem_sbval) or (not mem_sbval.IsValid()):
                raise KeyError(
                    'No member with name "%s" in value of type "%s".' %
                    (index, sbtype.GetName()))
            return Value(mem_sbval)

        if not (isinstance(index, int) or isinstance(index, long)):
            raise KeyError('Unsupported key type for "[]" operator.')

        if type_class == lldb.eTypeClassPointer:
            addr = self._sbvalue_object.GetValueAsUnsigned()
            elem_sbtype = self._sbvalue_object.GetType().GetPointeeType()
        elif type_class == lldb.eTypeClassArray:
            addr = self._sbvalue_object.GetLoadAddress()
            elem_sbtype = self._sbvalue_object.GetType().GetArrayElementType()
        else:
            raise TypeError('Cannot use "[]" operator on values of type "%s".' %
                            str(sbtype))
        new_addr = addr + index * elem_sbtype.GetByteSize()
        return Value(self._sbvalue_object.CreateValueFromAddress(
             "", new_addr, elem_sbtype))

    def __add__(self, number):
        return self._binary_op(number, OP_ADD)

    def __radd__(self, number):
        return self._binary_op(number, OP_ADD, reverse=True)

    def __sub__(self, number):
        return self._binary_op(number, OP_SUB)

    def __rsub__(self, number):
        return self._binary_op(number, OP_SUB, reverse=True)

    def __nonzero__(self):
        sbtype, type_class = self._stripped_sbtype()
        if ((type_class == lldb.eTypeClassPointer) or
            (type_class in BASIC_UNSIGNED_INTEGER_TYPES) or
            (type_class in BASIC_UNSIGNED_INTEGER_TYPES) or
            (type_class in BASIC_FLOAT_TYPES)):
            return self._as_number() != 0
        else:
            return self._sbvalue_object.IsValid()

    def __eq__(self, other):
        if self._cmp(other) == 0:
            return True
        else:
            return False

    def __ne__(self, other):
        if self._cmp(other) != 0:
            return True
        else:
            return False

    def __lt__(self, other):
        if self._cmp(other) < 0:
            return True
        else:
            return False

    def __le__(self, other):
        if self._cmp(other) <= 0:
            return True
        else:
            return False

    def __gt__(self, other):
        if self._cmp(other) > 0:
            return True
        else:
            return False

    def __ge__(self, other):
        if self._cmp(other) >= 0:
            return True
        else:
            return False

    def __and__(self, other):
        return self._binary_op(other, OP_BITWISE_AND)

    def __rand__(self, other):
        return self._binary_op(other, OP_BITWISE_AND, reverse=True)

    def __or__(self, other):
        return self._binary_op(other, OP_BITWISE_OR)

    def __ror__(self, other):
        return self._binary_op(other, OP_BITWISE_OR, reverse=True)

    def __xor__(self, other):
        return self._binary_op(other, OP_BITWISE_XOR)

    def __rxor__(self, other):
        return self._binary_op(other, OP_BITWISE_XOR, reverse=True)

    def __lshift__(self, other):
        return self._binary_op(other, OP_LSHIFT)

    def __rlshift__(self, other):
        return self._binary_op(other, OP_LSHIFT, reverse=True)

    def __rshift__(self, other):
        return self._binary_op(other, OP_RSHIFT)

    def __rrshift__(self, other):
        return self._binary_op(other, OP_RSHIFT, reverse=True)

    @property
    def type(self):
        return Type(self._sbvalue_object.GetType())

    @property
    def address(self):
        ptr_sbvalue = self._sbvalue_object.AddressOf()
        if not ptr_sbvalue.IsValid():
            load_address = self._sbvalue_object.GetLoadAddress()
            new_sbvalue = self._sbvalue_object.CreateValueFromAddress(
                '', load_address, self._sbvalue_object.GetType())
            ptr_sbvalue = new_sbvalue.AddressOf()
        return Value(ptr_sbvalue)

    def cast(self, gdbtype):
        target_sbtype = gdbtype.sbtype()
        self_sbtype = self._sbvalue_object.GetType()
        is_baseclass, offset = Type(self_sbtype)._is_baseclass(target_sbtype)
        if is_baseclass:
            return Value(self._sbvalue_object.CreateChildAtOffset(
                self._sbvalue_object.GetName(), offset, target_sbtype))
        return Value(self._sbvalue_object.Cast(gdbtype.sbtype()))

    def dereference(self):
        stripped_sbtype, _ = self._stripped_sbtype()
        stripped_sbval = self._sbvalue_object.Cast(stripped_sbtype)
        return Value(stripped_sbval.Dereference())

    def referenced_value(self):
        return Value(self._sbvalue_object.Dereference())

    def string(self, length):
        s = ''
        for i in range(0, length):
            target = lldb.debugger.GetSelectedTarget()
            sbaddr = lldb.SBAddress(
                self._sbvalue_object.GetValueAsUnsigned() + i, target)
            sberr = lldb.SBError()
            ss = str(target.ReadMemory(sbaddr, 1, sberr))
            s += ss
        return s


def parse_and_eval(expr):
    opts = lldb.SBExpressionOptions()
    sbvalue = lldb.debugger.GetSelectedTarget().EvaluateExpression(expr, opts)
    if sbvalue and sbvalue.IsValid():
        return Value(sbvalue)
    return RuntimeError('Unable to evaluate "%s".', expr)


def lookup_type(name, block=None):
    chunks = name.split('::')
    unscoped_name = chunks[-1]
    typelist = lldb.debugger.GetSelectedTarget().FindTypes(unscoped_name)
    count = typelist.GetSize()
    for i in range(count):
        t = typelist.GetTypeAtIndex(i)
        if t.GetName() == name:
            return Type(t)
        else:
            canonical_sbtype = t.GetCanonicalType()
            if canonical_sbtype.GetName() == name:
                return Type(canonical_sbtype)
    raise RuntimeError('Type "%s" not found in %d matches.' % (name, count))


def current_objfile():
    return None


def default_visualizer(value):
    for p in pretty_printers:
        pp = p(value)
        if pp:
            return pp
