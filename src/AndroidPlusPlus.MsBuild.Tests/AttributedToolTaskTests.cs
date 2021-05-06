using AndroidPlusPlus.MsBuild.Common;
using AndroidPlusPlus.MsBuild.Common.Attributes;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Diagnostics;

namespace AndroidPlusPlus.MsBuild.Tests
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  internal class AttributedToolTask : TrackedToolTask
  {
    [SwitchBool(Switch = "--true", ReverseSwitch = "--false", Separator = " ")]
    bool Boolean => true;

    [SwitchBool(Switch = "--true", ReverseSwitch = "--false", Separator = " ")]
    bool BooleanFalse => false;

    [SwitchBool(Switch = "--nullable-bool", ReverseSwitch = "--no-nullable-bool", Separator = " ")]
    bool? BooleanNullable => null;

    [SwitchEnum(Switch = "--enum", Separator = " ")]
    [SwitchEnumValue(Name = "enum-name", Switch = "enum-value")]
    string Enum => "enum-name";

    [SwitchInt(Switch = "--integer", Separator = " ")]
    int Integer => default;

    [SwitchInt(Switch = "--integer-nullable", Separator = " ")]
    int? IntegerNullable => null;

    [SwitchString(Switch = "--string-null", Separator = " ")]
    string StringNullString => null;

    [SwitchString(Switch = "--string-valid", Separator = " ")]
    string StringValidString => nameof(StringValidString);

    [SwitchStringList(Subtype = "file", Switch = "--string-list-files", Separator = " ", CommandLineValueSeparator = ";")]
    ITaskItem[] FileListArray { get => new ITaskItem[] { new TaskItem("/absolute/unix/style/path"), new TaskItem("c:\\windows\\style\\path") }; }

    [SwitchStringList(Subtype = "file", Switch = "--string-list-datasource", Separator = " ")]
    [SwitchDataSource(ItemType = "Item")]
    ITaskItem[] FileListDataSourceArray { get => new ITaskItem[] { new TaskItem("c:\\file path\\with.spaces") }; }

    protected override string ToolName => GetType().Name;

    protected override string GenerateFullPathToTool() => "cmd.exe";

    protected override string GenerateCommandLineCommands()
    {
      var rule = new Rule();

      var parser = new CommandLineGenerator();

      var parameterValues = new Dictionary<string, object>();

      foreach (var attr in SwitchAttributes)
      {
        rule.Properties.Add(attr.Value.GetProperty());

        parameterValues.Add(attr.Key, attr.Value.GetValue());
      }

      var builder = parser.GenerateCommandLine(rule, parameterValues);

      Trace.WriteLine(builder.ToString());

      return builder.ToString();
    }

    internal string TestGenerateCommandLineCommands() => GenerateCommandLineCommands();
  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  [TestClass]
  public class AttributedToolTaskTests
  {
    [TestMethod]
    public void PropertyToCommandLineEvaluation()
    {
      var attributedToolTask = new AttributedToolTask();

      var mock = new Mock<IBuildEngine>();
      attributedToolTask.BuildEngine = mock.Object;

      Assert.IsTrue(attributedToolTask.Execute());
      Assert.AreEqual("--true --false --enum enum-value --integer 0 --string-valid StringValidString --string-list-files /absolute/unix/style/path;c:\\windows\\style\\path --string-list-datasource \"c:\\file path\\with.spaces\"", attributedToolTask.TestGenerateCommandLineCommands());
    }
  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}
