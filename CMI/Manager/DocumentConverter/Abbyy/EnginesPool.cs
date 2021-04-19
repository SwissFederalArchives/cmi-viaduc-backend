// © 2017 ABBYY Production LLC
// SAMPLES code is property of ABBYY, exclusive rights are reserved. 
//
// DEVELOPER is allowed to incorporate SAMPLES into his own APPLICATION and modify it under 
// the  terms of  License Agreement between  ABBYY and DEVELOPER.
// ABBYY FineReader Engine 11/12 Sample

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using FREngine;
using Serilog;

namespace CMI.Manager.DocumentConverter.Abbyy
{
    public interface IEnginesPool : IDisposable
    {
        IEngine GetEngine();
        void ReleaseEngine(IEngine engine, bool isRecycleRequired);
        bool ShouldRestartEngine(Exception exception);
    }

    public class DummyEnginePool: IEnginesPool
    {
        public void Dispose()
        {
            
        }

        public IEngine GetEngine()
        {
            return null;
        }

        public void ReleaseEngine(IEngine engine, bool isRecycleRequired)
        {
            // Nothing to do
        }

        public bool ShouldRestartEngine(Exception exception)
        {
            return false;
        }
    }

    public class EnginesPool : IEnginesPool
    {
        private readonly string projectId;
        private EngineHolder[] engineHolders;
        private readonly object engineHolderLock;
        private readonly int waitingTimeout;
        private readonly Semaphore semaphore;
        private readonly object usageCountLock;
        private int autoRecycleUsageCount;


        public EnginesPool(int enginesCount, string developerSerialNumber, int waitingEngineTimeout)
        {
            projectId = developerSerialNumber;
            engineHolders = new EngineHolder [enginesCount];
            for (var i = 0; i < enginesCount; i++)
            {
                engineHolders[i] = new EngineHolder(projectId);
            }

            engineHolderLock = new object();
            waitingTimeout = waitingEngineTimeout;
            semaphore = new Semaphore(enginesCount, enginesCount);
            usageCountLock = new object();
            autoRecycleUsageCount = 0; // Zero means that auto recycling will not be performed
        }

        public int AutoRecycleUsageCount
        {
            get
            {
                lock (usageCountLock)
                {
                    return autoRecycleUsageCount;
                }
            }
            set
            {
                lock (usageCountLock)
                {
                    autoRecycleUsageCount = value;
                }
            }
        }

        public void Dispose()
        {
            if (engineHolders != null)
            {
                for (var i = 0; i < engineHolders.Length; i++)
                {
                    engineHolders[i].Dispose();
                    engineHolders[i] = null;
                }

                engineHolders = null;
            }
        }

        public IEngine GetEngine()
        {
            Trace.Assert(semaphore.WaitOne(waitingTimeout, false), "Waiting engine timeout exceeded.");

            for (var i = 0; i < engineHolders.Length; i++)
            {
                lock (engineHolderLock)
                {
                    if (!engineHolders[i].IsEngineLocked())
                    {
                        Log.Information("Getting Abbyy engine with id {i}", i);
                        return engineHolders[i].GetLockedEngine();
                    }
                }
            }

            // No free engine has been found, and we have a valid installation
            // If we don't have an Abbyy installation, the engineHolders array is empty
            Trace.Assert(engineHolders.Length > 0);
            return null;
        }

        public void ReleaseEngine(IEngine engine, bool isRecycleRequired)
        {
            for (var i = 0; i < engineHolders.Length; i++)
            {
                if (engineHolders[i].ContainsEngine(engine))
                {
                    try
                    {
                        ReleaseEngine(i, isRecycleRequired);
                    }
                    finally
                    {
                        semaphore.Release();
                    }

                    return;
                }
            }

            // The engine haven't been found
            Trace.Assert(false);
        }

        public bool ShouldRestartEngine(Exception exception)
        {
            COMException comException = exception as COMException;
            if (comException != null)
            {
                uint hResult = (uint)comException.ErrorCode;
                // The RPC server is unavailable because of a crash of the surrogate process
                // You should restart the engine
                if (hResult == 0x800706BA)
                {
                    return true;
                }
                // This is RPC_E_SERVERFAULT because of a structural exception in the surrogate process
                // Most probably now the engine is in the undefined state and you should restart it
                if (hResult == 0x80010105)
                {
                    return true;
                }
            }
            OutOfMemoryException outOfMemoryException = exception as OutOfMemoryException;
            if (outOfMemoryException != null)
            {
                return true;
            }
            return false;
        }

        #region Implementation

        private void ReleaseEngine(int engineIndex, bool isRecycleRequired)
        {
            lock (engineHolderLock)
            {
                var isAutoRecycleRequired = AutoRecycleUsageCount != 0
                                            && AutoRecycleUsageCount <= engineHolders[engineIndex].GetEngineUsageCount();

                Log.Information("Releasing Abbyy engine with id {engineIndex}", engineIndex);
                engineHolders[engineIndex].UnlockEngine();
                if (isRecycleRequired || isAutoRecycleRequired)
                {
                    Log.Information("Recycling Abbyy engine with id {engineIndex}", engineIndex);
                    engineHolders[engineIndex].Dispose();
                    engineHolders[engineIndex] = new EngineHolder(projectId);
                }
            }
        }

        private class EngineHolder : IDisposable
        {
            private IEngine engine;

            private IEngineLoader engineLoader;
            private int engineUsageCount;
            private bool isEngineLocked;
            private Process process;

            public EngineHolder(string projectId)
            {
                try
                {
                    engineLoader = new OutprocLoader();

                    // When a process finishes normally, all COM objects are released and work processes hosting these objects also finish 
                    // normally. If the process terminates abnormally (hard exception or manually killed) the COM objects are not released 
                    // and work processes will remain loaded. To address this issue and make your server more robust you can make 
                    // each work process watch if its parent process is still alive and terminate if not.
                    var processControl = (IHostProcessControl) engineLoader;
                    processControl.SetClientProcessId(Process.GetCurrentProcess().Id);

                    process = Process.GetProcessById(processControl.ProcessId);

                    engine = engineLoader.GetEngineObject(projectId);
                }
                catch (COMException exception)
                {
                    var hResult = (uint) exception.ErrorCode;
                    if (hResult == 0x80070005)
                    {
                        // To use LocalServer under a special account you must add this account to 
                        // the COM-object's launch permissions (using DCOMCNFG or OLE/COM object viewer)
                        throw new Exception(@"Launch permission for the work-process COM-object is not granted.
                            Use DCOMCNFG to change security settings for the object. (" + exception.Message + ")");
                    }

                    throw;
                }

                isEngineLocked = false;
                engineUsageCount = 0;
            }

            public void Dispose()
            {
                engine = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (engineLoader != null)
                {
                    engineLoader.ExplicitlyUnload();
                    engineLoader = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                if (!process.WaitForExit(5000))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                        // The process could exit between a decision to kill it and this code
                        // Skip this error
                    }
                }

                process.Dispose();
                process = null;
            }

            public IEngine GetLockedEngine()
            {
                isEngineLocked = true;
                engineUsageCount++;
                return engine;
            }

            public void UnlockEngine()
            {
                isEngineLocked = false;
            }

            public bool IsEngineLocked()
            {
                return isEngineLocked;
            }

            public bool ContainsEngine(IEngine value)
            {
                return engine == value;
            }

            public int GetEngineUsageCount()
            {
                return engineUsageCount;
            }
        }

        #endregion
    }
}