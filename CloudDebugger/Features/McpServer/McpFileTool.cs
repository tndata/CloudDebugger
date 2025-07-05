using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CloudDebugger.Features.McpServer;


public class WriteFileArgs
{
    [Description("The file path to write to")]
    public string Path { get; set; } = "";

    [Description("The content to write to the file")]
    public string Content { get; set; } = "";
}

[McpServerToolType]
public static class McpFileTools
{
    // Constants
    private const string ErrorPathEmpty = "Path cannot be empty";
    private const string ErrorAccessDenied = "Access denied";
    private const string ErrorIOOperation = "IO error";
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    // Exception type names
    private const string UnauthorizedAccessExceptionName = "UnauthorizedAccessException";
    private const string IOExceptionName = "IOException";
    private const string DirectoryNotFoundExceptionName = "DirectoryNotFoundException";

    [McpServerTool(Name = "list_files")]
    [Description("List all files and directories in the specified directory path")]
    public static object ListFiles(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return CreateErrorResponse(ErrorPathEmpty, path);
            }

            if (!Directory.Exists(path))
            {
                return CreateErrorResponse($"Directory does not exist: {path}", path);
            }

            var entries = Directory.EnumerateFileSystemEntries(path)
                .Select(entry =>
                {
                    var isFile = File.Exists(entry);
                    var fileInfo = isFile ? new FileInfo(entry) : null;

                    return new
                    {
                        name = Path.GetFileName(entry),
                        full_path = entry,
                        is_directory = !isFile,
                        is_file = isFile,
                        size = fileInfo?.Length ?? 0,
                        last_modified = FormatDateTime(isFile ? File.GetLastWriteTime(entry) : Directory.GetLastWriteTime(entry))
                    };
                })
                .ToList();

            return new
            {
                success = true,
                path,
                total_entries = entries.Count,
                entries
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateExceptionResponse(ErrorAccessDenied, ex, path, UnauthorizedAccessExceptionName);
        }
        catch (Exception ex)
        {
            return CreateExceptionResponse("Failed to list files", ex, path);
        }
    }

    [McpServerTool(Name = "read_file")]
    [Description("Read the complete contents of a text file")]
    public static object ReadFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return CreateErrorResponse(ErrorPathEmpty, path);
            }

            if (!File.Exists(path))
            {
                return CreateErrorResponse($"File does not exist: {path}", path);
            }

            var content = File.ReadAllText(path);
            var fileInfo = new FileInfo(path);

            return new
            {
                success = true,
                path,
                content,
                size_bytes = fileInfo.Length,
                last_modified = FormatDateTime(fileInfo.LastWriteTime),
                character_count = content.Length,
                line_count = CountLines(content)
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateExceptionResponse(ErrorAccessDenied, ex, path, UnauthorizedAccessExceptionName);
        }
        catch (IOException ex)
        {
            return CreateExceptionResponse($"{ErrorIOOperation} reading file", ex, path, IOExceptionName);
        }
        catch (Exception ex)
        {
            return CreateExceptionResponse("Failed to read file", ex, path);
        }
    }

    [McpServerTool(Name = "write_file")]
    [Description("Create a new file or overwrite an existing file with the specified content")]
    public static object WriteFile(WriteFileArgs args)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(args.Path))
            {
                return CreateErrorResponse(ErrorPathEmpty, args.Path);
            }

            args.Content ??= ""; // Allow empty files

            // Ensure directory exists
            var directory = Path.GetDirectoryName(args.Path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var existedBefore = File.Exists(args.Path);
            var previousSize = existedBefore ? new FileInfo(args.Path).Length : 0;

            File.WriteAllText(args.Path, args.Content);

            var fileInfo = new FileInfo(args.Path);

            return new
            {
                success = true,
                path = args.Path,
                bytes_written = args.Content.Length,
                file_size_bytes = fileInfo.Length,
                character_count = args.Content.Length,
                line_count = CountLines(args.Content),
                created_new_file = !existedBefore,
                previous_size = previousSize,
                last_modified = FormatDateTime(fileInfo.LastWriteTime)
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateExceptionResponse(ErrorAccessDenied, ex, args.Path, UnauthorizedAccessExceptionName);
        }
        catch (DirectoryNotFoundException ex)
        {
            return CreateExceptionResponse("Directory not found", ex, args.Path, DirectoryNotFoundExceptionName);
        }
        catch (IOException ex)
        {
            return CreateExceptionResponse($"{ErrorIOOperation} writing file", ex, args.Path, IOExceptionName);
        }
        catch (Exception ex)
        {
            return CreateExceptionResponse("Failed to write file", ex, args.Path);
        }
    }

    [McpServerTool(Name = "delete_file")]
    [Description("Delete a file from the filesystem")]
    public static object DeleteFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return CreateErrorResponse(ErrorPathEmpty, path);
            }

            if (!File.Exists(path))
            {
                return CreateErrorResponse($"File does not exist: {path}", path);
            }

            var fileInfo = new FileInfo(path);
            var fileSize = fileInfo.Length;
            var lastModified = fileInfo.LastWriteTime;

            File.Delete(path);

            return new
            {
                success = true,
                path,
                message = "File deleted successfully",
                deleted_file_size = fileSize,
                deleted_file_last_modified = FormatDateTime(lastModified)
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateExceptionResponse(ErrorAccessDenied, ex, path, UnauthorizedAccessExceptionName);
        }
        catch (IOException ex)
        {
            return CreateExceptionResponse($"{ErrorIOOperation} deleting file", ex, path, IOExceptionName);
        }
        catch (Exception ex)
        {
            return CreateExceptionResponse("Failed to delete file", ex, path);
        }
    }

    [McpServerTool(Name = "get_file_info")]
    [Description("Get detailed information about a file or directory")]
    public static object GetFileInfo(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return CreateErrorResponse(ErrorPathEmpty, path);
            }

            var isFile = File.Exists(path);
            var isDirectory = Directory.Exists(path);

            if (!isFile && !isDirectory)
            {
                return CreateErrorResponse($"Path does not exist: {path}", path);
            }

            if (isFile)
            {
                var fileInfo = new FileInfo(path);
                return new
                {
                    success = true,
                    path,
                    type = "file",
                    name = fileInfo.Name,
                    size_bytes = fileInfo.Length,
                    created = FormatDateTime(fileInfo.CreationTime),
                    last_modified = FormatDateTime(fileInfo.LastWriteTime),
                    last_accessed = FormatDateTime(fileInfo.LastAccessTime),
                    extension = fileInfo.Extension,
                    is_readonly = fileInfo.IsReadOnly,
                    attributes = fileInfo.Attributes.ToString()
                };
            }
            else
            {
                var dirInfo = new DirectoryInfo(path);
                var files = dirInfo.GetFiles();
                var directories = dirInfo.GetDirectories();

                return new
                {
                    success = true,
                    path,
                    type = "directory",
                    name = dirInfo.Name,
                    created = FormatDateTime(dirInfo.CreationTime),
                    last_modified = FormatDateTime(dirInfo.LastWriteTime),
                    last_accessed = FormatDateTime(dirInfo.LastAccessTime),
                    file_count = files.Length,
                    subdirectory_count = directories.Length,
                    total_entries = files.Length + directories.Length,
                    attributes = dirInfo.Attributes.ToString()
                };
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return CreateExceptionResponse(ErrorAccessDenied, ex, path, UnauthorizedAccessExceptionName);
        }
        catch (Exception ex)
        {
            return CreateExceptionResponse("Failed to get file info", ex, path);
        }
    }

    // Helper methods
    private static object CreateErrorResponse(string error, string path)
    {
        return new
        {
            success = false,
            error,
            path
        };
    }

    private static object CreateExceptionResponse(string baseMessage, Exception ex, string path, string? exceptionTypeName = null)
    {
        return new
        {
            success = false,
            error = $"{baseMessage}: {ex.Message}",
            path,
            exception_type = exceptionTypeName ?? ex.GetType().Name
        };
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString(DateTimeFormat);
    }

    private static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var count = 1;
        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n')
                count++;
        }
        return count;
    }
}