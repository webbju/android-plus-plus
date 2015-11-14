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

import gdb
import lldb

import traceback

def print_exc(err_msg):
    print '<<< %s >>>' % err_msg
    traceback.print_exc()
    print '<<< --- >>>'


def type_summary_function(sbvalue, internal_dict):
    for p in gdb.pretty_printers:
        pp = p(gdb.Value(sbvalue.GetNonSyntheticValue()))
        if pp:
            try:
                summary = str(pp.to_string())
            except:
                print_exc('Error calling "to_string" method of a '
                          'GDB pretty printer.')
                summary = ''
            if hasattr(pp, 'display_hint') and pp.display_hint() == 'string':
                summary = '"%s"' % summary
            return summary
    raise RuntimeError('Could not find a pretty printer!')


class GdbPrinterSynthProvider(object):
    def __init__(self, sbvalue, internal_dict):
        self._sbvalue = sbvalue
        self._pp = None
        self._children = []
        for p in gdb.pretty_printers:
            try:
                self._pp = p(gdb.Value(self._sbvalue))
            except:
                print_exc('Error calling into GDB printer "%s".' % p.name)
            if self._pp:
                break
        if not self._pp:
            raise RuntimeError('Could not find a pretty printer!')

    def _get_children(self):
        if len(self._children) > 0:
            return
        if hasattr(self._pp, 'children') and (not self._children):
            try:
                children = self._pp.children()
            except:
                print_exc('Error calling "children" method of a '
                          'GDB pretty printer.')
                return
            try:
                for c in children:
                    self._children.append(c)
            except:
                print_exc('Error iterating over pretty printer children.')

    def _get_display_hint(self):
        if hasattr(self._pp, 'display_hint'):
            return self._pp.display_hint()

    def num_children(self):
        self._get_children()
        if self._get_display_hint() == 'map':
            return len(self._children) / 2
        else:
            return len(self._children)

    def get_child_index(self, name):
        if self._get_display_hint() == 'array':
            try:
                return int(name.lstrip('[').rstrip(']'))
            except:
                raise NameError(
                    'Value does not have a child with name "%s".' % name)

    def get_child_at_index(self, index):
        assert hasattr(self._pp, 'children')
        self._get_children()
        if self._get_display_hint() == 'map':
            if index < len(self._children):
                key = self._children[index * 2][1]
                val = self._children[index * 2 + 1][1]
                key_str = key.sbvalue().GetSummary()
                if not key_str:
                    key_str = key.sbvalue().GetValue()
                if not key_str:
                    key_str = str(key)
                if isinstance(val, gdb.Value):
                    return val.sbvalue().CreateChildAtOffset(
                        '[%s]' % key_str,
                        0,
                        val.sbvalue().GetType())
                else:
                    data = lldb.SBData()
                    data.SetDataFromUInt64Array([int(val)])
                    return self._sbvalue.CreateValueFromData(
                        '[%s]' % key_str,
                        data,
                        lldb.debugger.GetSelectedTarget().FindFirstType('int'))
        else:
            if index < len(self._children):
                c = self._children[index]
                if not isinstance(c[1], gdb.Value):
                    data = lldb.SBData()
                    data.SetDataFromUInt64Array([int(c[1])])
                    return self._sbvalue.CreateValueFromData(
                        c[0],
                        data,
                        lldb.debugger.GetSelectedTarget().FindFirstType('int'))
                else:
                    return c[1].sbvalue().CreateChildAtOffset(
                        c[0], 0, c[1].sbvalue().GetType())
                return sbvalue
        raise IndexError('Child not present at given index.')

    def update(self):
        self._children = []

    def has_children(self):
        return hasattr(self._pp, 'children')

    def get_value(self):
        return self._sbvalue


def register_pretty_printer(obj, printer):
    gdb.pretty_printers.append(printer)
    if lldb.debugger.GetCategory(printer.name).IsValid():
        print ('WARNING: A type category with name "%s" already exists.' %
               printer.name)
        return
    cat = lldb.debugger.CreateCategory(printer.name)
    type_options = (lldb.eTypeOptionCascade |
                    lldb.eTypeOptionSkipPointers |
                    lldb.eTypeOptionSkipReferences |
                    lldb.eTypeOptionHideEmptyAggregates)
    for sp in printer.subprinters:
        cat.AddTypeSummary(
            lldb.SBTypeNameSpecifier('^%s(<.+>)?(( )?&)?$' % sp.name, True),
            lldb.SBTypeSummary.CreateWithFunctionName(
                'gdb.printing.type_summary_function', type_options))
        cat.AddTypeSynthetic(
            lldb.SBTypeNameSpecifier('^%s(<.+>)?(( )?&)?$' % sp.name, True),
            lldb.SBTypeSynthetic.CreateWithClassName(
                'gdb.printing.GdbPrinterSynthProvider', type_options))
    cat.SetEnabled(True)
