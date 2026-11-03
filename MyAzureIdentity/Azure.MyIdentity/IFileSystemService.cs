// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

// Hack: Use a separate namespace so the instrumented library can coexist with Azure.Identity.
namespace Azure.MyIdentity
{
    internal interface IFileSystemService
    {
        bool FileExists(string path);
        string ReadAllText(string path);
        FileStream GetFileStream(string path);
    }
}
