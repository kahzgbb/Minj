using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
    private static IntPtr allocatedMemory = IntPtr.Zero;

    static void Main()
    {
        try
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                PrintBlackhatLogoAndMenu();
                Console.Title = "MINJ - [ github.com/kahzgbb ]";

                string option = Console.ReadLine();

                if (option == "1")
                {
                    InjectString();
                }
                else if (option == "2")
                {
                    RemoveString();
                }
                else if (option == "3")
                {
                    RemoveStringList();
                }
                else if (option == "4")
                {
                    AddStringList();
                }
                else if (option == "5")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid option! Press Enter to try again...");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private static void PrintBlackhatLogoAndMenu()
    {
        string logo = @" 


                                            ███    ███ ██ ███    ██      ██ 
                                            ████  ████ ██ ████   ██      ██ 
                                            ██ ████ ██ ██ ██ ██  ██      ██ 
                                            ██  ██  ██ ██ ██  ██ ██ ██   ██ 
                                            ██      ██ ██ ██   ████  █████  
";

        string menu = @"
                                    +--------------------------------------------+
                                    |              Memory Inject SI              |
                                    |                Menu Options                |
                                    +--------------------------------------------+
                                    |   1 - Inject String                        |
                                    |   2 - Remove String                        |
                                    |   3 - Remove List of Strings               |
                                    |   4 - Add List of Strings                  |
                                    |   5 - Exit                                 |
                                    +--------------------------------------------+

        >  ";

        int windowWidth = Console.WindowWidth;
        int logoWidth = logo.Split('\n')[0].Length;
        int menuWidth = menu.Split('\n')[0].Length;

        int centerLogoPos = Math.Max((windowWidth - logoWidth) / 2, 0);
        int centerMenuPos = Math.Max((windowWidth - menuWidth) / 2, 0);

        Console.SetCursorPosition(centerLogoPos, 0);
        Console.WriteLine(logo);

        Console.SetCursorPosition(centerMenuPos, logo.Split('\n').Length);
        Console.WriteLine(menu);
    }

    private static Process GetTargetProcess(int pid)
    {
        try
        {
            return Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static void InjectString()
    {
        Console.Write("Enter the PID of the target process: ");
        string pidInput = Console.ReadLine();

        if (string.IsNullOrEmpty(pidInput))
        {
            Console.WriteLine("Invalid PID input. Please try again.");
            return;
        }

        int pid;
        if (!int.TryParse(pidInput, out pid))
        {
            Console.WriteLine("Invalid PID. Please enter a valid number.");
            return;
        }

        Process targetProcess = GetTargetProcess(pid);
        if (targetProcess == null)
        {
            Console.WriteLine("Process not found!");
            return;
        }

        Console.Write("Enter the string to inject: ");
        string injectedString = Console.ReadLine();
        if (string.IsNullOrEmpty(injectedString))
        {
            Console.WriteLine("The string cannot be empty.");
            return;
        }

        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
        if (processHandle == IntPtr.Zero)
        {
            Console.WriteLine("Unable to open the process.");
            return;
        }

        try
        {
            allocatedMemory = VirtualAllocEx(processHandle, IntPtr.Zero, (uint)(injectedString.Length + 1), 0x1000 | 0x2000, 0x04);
            if (allocatedMemory == IntPtr.Zero)
            {
                Console.WriteLine("Failed to allocate memory in the process.");
                return;
            }

            byte[] buffer = Encoding.ASCII.GetBytes(injectedString);

            if (WriteProcessMemory(processHandle, allocatedMemory, buffer, (uint)buffer.Length, out uint bytesWritten))
            {
                Console.WriteLine($"String '{injectedString}' injected into process {targetProcess.ProcessName} (PID: {pid}).");
            }
            else
            {
                Console.WriteLine("Failed to write into the process memory.");
                return;
            }
        }
        finally
        {
            CloseHandle(processHandle);
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private static void RemoveString()
    {
        Console.Write("Enter the PID of the target process: ");
        string pidInput = Console.ReadLine();

        if (string.IsNullOrEmpty(pidInput))
        {
            Console.WriteLine("Invalid PID input. Please try again.");
            return;
        }

        int pid;
        if (!int.TryParse(pidInput, out pid))
        {
            Console.WriteLine("Invalid PID. Please enter a valid number.");
            return;
        }

        Process targetProcess = GetTargetProcess(pid);
        if (targetProcess == null)
        {
            Console.WriteLine("Process not found!");
            return;
        }

        Console.Write("Enter the string to remove: ");
        string removedString = Console.ReadLine();

        if (string.IsNullOrEmpty(removedString))
        {
            Console.WriteLine("The string cannot be empty.");
            return;
        }

        if (allocatedMemory == IntPtr.Zero)
        {
            Console.WriteLine("No string has been injected to remove.");
            return;
        }

        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
        if (processHandle == IntPtr.Zero)
        {
            Console.WriteLine("Unable to open the process.");
            return;
        }

        try
        {
            if (VirtualFreeEx(processHandle, allocatedMemory, 0, 0x8000))
            {
                Console.WriteLine($"String '{removedString}' removed from process {targetProcess.ProcessName} (PID: {pid}).");
                allocatedMemory = IntPtr.Zero;
            }
            else
            {
                Console.WriteLine("Failed to release the memory.");
            }
        }
        finally
        {
            CloseHandle(processHandle);
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private static void RemoveStringList()
    {
        Console.Write("Enter the PID of the target process: ");
        string pidInput = Console.ReadLine();

        if (string.IsNullOrEmpty(pidInput))
        {
            Console.WriteLine("Invalid PID input. Please try again.");
            return;
        }

        int pid;
        if (!int.TryParse(pidInput, out pid))
        {
            Console.WriteLine("Invalid PID. Please enter a valid number.");
            return;
        }

        Process targetProcess = GetTargetProcess(pid);
        if (targetProcess == null)
        {
            Console.WriteLine("Process not found!");
            return;
        }

        Console.WriteLine("Enter the list of strings to remove (one per line):");
        Console.WriteLine("Type 'list end' to finish the list.");

        List<string> stringsToRemove = new List<string>();

        while (true)
        {
            string input = Console.ReadLine();
            if (input == "list end")
            {
                break;
            }

            stringsToRemove.Add(input);
        }

        Console.WriteLine("Removing strings from the process...");
        foreach (var str in stringsToRemove)
        {
            IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
            if (processHandle == IntPtr.Zero)
            {
                Console.WriteLine("Unable to open the process.");
                return;
            }

            try
            {
                IntPtr currentAllocatedMemory = allocatedMemory;
                if (currentAllocatedMemory != IntPtr.Zero && VirtualFreeEx(processHandle, currentAllocatedMemory, 0, 0x8000))
                {
                    Console.WriteLine($"String '{str}' removed from process {targetProcess.ProcessName} (PID: {pid}).");
                }
                else
                {
                    Console.WriteLine($"Failed to remove the string '{str}'.");
                }
            }
            finally
            {
                CloseHandle(processHandle);
            }
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private static void AddStringList()
    {
        Console.Write("Enter the PID of the target process: ");
        string pidInput = Console.ReadLine();

        if (string.IsNullOrEmpty(pidInput))
        {
            Console.WriteLine("Invalid PID input. Please try again.");
            return;
        }

        int pid;
        if (!int.TryParse(pidInput, out pid))
        {
            Console.WriteLine("Invalid PID. Please enter a valid number.");
            return;
        }

        Process targetProcess = GetTargetProcess(pid);
        if (targetProcess == null)
        {
            Console.WriteLine("Process not found!");
            return;
        }

        Console.WriteLine("Enter the list of strings to add (one per line):");
        Console.WriteLine("Type 'list end' to finish the list.");

        List<string> stringsToAdd = new List<string>();

        while (true)
        {
            string input = Console.ReadLine();
            if (input == "list end")
            {
                break;
            }

            stringsToAdd.Add(input);
        }

        Console.WriteLine("Adding strings to the process...");
        foreach (var str in stringsToAdd)
        {
            InjectString();
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }
}
