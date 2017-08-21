﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JexusManager.Features.Main
{
    using System;
    using System.ComponentModel;
    using System.Drawing.Design;
    using System.Reflection;

    using Microsoft.Web.Administration;

    internal class ApplicationPoolAdvancedSettings
    {
        public ApplicationPoolAdvancedSettings(ApplicationPool pool)
        {
            if (pool.ManagedRuntimeVersion == "v4.0")
            {
                CLRVersion = CLRVersion.V40;
            }
            else if (pool.ManagedRuntimeVersion == "v2.0")
            {
                CLRVersion = CLRVersion.V20;
            }
            else
            {
                CLRVersion = CLRVersion.NoManagedCode;
            }

            Enable32Bit = pool.Enable32BitAppOnWin64;
            Mode = pool.ManagedPipelineMode;
            Name = pool.Name;
            QueueLength = pool.QueueLength;
            Start = pool.StartMode;

            Limit = pool.Cpu.Limit;
            Action = pool.Cpu.Action;
            Interval = pool.Cpu.ResetInterval.GetTotalMinutes();
            Affinitized = pool.Cpu.SmpAffinitized;
            Mask = pool.Cpu.SmpProcessorAffinityMask;
            Mask2 = pool.Cpu.SmpProcessorAffinityMask2;

            LogEntry = new LogEntrySettings();
            ConfigurationAttributeSchema enumLog = pool.ProcessModel.Schema.AttributeSchemas["logEventOnProcessModel"];
            if (enumLog != null)
            {
                if (enumLog.GetEnumValues().Count > 1)
                {
                    LogEntry.IdleTimeout = pool.ProcessModel.LogEventOnProcessModel
                                                == ProcessModelLogEventOnProcessModel.IdleTimeout;
                }
                else
                {
                    // IMPORTANT: workaround for IIS 8 Express.
                    HideProperty(GetType(), nameof(LogEntry));
                }
            }
            else
            {
                HideProperty(GetType(), nameof(LogEntry));
                LogEntry.IdleTimeout = true;
            }

            IdleTimeout = pool.ProcessModel.IdleTimeout.GetTotalMinutes();
            if (pool.ProcessModel.Schema.AttributeSchemas["idleTimeoutAction"] != null)
            {
                IdleAction = pool.ProcessModel.IdleTimeoutAction;
            }
            else
            {
                HideProperty(GetType(), nameof(IdleAction));
            }

            MaxProcesses = pool.ProcessModel.MaxProcesses;
            Identity = pool.ProcessModel;
        }

        internal void Apply(ApplicationPool pool)
        {
            if (CLRVersion == CLRVersion.V40)
            {
                pool.ManagedRuntimeVersion = "v4.0";
            }
            else if (CLRVersion == CLRVersion.V20)
            {
                pool.ManagedRuntimeVersion = "v2.0";
            }
            else if (CLRVersion == CLRVersion.NoManagedCode)
            {
                pool.ManagedRuntimeVersion = string.Empty;
            }

            pool.Enable32BitAppOnWin64 = Enable32Bit;
            pool.ManagedPipelineMode = Mode;
            pool.QueueLength = QueueLength;
            pool.StartMode = Start;

            pool.ProcessModel.MaxProcesses = MaxProcesses;

            pool.Cpu.Limit = Limit;
            pool.Cpu.Action = pool.Cpu.Schema.AttributeSchemas["action"].GetEnumValues().GetName((long)Action)
                              != null
                                  ? Action
                                  : ProcessorAction.NoAction;
            pool.Cpu.ResetInterval = new TimeSpan(0, Interval, 0);
            pool.Cpu.SmpAffinitized = Affinitized;
            pool.Cpu.SmpProcessorAffinityMask = Mask;
            pool.Cpu.SmpProcessorAffinityMask2 = Mask2;

            var enumIdle = pool.ProcessModel.Schema.AttributeSchemas["logEventOnProcessModel"];
            if (enumIdle != null)
            {
                if (enumIdle.GetEnumValues().Count > 1)
                {
                    pool.ProcessModel.LogEventOnProcessModel = LogEntry.IdleTimeout
                                                                   ? ProcessModelLogEventOnProcessModel.IdleTimeout
                                                                   : ProcessModelLogEventOnProcessModel.None;
                }
                else
                {
                    pool.ProcessModel.LogEventOnProcessModel = ProcessModelLogEventOnProcessModel.IdleTimeout;
                }
            }

            pool.ProcessModel.IdleTimeout = new TimeSpan(0, IdleTimeout, 0);
            if (pool.ProcessModel.Schema.AttributeSchemas["idleTimeoutAction"] != null)
            {
                pool.ProcessModel.IdleTimeoutAction = IdleAction;
            }

            pool.ProcessModel.MaxProcesses = MaxProcesses;
        }

        private static void HideProperty(Type type, string name)
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(type)[name];
            BrowsableAttribute attrib = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
            FieldInfo isBrow = attrib.GetType().GetField("browsable", BindingFlags.NonPublic | BindingFlags.Instance);
            isBrow?.SetValue(attrib, false);
        }

        [Browsable(true)]
        [Category("(General)")]
        [Description("[managedRuntimeVersion] Configures the application pool to load a specific .NET CLR Version. The CLR version chosen should correspond to the appropriate version of the .NET Framework being used by your application. Select \"No Managed Code\" will cause all ASP.NET requests to fail.")]
        [DisplayName(".NET CLR Version")]
        [TypeConverter(typeof(DescriptionConverter))]
        [DefaultValue(CLRVersion.V20)]
        // ReSharper disable once InconsistentNaming
        public CLRVersion CLRVersion { get; set; }

        [Browsable(true)]
        [Category("(General)")]
        [Description("[enable32BitAppOnWin64] If set to true for an application pool on 64-bit operating system, the worker process(es) serving the application pool will be in WOW64 (Windows on Windows64) mode. Processes in WOW64 mode are 32-bit processes that load only 32-bit applications.")]
        [DisplayName("Enable 32-Bit Applications")]
        [DefaultValue(false)]
        public bool Enable32Bit { get; set; }

        [Browsable(true)]
        [Category("(General)")]
        [Description("[managedPipelineMode] Configures ASP.NET to run in Classic Mode as an ISAPI extension, or in Integrated Mode where managed code is integrated into the request processing pipeline.")]
        [DisplayName("Managed Pipeline Mode")]
        [TypeConverter(typeof(DescriptionConverter))]
        [DefaultValue(ManagedPipelineMode.Integrated)]
        public ManagedPipelineMode Mode { get; set; }

        [Browsable(true)]
        [Category("(General)")]
        [Description("[name] The application pool name is the unique identifier for the application pool.")]
        [DisplayName("Name")]
        [ReadOnly(true)]
        public string Name { get; set; }

        [Browsable(true)]
        [Category("(General)")]
        [Description("[queueLength] Maximum number of requests that HTTP.sys will queue for the application pool. When the queue is full, new requests receive a 503 \"Service Unavailable\" response.")]
        [DisplayName("Queue Length")]
        [DefaultValue((long)1000)]
        public long QueueLength { get; set; }

        [Browsable(true)]
        [Category("(General)")]
        [Description("[startMode] Configures application pool to run in On Demand Mode or Always Running Mode")]
        [TypeConverter(typeof(DescriptionConverter))]
        [DisplayName("Start Mode")]
        [DefaultValue(StartMode.OnDemand)]
        public StartMode Start { get; set; }

        [Browsable(true)]
        [Category("CPU")]
        [Description("[limit] Configures the maximum percentage of CPU time that the worker processes in an application pool are allowed to consume over a period of time as indicated by the CPU Limit Interval property. If the limit set by the CPU Limit property is exceeded, an event is written to the event log and an optional set of events can be triggered as determined by the CPU Limit Action property. Setting the value of this property to 0 disables limiting the worker processes to a percentage of CPU time.")]
        [DisplayName("Limit (percent)")]
        [DefaultValue((long)0)]
        public long Limit { get; set; }

        [Browsable(true)]
        [Category("CPU")]
        [Description("[action] If set to \"NoAction\", an event log entry is generated. If set to \"KillW3WP\", the application pool is shut down for the duration of the reset interval and an event log entry is generated. If set to \"Throttle\", the CPU consumption is limited to the value set in Limit. Limit interval is not used and an event log entry is generated. If set to \"ThrottleUnderLoad\", the CPU consumption is limited only when there is contention on CPU. Limit Interval is not used and an event log entry is generated.")]
        [TypeConverter(typeof(DescriptionConverter))]
        [DisplayName("Limit Action")]
        [DefaultValue(ProcessorAction.NoAction)]
        public ProcessorAction Action { get; set; }

        [Browsable(true)]
        [Category("CPU")]
        [Description("[resetInterval] Specifies the reset period (in minutes) for CPU monitoring and throttling limits on the application pool. When the number of minutes elapsed since the last process accounting reset equals the number specified by this property, IIS will reset the CPU timers for both the logging and limit intervals. Setting the value of this property to 0 disables CPU monitoring.")]
        [DisplayName("Limit Interval (minutes)")]
        [DefaultValue(5)]
        public int Interval { get; set; }

        [Browsable(true)]
        [Category("CPU")]
        [Description("[smpAffinitized] If true, the Processor Affinity Mask property forces the worker process(es) serving this application pool to run on specific CPUs. This enables efficient use of CPU caches on multiprocessor servers.")]
        [DisplayName("Processor Affinity Enabled")]
        [DefaultValue(false)]
        public bool Affinitized { get; set; }

        [Browsable(true)]
        [Category("CPU")]
        [Description("[smpProcessorAffinityMask] Hexadecimal mask that forces the worker process(es) for this application pool to run on a specific CPU. If processor affinity is enabled, a value of 0 will cause an error condition.")]
        [DisplayName("Processor Affinity Mask")]
        [DefaultValue(4294967295)]
        public long Mask { get; set; }

        [Browsable(true)]
        [Category("CPU")]
        [Description("[smpProcessorAffinityMask2] Specifies the high-order DWORD hexadecimal mask for 64-bit machine, that forces the worker process(es) for this application pool to run on a specific CPU.\r\n On 64-bit computer, the smpProcessorAffinityMask attribute contains the low-order DWORD for the processor mask, and the smpProcessorAffinityMask2 attribute contains the high-order DWORD for the processor mask.")]
        [DisplayName("Processor Affinity Mask (64-bit option)")]
        [DefaultValue(4294967295)]
        public long Mask2 { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [DisplayName("Generate Process Model Event Log Entry")]
        public LogEntrySettings LogEntry { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[identityType, username, password] Configures the application pool to run as built-in account, i.e. Application Pool Identity (recommended), Network Service, Local System, Local Service, or as a specific user identity.")]
        [DisplayName("Identity")]
        [Editor(typeof(IdentityEditor), typeof(UITypeEditor))]
        public ApplicationPoolProcessModel Identity { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[idleTimeout] Amount of time (in minutes) a worker process will remain idle before it shuts down. A worker process is idle if it is not processing requests and no new requests are received.")]
        [DisplayName("Idle Time-out (minutes)")]
        [DefaultValue(20)]
        public int IdleTimeout { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[idleTimeoutAction] What action to perform when the Idle Time-out duration has been reached.")]
        [DisplayName("Idle Time-out Action")]
        [DefaultValue(IdleTimeoutAction.Terminate)]
        public IdleTimeoutAction IdleAction { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[loadUserProfile] This setting specifies whether IIS loads the user profile for an application pool identity. When this value is true, IIS loads the user profile for the application poool identity. Set this value to false when you require the IIS 6.0 behavior of not loading the user profile for the application pool identity.")]
        [DisplayName("Load User Profile")]
        [DefaultValue(false)]
        public bool LoadUserProfile { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[maxProcesses] Maximum number of worker processes permitted to service requests for the application pool. If this number is greater than 1, the application pool is a \"Web Garden\". On a NUMA aware system, if this number is 0, IIS will start as many worker processes as there are NUMA nodes for optimal performance.")]
        [DisplayName("Maximum Worker Processes")]
        [DefaultValue(1L)]
        public long MaxProcesses { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[pingingEnabled] If true, the worker process(es) serving this application pool are pinged periodically to ensure that they are still responsive. This process is called health monitoring.")]
        [DisplayName("Ping Enabled")]
        [DefaultValue(true)]
        public bool PingingEnabled { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[pingResponseTime] Maximum time (in seconds) that a worker process is given to respond to a health monitoring ping. If the worker process does not respond, it is terminated.")]
        [DisplayName("Ping Maximum Response Time (seconds)")]
        [DefaultValue(90)]
        public uint PingResponseTime { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[pingInterval] Period of time (in seconds) between health monitoring pings sent to the worker process(es) serving this application pool.")]
        [DisplayName("Ping Period (seconds)")]
        [DefaultValue(30)]
        public uint PingInterval { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[shutdownTimeLimit] Period of time (in seconds) a worker process is given to finish processing requests and shut down. If the worker process exceeds the shutdown time limit, it is terminated.")]
        [DisplayName("Shutdown Time Limit (seconds)")]
        [DefaultValue(90)]
        public uint ShutdownTimeLimit { get; set; }

        [Browsable(true)]
        [Category("Process Model")]
        [Description("[startupTimeLimit] Period of time (in seconds) a worker process is given to start up and initialize. If the worker process initialization exceeds the startup time limit,  it is terminated.")]
        [DisplayName("Startup Time Limit (seconds)")]
        [DefaultValue(30)]
        public uint StartupTimeLimit { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        internal class LogEntrySettings
        {
            [Browsable(true)]
            [Description("[IdleTimeout] If true, an event log entry is generated when the application pool is shutdown after exceeding its idle time-out limit.")]
            [DisplayName("Idle Time-out Reached")]
            [DefaultValue(true)]
            public bool IdleTimeout { get; set; }

            public override string ToString()
            {
                return string.Empty;
            }
        }
    }
}
