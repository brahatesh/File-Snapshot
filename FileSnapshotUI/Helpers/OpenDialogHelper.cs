using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace FileSnapshotUI.Helpers {
    public static class OpenDialogHelper {
        /// <summary>
        /// Opens a native Win32 File Open Dialog and returns the selected file path.
        /// Returns null if the user cancels or an error occurs.
        /// </summary>
        public static unsafe string? PickSingleFile(Window hostWindow) {
            // Generate standard local copies of the Guids
            Guid clsid = typeof(FileOpenDialog).GUID;
            Guid iid = typeof(IFileOpenDialog).GUID;

            // Use '&' to extract the raw Guid* memory addresses from variables
            HRESULT hr = PInvoke.CoCreateInstance(
                &clsid,
                null,
                Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER,
                &iid,
                out object dialogObj);

            if (hr.Failed) return null;

            // Extract the clean interface assignment context
            IFileOpenDialog dialog = (IFileOpenDialog)dialogObj;

            try {
                // Extract your WinUI 3 window context handler
                IntPtr hwndHandle = WinRT.Interop.WindowNative.GetWindowHandle(hostWindow);
                HWND parentHwnd = new HWND(hwndHandle);

                // Blocks thread execution until closed/dismissed
                dialog.Show(parentHwnd);

                // If execution proceeds past Show(), retrieve the chosen item reference data
                dialog.GetResult(out IShellItem item);

                if (item != null) {
                    // Request the clean path structure from the file system
                    item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out PWSTR pFilePath);

                    // Read into a standard managed framework string configuration
                    string selectedFilePath = pFilePath.ToString();

                    // Clean up memory structures handled inside the Win32 subsystem layer
                    PInvoke.CoTaskMemFree(pFilePath);

                    System.Diagnostics.Debug.WriteLine($"Selected File Path: {selectedFilePath}");

                    return selectedFilePath;
                }
            }
            catch (COMException comEx) when ((uint)comEx.HResult == 0x800704C7) {
                // Explicitly catch and bypass the native Win32 ERROR_CANCELLED (User closed/cancelled)
                System.Diagnostics.Debug.WriteLine("User closed or cancelled the dialog window.");
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"General dialog operational failure: {ex.Message}");
            }

            // Return null if cancelled or failed
            return null;
        }

        public static unsafe string? PickSingleFolder(Window hostWindow) {
            Guid clsid = typeof(FileOpenDialog).GUID;
            Guid iid = typeof(IFileOpenDialog).GUID;

            HRESULT hr = PInvoke.CoCreateInstance(
                &clsid,
                null,
                Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER,
                &iid,
                out object dialogObj);

            if (hr.Failed) return null;

            IFileOpenDialog dialog = (IFileOpenDialog)dialogObj;

            try {
                // Retrieve the default options of the dialog
                dialog.GetOptions(out FILEOPENDIALOGOPTIONS options);

                // Append the FOS_PICKFOLDERS flag using a bitwise OR, 
                // which transforms the File Picker into a Folder Picker
                dialog.SetOptions(options | FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

                IntPtr hwndHandle = WinRT.Interop.WindowNative.GetWindowHandle(hostWindow);
                HWND parentHwnd = new HWND(hwndHandle);

                dialog.Show(parentHwnd);

                dialog.GetResult(out IShellItem item);

                if (item != null) {
                    item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out PWSTR pFolderPath);
                    string selectedFolderPath = pFolderPath.ToString();
                    PInvoke.CoTaskMemFree(pFolderPath);

                    System.Diagnostics.Debug.WriteLine($"Selected Folder Path: {selectedFolderPath}");
                    return selectedFolderPath;
                }
            }
            catch (COMException comEx) when ((uint)comEx.HResult == 0x800704C7) {
                System.Diagnostics.Debug.WriteLine("User closed or cancelled the folder dialog.");
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Folder dialog operational failure: {ex.Message}");
            }

            return null;
        }
    }
}