using System.Numerics;
using ImGuiNET;

namespace nb3D.UI;

public class DevConsole : ILogger
{
    private enum ConsoleLogEntryLevel
    {
        Info,
        Debug,
        Warning,
        Error
    }

    private readonly struct ConsoleLogEntry(DateTime timestamp, string message, ConsoleLogEntryLevel level)
    {
        public readonly DateTime Timestamp = timestamp;
        public readonly string Message = message;
        public readonly ConsoleLogEntryLevel Level = level;
    }

    private readonly byte[] m_commandBuffer = new byte[256];
    private readonly List<ConsoleLogEntry> m_logEntries = new();
    private readonly Dictionary<ConsoleLogEntryLevel, uint> m_levelColors = new()
    {
        { ConsoleLogEntryLevel.Error, 0xFF0000FF },
        { ConsoleLogEntryLevel.Debug, 0xFFFFFFFF },
        { ConsoleLogEntryLevel.Info, 0xFFFFFFFF },
        { ConsoleLogEntryLevel.Warning, 0xFF00A5FF },
    };

    public void Render()
    {
        ImGui.Begin("Developer console");
        {
            RenderLogWindow();
            RenderCommandInput();
        }
        ImGui.End();
    }

    public void Info(string message) => AddLogEntry(message, ConsoleLogEntryLevel.Info);

    public void Debug(string message) => AddLogEntry(message, ConsoleLogEntryLevel.Debug);

    public void Warning(string message) => AddLogEntry(message, ConsoleLogEntryLevel.Warning);

    public void Error(string message) => AddLogEntry(message, ConsoleLogEntryLevel.Error);

    private void AddLogEntry(string message, ConsoleLogEntryLevel level)
    {
        m_logEntries.Add(new ConsoleLogEntry(DateTime.Now, message, level));
    }

    private void RenderCommandInput()
    {
        var inputTextFlags = ImGuiInputTextFlags.EnterReturnsTrue;
        var reclaimFocus = false;

        if (ImGui.InputText("Command", m_commandBuffer, (uint)m_commandBuffer.Length, inputTextFlags))
        {
            reclaimFocus = true;
            Array.Clear(m_commandBuffer);
        }

        if (reclaimFocus)
        {
            ImGui.SetKeyboardFocusHere(-1);
        }
    }

    private void RenderLogWindow()
    {
        var footerHeightToReserve = ImGui.GetStyle().ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing();

        if (ImGui.BeginChild("ScrollRegion##", new Vector2(0, -footerHeightToReserve)))
        {
            foreach (var logEntry in m_logEntries)
            {
                RenderLogEntry(logEntry);
            }
        }

        ImGui.EndChild();
    }

    private void RenderLogEntry(ConsoleLogEntry logEntry)
    {
        var timestampStr = $"{logEntry.Timestamp.Hour:00}:{logEntry.Timestamp.Minute:00}:{logEntry.Timestamp.Second:00}";
        var message = $"[{timestampStr}][{logEntry.Level.ToString().ToUpper()}] {logEntry.Message}";
        var color = m_levelColors[logEntry.Level];

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text(message);
        ImGui.PopStyleColor();
    }
}