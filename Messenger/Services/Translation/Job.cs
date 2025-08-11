using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Messenger.Services.Translation;

public sealed unsafe class Job : IDisposable
{
    private HANDLE _handle;
    private bool _disposed;

    public Job(string? name = null)
    {
        if(name is null)
        {
            _handle = FXWindows.CreateJobObject(null, null);
        }
        else
        {
            fixed(char* pName = name)
            {
                _handle = FXWindows.CreateJobObject(null, (ushort*)pName);
            }
        }

        if(_handle == default)
        {
            throw new Win32Exception((int)FXWindows.GetLastError());
        }

        JOBOBJECT_EXTENDED_LIMIT_INFORMATION extendedInfo = default;
        extendedInfo.BasicLimitInformation.LimitFlags = JOB.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

        if(FXWindows.SetInformationJobObject(
                _handle,
                JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                &extendedInfo,
                (uint)sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION)) == 0)
        {
            throw new Win32Exception((int)FXWindows.GetLastError());
        }
    }

    public void AddProcess(Process process)
    {
        if(FXWindows.AssignProcessToJobObject(_handle, (HANDLE)process.Handle) == 0)
        {
            throw new Win32Exception((int)FXWindows.GetLastError());
        }
    }

    public void Dispose()
    {
        if(!_disposed)
        {
            FXWindows.CloseHandle(_handle);
            _disposed = true;
        }
    }
}