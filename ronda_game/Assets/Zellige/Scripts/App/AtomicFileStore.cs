using System;
using System.IO;
using System.Text;

namespace Zellige.App
{
    /// <summary>Result of reading one stored file.</summary>
    public enum ReadStatus
    {
        Found,
        /// <summary>No file: first launch, or the data was never written.</summary>
        Missing,
        /// <summary>The file exists but could not be read (I/O or permission error). It must not be overwritten.</summary>
        Unreadable
    }

    /// <summary>
    /// Reads and writes small text files in one folder without ever leaving a half-written file in
    /// place of a good one. A write goes to "name.json.tmp" (flushed to disk), the current file is
    /// moved to "name.json.old", the new file is moved into place, then the old copy is deleted.
    /// If the app dies between those steps, the next read restores the last complete file.
    /// Plain System.IO only, so it behaves the same in the Editor, on desktop and on Android.
    /// </summary>
    public sealed class AtomicFileStore
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public string Directory { get; }

        public AtomicFileStore(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("A folder is required.", nameof(directory));
            Directory = directory;
        }

        public string PathFor(string name) => Path.Combine(Directory, name + ".json");

        public ReadStatus TryRead(string name, out string text)
        {
            text = null;
            string path = PathFor(name);
            try
            {
                RecoverInterruptedWrite(path);
                if (!File.Exists(path)) return ReadStatus.Missing;
                text = File.ReadAllText(path, Utf8NoBom);
                return ReadStatus.Found;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            {
                return ReadStatus.Unreadable;
            }
        }

        /// <summary>Replaces the file's content. On failure the previous file is left (or put back) in place.</summary>
        public bool TryWrite(string name, string text)
        {
            string path = PathFor(name);
            string temp = path + ".tmp";
            string old = path + ".old";
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                byte[] bytes = Utf8NoBom.GetBytes(text ?? string.Empty);
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(path, old);
                }
                File.Move(temp, path);
                TryDelete(old);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            {
                TryDelete(temp);
                try
                {
                    if (!File.Exists(path) && File.Exists(old)) File.Move(old, path);
                }
                catch (Exception restore) when (restore is IOException || restore is UnauthorizedAccessException)
                {
                    // The next read retries the restore (RecoverInterruptedWrite).
                }
                return false;
            }
        }

        public bool Exists(string name) => File.Exists(PathFor(name));

        /// <summary>
        /// Finishes or rolls back a write that was interrupted:
        /// no file but an ".old" copy → restore it (the last complete file);
        /// a file and an ".old" copy → the new file is complete, drop the copy;
        /// a ".tmp" file → an unfinished write, drop it.
        /// </summary>
        private static void RecoverInterruptedWrite(string path)
        {
            string old = path + ".old";
            if (!File.Exists(path) && File.Exists(old)) File.Move(old, path);
            else if (File.Exists(old)) File.Delete(old);
            TryDelete(path + ".tmp");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // Left for the next RecoverInterruptedWrite.
            }
        }
    }
}
