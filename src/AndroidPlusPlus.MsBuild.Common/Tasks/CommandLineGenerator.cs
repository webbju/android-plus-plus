﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.Build.Framework;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xaml;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.Common
{

  public class CommandLineGenerator
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CommandLineGenerator()
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static Rule LoadXamlRule (string filepath)
    {
      if (filepath == null)
      {
        throw new FileNotFoundException(nameof(filepath), filepath);
      }

      // 
      // Allow for the potential to load properties through XAML files with a 'ProjectSchemaDefinitions' root node.
      // 

      var xmlRootNode = XamlServices.Load (filepath);

      if (xmlRootNode == null)
      {
        return null;
      }

      if (xmlRootNode.GetType () == typeof (ProjectSchemaDefinitions))
      {
        ProjectSchemaDefinitions projectSchemaDefs = (ProjectSchemaDefinitions)xmlRootNode;

        return (Rule)projectSchemaDefs.Nodes [0];
      }

      return (Rule)xmlRootNode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static Rule ParseXamlRule(string xaml)
    {
      var xmlRootNode = XamlServices.Parse(xaml);

      if (xmlRootNode.GetType() == typeof(ProjectSchemaDefinitions))
      {
        ProjectSchemaDefinitions projectSchemaDefs = (ProjectSchemaDefinitions)xmlRootNode;

        return projectSchemaDefs.Nodes[0] as Rule;
      }

      return xmlRootNode as Rule;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string GenerateCommandLine (Rule rule, Dictionary<string, object> propertyValues, HashSet<string> propertiesToIgnore = null)
    {
      // 
      // Generate a command line with arguments in the same order as those in rule.Properties.
      // (We might need to add support for sorting by an explicit order.)
      // 

      var builder = new CommandLineBuilder();

      foreach (var property in rule.Properties)
      {
        if (property == null)
        {
          continue;
        }
        else if (!property.IncludeInCommandLine)
        {
          continue; // Property explicitly excluded.
        }
        else if (propertiesToIgnore?.Contains(property.Name) ?? false)
        {
          continue; // Property explicitly ignored.
        }
        
        if (!propertyValues.TryGetValue(property.Name, out object value))
        {
          continue; // No associated value found for this property.
        }

        if (property.GetType().Equals(typeof(StringListProperty)))
        {
          GenerateArgumentStringList(ref builder, rule, property as StringListProperty, value);
        }
        else if (property.GetType().Equals(typeof(StringProperty)))
        {
          GenerateArgumentString(ref builder, rule, property as StringProperty, value);
        }
        else if (property.GetType().Equals(typeof(EnumProperty)))
        {
          GenerateArgumentEnum(ref builder, rule, property as EnumProperty, value);
        }
        else if (property.GetType().Equals(typeof(BoolProperty)))
        {
          GenerateArgumentBool(ref builder, rule, property as BoolProperty, value);
        }
        else if (property.GetType().Equals(typeof(IntProperty)))
        {
          GenerateArgumentInt(ref builder, rule, property as IntProperty, value);
        }
        else
        {
          throw new NotImplementedException();
        }
      }

      return builder.ToString();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void GenerateArgumentEnum (ref CommandLineBuilder builder, Rule rule, EnumProperty property, object value)
    {
      if (string.IsNullOrWhiteSpace(value as string))
      {
        return;
      }

      var result = property.AdmissibleValues.Find(x => x.Name == value as string);

      if (property.AdmissibleValues.Count > 0 && result == null)
      {
        throw new ArgumentException($"Failed to find {value} in {nameof(property.AdmissibleValues)}");
      }

      string switchPrefix = !string.IsNullOrWhiteSpace(property.SwitchPrefix) ? property.SwitchPrefix : rule.SwitchPrefix;

      string evaluatedSwitch = $"{switchPrefix ?? string.Empty}{property.Switch ?? string.Empty}{property.Separator ?? string.Empty}";

      string enumSwitch = $"{result?.SwitchPrefix ?? string.Empty}{result?.Switch ?? string.Empty}";

      if (string.IsNullOrEmpty(enumSwitch))
      {
        return;
      }

      builder.AppendSwitch($"{evaluatedSwitch}{enumSwitch}");
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void GenerateArgumentStringList (ref CommandLineBuilder builder, Rule rule, StringListProperty property, object value)
    {
      AppendStringListValue(ref builder, rule, property, value);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void GenerateArgumentString (ref CommandLineBuilder builder, Rule rule, StringProperty property, object value)
    {
      AppendStringValue (ref builder, rule, property, value);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void GenerateArgumentInt (ref CommandLineBuilder builder, Rule rule, IntProperty property, object value)
    {
      if (value == null)
      {
        return;
      }
      else if (Nullable.GetUnderlyingType(value.GetType()) == typeof(int) && (value as int?).HasValue)
      {
        AppendIntValue(ref builder, rule, property, (value as int?).Value);
      }
      else if (value.GetType() == typeof(int))
      {
        AppendIntValue(ref builder, rule, property, (int)value);
      }
      else
      {
        throw new NotImplementedException($"{nameof(value)} is unexpected type: {value.GetType()}");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void GenerateArgumentBool (ref CommandLineBuilder builder, Rule rule, BoolProperty property, object value)
    {
      if (value == null)
      {
        return;
      }
      else if (Nullable.GetUnderlyingType(value.GetType()) == typeof(bool) && (value as bool?).HasValue)
      {
        AppendBoolProperty(ref builder, rule, property, (value as bool?).Value);
      }
      else if (value.GetType() == typeof(bool))
      {
        AppendBoolProperty(ref builder, rule, property, (bool)value);
      }
      else
      {
        throw new NotImplementedException($"{nameof(value)} is unexpected type: {value.GetType()}");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void AppendBoolProperty(ref CommandLineBuilder builder, Rule rule, BoolProperty property, bool value)
    {
      string switchPrefix = !string.IsNullOrWhiteSpace(property.SwitchPrefix) ? property.SwitchPrefix : rule.SwitchPrefix;

      if (value)
      {
        builder.AppendSwitch($"{switchPrefix}{property.Switch}");
      }
      else if (!string.IsNullOrWhiteSpace(property.ReverseSwitch))
      {
        builder.AppendSwitch($"{switchPrefix}{property.ReverseSwitch}");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void AppendIntValue(ref CommandLineBuilder builder, Rule rule, IntProperty property, int value)
    {
      string switchPrefix = !string.IsNullOrWhiteSpace(property.SwitchPrefix) ? property.SwitchPrefix : rule.SwitchPrefix;

      string evaluatedSwitch = $"{switchPrefix ?? string.Empty}{property.Switch ?? string.Empty}{property.Separator ?? string.Empty}";

      builder.AppendSwitchUnquotedIfNotNull(evaluatedSwitch, $"{value}");
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void AppendStringValue(ref CommandLineBuilder builder, Rule rule, StringProperty property, object value)
    {
      if (value == null)
      {
        return;
      }

      string switchPrefix = !string.IsNullOrWhiteSpace(property.SwitchPrefix) ? property.SwitchPrefix : rule.SwitchPrefix;

      string evaluatedSwitch = $"{switchPrefix ?? string.Empty}{property.Switch ?? string.Empty}{property.Separator ?? string.Empty}";

      var delimiter = " ";// !string.IsNullOrWhiteSpace(property.CommandLineValueSeparator) ? property.CommandLineValueSeparator : $" {evaluatedSwitch}";

      if (value.GetType() == typeof(ITaskItem))
      {
        builder.AppendSwitchIfNotNull(evaluatedSwitch, ConvertToObject<ITaskItem>(value));
      }
      else if (value.GetType() == typeof(ITaskItem[]))
      {
        builder.AppendSwitchIfNotNull(evaluatedSwitch, value as ITaskItem[], $"{delimiter}{evaluatedSwitch}");
      }
      else if (string.Equals(property.DataSource?.ItemType, "Item", StringComparison.OrdinalIgnoreCase))
      {
        builder.AppendSwitchIfNotNull(evaluatedSwitch, value as ITaskItem[], $"{delimiter}{evaluatedSwitch}");
      }
      else if (string.Equals(property.Subtype, "file", StringComparison.OrdinalIgnoreCase) || string.Equals(property.Subtype, "folder", StringComparison.OrdinalIgnoreCase))
      {
        builder.AppendSwitchIfNotNull(evaluatedSwitch, ConvertToObject<ITaskItem>(value));
      }
      else if (!string.IsNullOrWhiteSpace(evaluatedSwitch))
      {
        builder.AppendSwitchUnquotedIfNotNull($"{evaluatedSwitch}", ConvertToObject<string>(value));
      }
      else
      {
        builder.AppendTextUnquoted(" " + value);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void AppendStringListValue(ref CommandLineBuilder builder, Rule rule, StringListProperty property, object value)
    {
      if (value == null)
      {
        return;
      }

      string switchPrefix = !string.IsNullOrWhiteSpace(property.SwitchPrefix) ? property.SwitchPrefix : rule.SwitchPrefix;

      string evaluatedSwitch = $"{switchPrefix ?? string.Empty}{property.Switch ?? string.Empty}{property.Separator ?? string.Empty}";

      var delimiter = !string.IsNullOrWhiteSpace(property.CommandLineValueSeparator) ? property.CommandLineValueSeparator : $" {evaluatedSwitch}";

      if (value.GetType() == typeof(ITaskItem[]))
      {
        builder.AppendSwitchIfNotNull(evaluatedSwitch, value as ITaskItem[], delimiter);
      }
      else if (value.GetType() == typeof(string[]))
      {
        builder.AppendSwitchIfNotNull($"{evaluatedSwitch}", value as string[], delimiter);
      }
      else
      {
        throw new NotImplementedException($"{property.Name} {value.GetType()}");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static T ConvertToObject<T>(object value)
    {
      if (value == null)
      {
        return default;
      }

      object returnValue = value;

      if ((typeof(T) == typeof(bool) || typeof(T) == typeof(bool?)) && value.GetType() == typeof(string))
      {
        returnValue = bool.TryParse(value as string, out bool boolValue) ? boolValue : (bool?)null;
      }
      else if ((typeof(T) == typeof(int) || typeof(T) == typeof(int?)) && value.GetType() == typeof(string))
      {
        returnValue = int.TryParse(value as string, out int intValue) ? intValue : (int?)null;
      }
      else if (typeof(T) == typeof(string) && value.GetType() == typeof(string))
      {
        returnValue = string.IsNullOrWhiteSpace(value as string) ? null : value as string;
      }
      else if (typeof(T) == typeof(ITaskItem) && value.GetType() == typeof(string))
      {
        returnValue = string.IsNullOrWhiteSpace(value as string) ? null : new TaskItem(value as string);
      }
      else if (typeof(T) == typeof(ITaskItem) && value.GetType() == typeof(TaskItem))
      {
        returnValue = value as ITaskItem;
      }

      try
      {
        return (T)returnValue; // Can we safety coerce this type cast anyway?
      }
      catch (Exception e)
      {
        throw new NotImplementedException($"Unhandled convertion from {value.GetType()} to {typeof(T)}", e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

}