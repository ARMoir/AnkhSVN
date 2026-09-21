// Copyright 2026 The AnkhSVN Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Copilot;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Ankh.Copilot
{
    /// <summary>
    /// Uses Visual Studio's brokered Copilot service. This deliberately runs
    /// inside devenv only when invoked so it shares Visual Studio's Copilot
    /// authentication instead of starting a second Copilot CLI login.
    /// </summary>
    public static class VisualStudioCopilot
    {
        public static async Task<string> GenerateAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("A Copilot prompt is required.", "prompt");

            CancellationToken cancellationToken = CancellationToken.None;

            IBrokeredServiceContainer container =
                await AsyncServiceProvider.GlobalProvider
                    .GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();

            if (container == null)
                throw new InvalidOperationException("Visual Studio's brokered service container is unavailable.");

            IServiceBroker serviceBroker = container.GetFullAccessServiceBroker();
            if (serviceBroker == null)
                throw new InvalidOperationException("Visual Studio's full-access service broker is unavailable.");

            ICopilotService copilotService =
                await serviceBroker.GetProxyAsync<ICopilotService>(
                    CopilotDescriptors.CopilotService,
                    cancellationToken);

            if (copilotService == null)
            {
                throw new InvalidOperationException(
                    "Visual Studio Copilot is not available. Install/enable GitHub Copilot and sign in to Copilot in Visual Studio.");
            }

            try
            {
                bool available = await copilotService.CheckAvailabilityAsync(cancellationToken);
                if (!available)
                {
                    throw new InvalidOperationException(
                        "Visual Studio Copilot is installed but is not currently available. Make sure Copilot is enabled and signed in.");
                }

                CopilotSessionOptions options =
                    new CopilotSessionOptions(new CopilotClientId("AnkhSVN"));

                ICopilotSession session =
                    await copilotService.StartSessionAsync(options, cancellationToken);

                if (session == null)
                    throw new InvalidOperationException("Visual Studio Copilot could not start a session.");

                try
                {
                    CopilotRequest request = new CopilotRequest(prompt);
                    CopilotResponse response =
                        await session.SendRequestAsync(request, cancellationToken);

                    if (response == null)
                        throw new InvalidOperationException("Visual Studio Copilot returned no response.");

                    StringBuilder text = new StringBuilder();
                    foreach (CopilotContentTextPart part in response.Content.OfType<CopilotContentTextPart>())
                        text.Append(part.Content);

                    string result = text.ToString().Trim();
                    if (result.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "Visual Studio Copilot returned a response without commit-message text.");
                    }

                    return result;
                }
                finally
                {
                    IDisposable disposableSession = session as IDisposable;
                    if (disposableSession != null)
                        disposableSession.Dispose();
                }
            }
            finally
            {
                IDisposable disposableService = copilotService as IDisposable;
                if (disposableService != null)
                    disposableService.Dispose();
            }
        }
    }
}
