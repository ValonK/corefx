﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Configuration
{
    internal class ImplicitMachineConfigHost : DelegatingConfigHost
    {
        private string _machineStreamName;
        ConfigurationFileMap _fileMap;

        internal ImplicitMachineConfigHost(UpdateConfigHost host)
        {
            // Delegate to the host provided.
            Host = host;
        }

        public override void InitForConfiguration(ref string locationSubPath, out string configPath,
            out string locationConfigPath, IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams)
        {
            // Stash the filemap so we can see if the machine config was explicitly specified
            _fileMap = (ConfigurationFileMap)hostInitConfigurationParams[0];
            base.InitForConfiguration(ref locationSubPath, out configPath, out locationConfigPath, configRoot, hostInitConfigurationParams);
        }

        public override void Init(IInternalConfigRoot configRoot, params object[] hostInitParams)
        {
            // Stash the filemap so we can see if the machine config was explicitly specified
            _fileMap = (ConfigurationFileMap)hostInitParams[0];
            base.Init(configRoot, hostInitParams);
        }

        public override string GetStreamName(string configPath)
        {
            string name = base.GetStreamName(configPath);

            if (ConfigPathUtility.GetName(configPath) == ClientConfigurationHost.MachineConfigName
                && !string.Equals(_fileMap?.MachineConfigFilename, name))
            {
                // The machine config was asked for and wasn't explicitly
                // specified, stash the "default" machine.config path
                _machineStreamName = name;
            }

            return name;
        }

        public override Stream OpenStreamForRead(string streamName)
        {
            Stream stream = base.OpenStreamForRead(streamName);

            if (stream == null && streamName == _machineStreamName)
            {
                // We only want to inject if we aren't able to load
                stream = new MemoryStream(Encoding.UTF8.GetBytes(s_implicitMachineConfig));
            }

            return stream;
        }

        private static string s_implicitMachineConfig =
@"<configuration>
    <configSections>
        <section name='appSettings' type='System.Configuration.AppSettingsSection, System.Configuration' restartOnExternalChanges='false' requirePermission='false'/>
    </configSections>
</configuration>";
    }
}
