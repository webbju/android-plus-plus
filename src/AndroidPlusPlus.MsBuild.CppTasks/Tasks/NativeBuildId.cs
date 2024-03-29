﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AndroidPlusPlus.MsBuild.Common;
using AndroidPlusPlus.MsBuild.Common.Attributes;
using Microsoft.Build.Framework;
using System;
using System.Reflection;
using System.Resources;
using System.Text;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.CppTasks.Tasks
{

  public class NativeBuildId : TrackedToolTask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly StringBuilder m_readElfOutput = new StringBuilder();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public NativeBuildId()
      : base(new ResourceManager("AndroidPlusPlus.MsBuild.CppTasks.Properties.Resources", Assembly.GetExecutingAssembly()))
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [SwitchEnum(Switch = "--hex-dump", Separator = "=")]
    [SwitchEnumValue(Name = "BuildId", Switch = ".note.gnu.build-id")]
    public string HexDumpBuildId { get; set; } = "BuildId";

    [Required]
    [SwitchString(Subtype = "file", IsRequired = true)]
    public ITaskItem ElfInput { get; set; }

    [Output]
    public string BuildId { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool ValidateParameters()
    {
      InputFiles = new ITaskItem [] { ElfInput };

      return base.ValidateParameters();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override int PostExecuteTool(int exitCode, string responseFileCommands, string commandLineCommands)
    {
      exitCode = base.PostExecuteTool(exitCode, responseFileCommands, commandLineCommands);

      if (exitCode == 0)
      {
        //
        // Parse readelf tool output to identify the build-id from a hex section dump.
        //

        /*

          Hex dump of section '.note.gnu.build-id':
          0x00000134 04000000 14000000 03000000 474e5500 ............GNU.
          0x00000144 ab9455ce ade577bb 423edaa0 e986585b ..U...w.B>....X[
          0x00000154 900c47cd                            ..G.

        */

        StringBuilder buildIdBuilder = new StringBuilder(40);

        string[] readElfOutputLines = m_readElfOutput.ToString().Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 2; i < readElfOutputLines.Length; ++i) // Skip the first 3 lines of output
        {
          string santisedLine = readElfOutputLines[i].TrimStart(new char[] { ' ' });

          for (int wordId = 0; wordId < 4; ++wordId)
          {
            string longWord = santisedLine.Substring(11 + (9 * wordId), 8);

            if (!string.IsNullOrWhiteSpace(longWord))
            {
              buildIdBuilder.Append(longWord);
            }
          }
        }

        BuildId = buildIdBuilder.ToString();

        if (string.IsNullOrWhiteSpace(BuildId))
        {
          Log.LogWarning("Failed to identify build-id.");
        }
      }

      return exitCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void LogEventsFromTextOutput (string singleLine, MessageImportance messageImportance)
    {
      base.LogEventsFromTextOutput(singleLine, messageImportance);

      m_readElfOutput.AppendLine (singleLine);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

}
