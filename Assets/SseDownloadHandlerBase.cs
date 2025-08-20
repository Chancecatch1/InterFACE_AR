using System.Buffers;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public abstract class SseDownloadHandlerBase : DownloadHandlerScript
{
    private readonly StringBuilder _currentLine = new();

    protected SseDownloadHandlerBase(byte[] buffer) : base(buffer)    { }

    protected abstract void OnNewLineReceived(string line);

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        for (var i = 0; i < dataLength; i++)
        {
            var b = data[i];
            if (b == '\n')
            {
                OnNewLineReceived(_currentLine.ToString());
                _currentLine.Clear();
            }
            else
            {
                _currentLine.Append((char) b);
            }
        }

        return true;
    }

    protected override void CompleteContent()
    {
        if(_currentLine.Length > 0)
            OnNewLineReceived(_currentLine.ToString());
    }
}