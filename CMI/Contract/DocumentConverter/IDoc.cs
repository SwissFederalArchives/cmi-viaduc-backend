using System;
using System.IO;

namespace CMI.Contract.DocumentConverter
{
    public interface IDoc : IDisposable
    {
        Stream Stream { get; }
        string Identifier { get; }
        string Extension { get; }
        string FileName { get; }
    }
}